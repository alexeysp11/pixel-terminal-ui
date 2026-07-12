# Распределенный рендеринг в консоли: переносим стейт-машину TUI-движка на gRPC и Redis

## Введение

В рамках работы над системой управления складом (WMS) я столкнулся со спецификой текстовых терминальных интерфейсов (Terminal UI). В таких системах логика обмена данными строится в парадигме Backend-Driven UI (BDUI), но в ее самом экстремальном, текстовом проявлении. Вместо передачи дерева компонентов или HTML-разметки, сервер полностью берет на себя всю графическую работу и возвращает клиенту готовую текстовую матрицу символов и цветов.

Концепция Backend-Driven UI (BDUI) прочно закрепилась в веб- и мобильной разработке, где клиент получает от сервера метаданные компонентов и преобразует их в интерфейс. Однако если применить эту парадигму к текстовым терминалам (Terminal UI), стандартный подход с передачей дерева виджетов теряет смысл. Чтобы клиент оставался максимально легковесным и «тупым», сервер должен брать на себя всю графическую работу и возвращать готовую текстовую матрицу символов и цветов.

#### Почему именно «тупой клиент» и текстовая матрица?

В основе автоматизации крупных складских комплексов (WMS) до сих пор лежит стек, способный вызвать у современного веб-разработчика культурный шок. Логика взаимодействия оператора и системы часто строится через стандартные Telnet-консоли, запущенные на промышленных терминалах сбора данных (ТСД) вроде премиальных Zebra или бюджетных китайских Urovo.

В такой архитектуре ТСД не содержит в себе ни одной строчки бизнес-логики. Он работает как «тупой клиент», отправляя сетевой запрос на сервер буквально на каждый чих — любое нажатие физической клавиши пользователем. Сервер принимает этот ввод, делает циклический обход событий, пересчитывает состояние интерфейса и отправляет обратно по сети строго детерминированный массив байт: символы и цвета, которые нужно изменить на экране.

У использования текстовой матрицы вместо HTML-страниц или мобильных приложений есть суровая прагматика:
* **Игнорирование геометрии зоопарка устройств**: Экранные матрицы жестко зафиксированы в стандартных координатах (например, 40x12 или 80x25). Это позволяет серверу абстрагироваться от физических габаритов, разрешений и пропорций дисплеев разных линеек Zebra и Urovo. Интерфейс гарантированно выглядит одинаково предсказуемо на любом девайсе.
* **Централизованный Deployment**: Изменение шагов валидации или логики экранов не требует раскатки обновлений через системы управления мобильными устройствами (MDM). В условиях распределенного парка ТСД по сети всегда есть риск, что часть терминалов не обновится, застрянет на промежуточной версии или отвалится в процессе. В Telnet-архитектуре все изменения деплоятся исключительно на сервере, и операторы мгновенно видят новую логику без прикосновения к самим ТСД.

#### Движок

Технически описываемый в статье движок представляет собой серверный рендерер текстовой матрицы. Его ключевая особенность — абстракция над слоем хранения сессий через интерфейс `ITerminalSessionRepository`. Это позволяет разворачивать как локальные in-memory конфигурации, так и полноценные распределенные хранилища на базе Redis, PostgreSQL или MongoDB.

Основная цель данного Proof of Concept (PoC) — проектирование распределенной стейт-машины шагов, способной изолированно обрабатывать каждый отдельный ввод пользователя на любом независимом инстансе сервера бэкенда, не удерживая постоянное состояние в оперативной памяти.

## Глава 1. Ограничения легаси-архитектур и тупик Stateful-модели

При проектировании систем управления терминалами традиционный подход опирается на Stateful-модель. Состояние сессии либо жестко привязывается к постоянному TCP-соединению, либо оседает внутри серверного `ConcurrentDictionary`.

Ниже представлен фрагмент реализации, иллюстрирующий, как объектно-ориентированное проектирование «в лоб» создает критические ограничения для масштабирования:
```csharp
public abstract class TerminalScreen
{
    public string Name { get; set; } = nameof(TerminalScreen);
    public int Height { get; set; }
    public int Width { get; set; }
    public SessionInfo? SessionInfo { get; set; }
    
    // Прямая ссылка на родительский экран (удержание графа объектов в куче)
    public TerminalScreen? ParentScreen { get; set; } 
    
    public List<TextWidget> Widgets { get; set; } = [];
    public TextEntryWidget? FocusedEntryWidget { get; set; }
    
    // Синхронные делегаты логики и навигационных переходов
    public Func<bool>? ShowValidation { get; set; }
    public Action? ShowMainMenu { get; set; }

    // Метод перехода, порождающий глубокую вложенность Call Stack
    public void ShowScreen(TerminalScreen screen)
    {
        try
        {
            SessionInfo.CurrentScreen = screen;
            screen.SessionInfo = SessionInfo;
            screen.ParentScreen = this; // Замыкание ссылки на предыдущее состояние
            screen.Init();
            screen.Show(); // Поток проваливается во вложенный метод и блокируется
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }
}
```

Данная модель обладает тремя неустранимыми архитектурными недостатками:
- **Удержание графа объектов**: Свойство `ParentScreen = this` заставляет рантайм хранить в куче (Heap) всю историю переходов пользователя. Память не освобождается, пока сессия активна.
- **Блокировка потоков во вложенном Call Stack**: Вызов `screen.Show()` внутри `ShowScreen` приводит к тому, что поток исполнения проваливается на глубину иерархии экранов. Поток физически заблокирован сервером в ожидании следующего ввода от конкретного терминала.
- **Нулевое масштабирование**: Поскольку состояние и поток выполнения привязаны к конкретной машине, горизонтальное масштабирование бэкенда (Scale Out) становится невозможным. Падение инстанса означает мгновенную потерю данных всех подключенных к нему сессий.

Чтобы преодолеть эти ограничения, необходимо было полностью переписать логику управления состоянием, разорвать синхронный Call Stack выполнения и вынести контекст шагов в распределенный слой хранения. О том, как это реализовано на базе стейт-машины команд, Redis и высокопроизводительного бинарного gRPC-пайплайна, мы и поговорим далее.

## Глава 2. Распределенный Stateless и стейт-машины команд

Когда речь заходит о Backend-Driven UI (BDUI), в памяти сразу всплывает мобильная разработка: сервер отдает дерево компонентов (JSON/Protobuf), а клиент на iOS или Android это дерево парсит и рендерит. Но если мы спускаемся на уровень текстовых терминалов, эта схема ломается. Передавать метаданные виджетов — значит заставлять тонкий клиент решать, как их расположить, красить и инвертировать. Чтобы сохранить парадигму «тупого» терминала, сервер должен отдавать не схему компонентов, а **готовый массив пикселей кадра или его дельту**.

Главная проблема классических терминальных движков (Stateful) — удержание сессии в памяти конкретного сервера. Поток выполнения блокируется в ожидании пользовательского ввода, полные графы объектов экранов и истории команд живут в куче (Heap), намертво привязывая пользователя к единственному инстансу бэкенда.

Чтобы сделать систему по-настоящему Stateless и распределенной, нам нужно было решить три задачи:
- Полностью выгружать состояние экрана из памяти сервера после каждого отданного ответа.
- Научить систему обрабатывать первый ввод пользователя на Сервере 1, а следующий клик — на Сервере 2 без потери контекста.
- Избавиться от аллокаций памяти в горячих путях выполнения стейт-машины, чтобы не душить сборщик мусора (GC).

### Анатомия распределенной команды и уход от жесткого Call Stack

Чтобы разорвать Call Stack и освободить потоки сервера от ожидания ввода, логика многошаговых интерфейсов была переведена на асинхронные команды, управляемые числовым токеном состояния (`RawState`).

Вот как выглядит базовый каркас контрактов виджетов и команд:
```csharp
public record TextWidget
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Value { get; set; }
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Visible { get; set; }
    public bool Inverted { get; set; }
    public virtual bool Editable { get; set; } = false;
    public int? TabIndex { get; set; }
    public ConsoleColor Foreground { get; set; } = ConsoleColor.White;
    public ConsoleColor Background { get; set; } = ConsoleColor.Black;
}

public record TextEntryWidget : TextWidget
{
    public override bool Editable { get; set; } = true;
    public bool Required { get; set; } = true;
    public char EmptyEnterSymbol { get; set; } = '.';
    public string? Hint { get; set; }
    public CommandBase? Command { get; set; }
}

public abstract record TerminalScreen
{
    public required Guid Id { get; set; }
    public required Guid SessionId { get; set; }
    public required string Name { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public IEnumerable<TextWidget> Widgets { get; set; } = [];
    public Guid? FocusedEntryWidgetId { get; set; }
    public bool EnableDoubleBuffering { get; init; } = false;
}

public interface ICommand
{
    Guid Id { get; }
    Guid WidgetId { get; set; }
    int RawState { get; set; }
    ValueTask<bool> ExecuteAsync(ICommandContext context);
}
```

Когда пользователь отправляет ввод, бэкенд не держит в памяти запущенный метод. Пайплайн работает декларативно:
- Из Redis по ключу `SessionId` поднимается легковесный хэш-сет (Redis Hash), содержащий только сырые данные состояния и текущий числовой идентификатор шага команды.
- Фабрика экранов собирает объект `TerminalScreen` с нуля.
- Извлеченный из Redis `int` прокидывается в свойство `RawState` команды, мгновенно восстанавливая логический контекст.

### Оптимизация перечислений: Борьба с Boxing через `Unsafe.As`

Писать стейт-машину на сырых `int`-ах в бизнес-коде — это гарантированный способ запутаться в индексах и выстрелить себе в ногу. С точки зрения читаемости кода шаги обязаны быть строго типизированными перечислениями (`enum`).

Однако в C# приведение обобщенного типа перечисления к целому числу (и обратно) в обобщенных классах стандартными методами вроде `(int)(object)State` или `Convert.ToInt32(State)` приводит к упаковке значимого типа (Boxing). Рантайм вынужден аллоцировать память в куче, чтобы завернуть `enum` в объект. В цикле высокоинтенсивного ввода, когда сотни пользователей одновременно колотят по клавишам терминала, эти микро-аллокации мгновенно забивают память мусором и вызывают микрофризы из-за работы Garbage Collector.

Чтобы обойти это ограничение рантайма без потери производительности, мы применили жесткое побитовое копирование ссылок через сырую память с принудительным инлайнингом методов:
```csharp
public abstract class CommandBase : ICommand
{
    public abstract Guid Id { get; }
    public abstract Guid WidgetId { get; set; }
    public abstract int RawState { get; set; }
    public abstract ValueTask<bool> ExecuteAsync(ICommandContext context);
}

public abstract class Command<TEnum> : CommandBase where TEnum : struct, Enum
{
    public abstract TEnum State { get; set; }

    public override int RawState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            TEnum localState = State;
            return Unsafe.As<TEnum, int>(ref localState);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            int localValue = value;
            State = Unsafe.As<int, TEnum>(ref localValue);
        }
    }
}
```

**Как это работает под капотом:** Инструкция `Unsafe.As` берет ссылку на ячейку памяти, где лежит значимый тип `TEnum`, и заставляет JIT-компилятор интерпретировать эти биты напрямую как `int` (и наоборот). За счет `MethodImplOptions.AggressiveInlining` этот вызов полностью растворяется в вызывающем коде, превращаясь в прямую процессорную команду работы с регистрами.

### Реализация многошагового сценария в действии

Посмотрим, как эта архитектура разворачивается на примере Sequence-команды обработки ввода. Метод `ExecuteAsync` выполняется атомарно и мгновенно возвращает управление, не блокируя поток сервера:
```csharp
public enum MultiStepState
{
    Initial = 0,
    Processing = 1,
    Finalizing = 2
}

public sealed class SequenceExecutionCommand : Command<MultiStepState>
{
    public override MultiStepState State { get; set; } = MultiStepState.Initial;
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }

    public override ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        if (context == null) return ValueTask.FromResult(false);

        switch (State)
        {
            case MultiStepState.Initial:
                // Фаза 1: Бизнес-валидация первичного ввода
                if (string.Equals(context.InputValue, "ABORT", StringComparison.OrdinalIgnoreCase))
                {
                    context.ErrorMessage = "Operation interrupted.";
                    return ValueTask.FromResult(false);
                }

                // Переводим команду на следующий шаг и мгновенно выходим
                State = MultiStepState.Processing;
                return ValueTask.FromResult(true);

            case MultiStepState.Processing:
                // Фаза 2: Глубокая проверка транзакционного ключа
                if (context.InputValue.Length < 3)
                {
                    context.ErrorMessage = "The token is too short.";
                    return ValueTask.FromResult(false);
                }

                State = MultiStepState.Finalizing;
                return ValueTask.FromResult(true);

            case MultiStepState.Finalizing:
                // Фаза 3: Завершение и очистка контекста навигации
                return ValueTask.FromResult(true);

            default:
                return ValueTask.FromResult(false);
        }
    }
}
```

С точки зрения рантайма переход выглядит как O(1) прыжок по таблице индексов перечисления. Команда считала шаг, переключила стейт в памяти, завершила выполнение, и обновленный `int` улетел обратно в Redis Hash под защитой оптимистичной блокировки версий сессии.

#### Взгляд в будущее: Овечий сахар против рантайм-перформанса

<!-- Обязательно переписать -->

С точки зрения конечного разработчика, декларативные Fluent-цепочки в стиле Workflow Core (например, `.StartWith<Step1>().Next<Step2>().Then<Step3>()`) выглядели бы гораздо элегантнее, чем громоздкие switch-case блоки внутри одного класса команды.

Однако важно понимать цену такой абстракции в распределенной системе. Под капотом любого «красивого» Fluent API неизбежно сидит точно такой же конечный автомат (State Machine), который прыгает по стейт-токенам. Для систем с ультра-низким задержками (где важна каждая наносекунда), классический switch обеспечивает максимально быстрый переход по таблице перечислений за O(1) без построения промежуточных цепочек объектов в памяти.

Идеальным вектором развития здесь видится скрещивание Fluent-описания с Source Generators (кодогенерацией): когда разработчик описывает бизнес-процесс красивым Fluent-кодом, а компилятор на этапе сборки разворачивает его в такой же плоский, агрессивно заинлайненный switch-case, сохраняя и чистоту кода, и Zero Allocation в рантайме.

### Связь стейт-машины с рендерингом кадра и обработкой ошибок

Такой подход накладывает жесткие правила на смежные слои системы — рендеринг и обработку ошибок:

1. **Гибридный расчет дельт (Double Buffering)**:

Бизнес-логика команд полностью изолирована от транспортного слоя. Когда команда успешно меняет шаг и возвращает `true`, управление переходит в общий пайплайн `HandleInputAsync`. Если в конфигурации активирована опция `EnableDoubleBuffering = true`, движок разворачивает оптимизационный алгоритм: он поднимает предыдущий (исторический) слепок кадра из Redis, в цикле сравнивает его с вновь отрендеренным экраном, вычленяет точечную дельту мутаций, сохраняет новую матрицу в кэш и отправляет клиенту компактный бинарный gRPC-пакет дельты.

2. **Парадигма Server-Driven UI для ошибок**:

В сетевом контракте нет REST-подобных JSON-ответов с кодами ошибок. Если шаг валидации внутри команды завалился (вернул `false` и записал строку в `context.ErrorMessage`), движок не генерирует исключений. Он на лету создает новый рекорд экрана ошибки, прорисовывает этот текст прямо в матрице пикселей на стороне сервера, и этот измененный кадр штатно улетает в gRPC-канал. Клиент остается абсолютно «тупым» — он просто послушно выводит на физический экран консоли то, что прислал бэкенд.

## Глава 3. Битва за байты: Пулинг, битовые маски и исчезающие массивы

Обеспечение Stateless-логики на бэкенде неизбежно увеличивает нагрузку на инфраструктурные слои. Если на каждый ввод пользователя движок будет выделять память под новые массивы данных, парсить тяжелые текстовые структуры и гонять их по сети, система захлебнется в аллокациях (GC Pressure), а время кадра выйдет далеко за рамки целевых 30 миллисекунд.

Для минимизации накладных расходов в горячих путях рендеринга и сетевого маршалинга были внедрены три оптимизационных механизма:
- Переиспользование плоских буферов отрисовки через пул объектов (`ArrayPool<T>`).
- Побитовое сжатие метаданных символа и цвета в единый примитив `uint`.
- Защита сетевого контракта от особенностей десериализации бинарных потоков Protobuf.

### Оптимизация горячих путей рендеринга через `ArrayPool`

В традиционной модели отрисовки интерфейса каждый новый кадр генерирует в куче (Heap) свежий массив объектов. Чтобы предотвратить постоянную нагрузку на сборщик мусора (Garbage Collector), новый рендерер движка переведен на симметричную аренду буферов из общего пула `ArrayPool<Pixel>.Shared`.

Выделение памяти происходит только один раз — под финальный плоский массив сжатых данных, который отправляется в gRPC-канал:
```csharp
Pixel[] pooledBuffer = ArrayPool<Pixel>.Shared.Rent(totalCellsCount);
uint[] currentFlatBuffer = new uint[totalCellsCount];

try
{
    renderer.Draw(screen, pooledBuffer);
    
    for (int index = 0; index < totalCellsCount; index++)
    {
        Pixel currentPixel = pooledBuffer[index];
        byte inversionFlag = (byte)(currentPixel.IsInverted ? 1 : 0);

        currentFlatBuffer[index] = PixelBitPacker.Pack(
            currentPixel.Symbol,
            (byte)currentPixel.Foreground,
            (byte)currentPixel.Background,
            inversionFlag);
    }
}
finally
{
    ArrayPool<Pixel>.Shared.Return(pooledBuffer);
}
```

Специфика работы `ArrayPool` заключается в том, что метод Rent может вернуть массив, реальная длина которого превышает запрошенный `totalCellsCount`. Чтобы излишки буфера и «грязные» хвосты от предыдущих операций рендеринга не попали в кадр, логика прорисовки внутри `renderer.Draw` жестко абстрагирована от физического размера массива. Движок оперирует исключительно логическими границами `Width` и `Height` текущего экрана:
```csharp
int width = screen.Width;
int height = screen.Height;

// Инициализируем экран пустым пространством по умолчанию, используя пиксели заполнения, заданные формулой смещения плоского массива.
for (int y = 0; y < height; y++)
{
    for (int x = 0; x < width; x++)
    {
        buffer[y * width + x] = new Pixel(' ', false, ConsoleColor.White);
    }
}
```

### Компактный байт-лейаут: Упаковка кадра через битовые маски

Передавать по сети массив объектов `Pixel` (пусть даже это `readonly record struct`) — неэффективно с точки зрения объема трафика. Данные пикселя (символ char, цвета `Foreground`/`Background` и флаги инверсии) были упакованы в одно 32-битное целое число (`uint`) с помощью побитовых масок и сдвигов с принудительным инлайнингом методов:
```csharp
public static class PixelBitPacker
{
    private const uint CharMask = 0xFFFF;
    private const uint ColorMask = 0x7F;
    private const uint FlagsMask = 0x03;
    
    private const int CharShift = 16;
    private const int ForegroundShift = 9;
    private const int BackgroundShift = 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Pack(char character, byte foreground, byte background, byte flags)
    {
        return (uint)character << CharShift
               | (foreground & ColorMask) << ForegroundShift
               | (background & ColorMask) << BackgroundShift
               | flags & FlagsMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Unpack(uint packed, out char character, out byte foreground, out byte background, out byte flags)
    {
        character = (char)(packed >> CharShift & CharMask);
        foreground = (byte)(packed >> ForegroundShift & ColorMask);
        background = (byte)(packed >> BackgroundShift & ColorMask);
        flags = (byte)(packed & FlagsMask);
    }
}
```

Поскольку JIT-компилятор полностью внедряет логику `Pack`/`Unpack` напрямую в вызывающие циклы, операция упаковки выполняется на уровне процессорных регистров. Весь графический кадр превращается в плоский и легковесный массив примитивов `uint[]`.

### Гибридный расчет дельт через Redis

Управление стратегией рендеринга вынесено на уровень конфигурации IoC-контейнера при регистрации зависимостей движка в `Program.cs.` Разработчик может гибко переключать режим оптимизации сетевого трафика с помощью флага `EnableDoubleBuffering`:
```csharp
builder.Services.AddPixelTerminalUI(options =>
{
    options.EnableDoubleBuffering = true;
});
```

Когда в конфигурации движка активирована опция `EnableDoubleBuffering = true`, сервер не отправляет клиенту полный массив пикселей при каждом изменении. Вместо этого он рассчитывает попиксельную разницу (дельту). Поскольку серверный инстанс полностью Stateless и не удерживает состояние кадра в памяти, исторический буфер предыдущего экрана при каждом запросе извлекается из распределенного хранилища `Redis Hash`:
```csharp
public async Task<uint[]?> GetHistoricalBufferAsync(Guid sessionId)
{
    try
    {
        string sessionKey = GetSessionKey(sessionId);
        RedisValue bufferJson = await _database.HashGetAsync(sessionKey, HistoricalBufferField);
        
        return bufferJson.IsNullOrEmpty 
            ? null 
            : JsonSerializer.Deserialize<uint[]>(bufferJson.ToString(), jsonOptions);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to load buffer for terminal session {SessionId}", sessionId);
        throw;
    }
}
```
Движок сопоставляет текущий flat-массив с историческим из Redis, вычленяет только изменившиеся индексы, формирует массив структур `PixelMutation` и перезаписывает новый слепок экрана обратно в `Redis Hash`.

<!-- Про NRE из-за пустых массивов: подумать, добавлять или нет -->
