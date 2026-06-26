using FluentAssertions;
using Moq;
using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using PixelTerminalUI.StatelessEngine.Commands.DismissError;
using PixelTerminalUI.StatelessEngine.Extensions.ServiceCollectionExtensions;
using PixelTerminalUI.StatelessEngine.Factories.StartupScreen;
using PixelTerminalUI.StatelessEngine.Factories.TerminalErrorScreen;
using PixelTerminalUI.StatelessEngine.Rendering.Core;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.StatelessEngine.RequestPipeline;
using PixelTerminalUI.StatelessEngine.ResponseBuilders;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Validators;
using PixelTerminalUI.StatelessEngine.Validators.ValidationProviders;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.StatelessEngine.Tests.RequestPipeline;

public sealed class RequestPipelineHandlerTests
{
    private readonly Mock<IStatelessRenderer> _rendererMock;
    private readonly Mock<ITerminalSessionRepository> _sessionRepositoryMock;
    private readonly Mock<IStartupScreenFactory> _startupScreenFactoryMock;
    private readonly Mock<ITerminalErrorScreenFactory> _errorScreenFactoryMock;
    private readonly Mock<IAdaptiveResponseBuilder> _adaptiveResponseBuilderMock = new();
    private readonly Mock<IScreenValidationProvider> _validationProviderMock = new();
    private readonly PixelTerminalUIOptions _defaultOptions = new();

    private readonly RequestPipelineHandler _sut;

    public RequestPipelineHandlerTests()
    {
        _rendererMock = new Mock<IStatelessRenderer>();
        _sessionRepositoryMock = new Mock<ITerminalSessionRepository>();
        _startupScreenFactoryMock = new Mock<IStartupScreenFactory>();
        _errorScreenFactoryMock = new Mock<ITerminalErrorScreenFactory>();

        // Mock the renderer to accept the flat array buffer without throwing exceptions
        _rendererMock
            .Setup(r => r.Draw(It.IsAny<TerminalScreen>(), It.IsAny<Pixel[]>()));

        // Configure the responsive builder to return a valid response object by default.
        _adaptiveResponseBuilderMock
            .Setup(b => b.Build(
                It.IsAny<Guid>(),
                It.IsAny<uint[]>(),
                It.IsAny<uint[]>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns((Guid sid, uint[] cur, uint[]? hist, int w, int h) =>
                new FullFrameResponse(sid, cur, w, h));

        // Setup the frame buffer repository to return null historical buffer by default
#nullable disable
        _sessionRepositoryMock
            .Setup(r => r.GetHistoricalBufferAsync(It.IsAny<Guid>()))
            .ReturnsAsync((uint[])null);
#nullable restore

        _validationProviderMock
            .Setup(p => p.GetValidatorsForScreen(It.IsAny<string>()))
            .Returns([]);

        _sut = new RequestPipelineHandler(
            _defaultOptions,
            _rendererMock.Object,
            _sessionRepositoryMock.Object,
            _startupScreenFactoryMock.Object,
            _errorScreenFactoryMock.Object,
            _adaptiveResponseBuilderMock.Object,
            _validationProviderMock.Object);
    }

    [Fact]
    public async Task HandleInputAsync_WhenInputIsValid_ShouldUpdateWidgetValueExecuteCommandAndSaveState()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid focusedWidgetId = Guid.NewGuid();
        string userInput = "WMS-SCAN-456";

        Mock<CommandBase> commandMock = new();
        commandMock
            .Setup(c => c.ExecuteAsync(It.IsAny<ICommandContext>()))
            .ReturnsAsync(true);

        TextEntryWidget activeWidget = new()
        {
            Id = focusedWidgetId,
            Name = "BarcodeEditor",
            Value = string.Empty,
            Visible = true,
            Command = commandMock.Object
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "UnknownScreenWithNoValidators",
            Width = 10,
            Height = 5,
            FocusedEntryWidgetId = focusedWidgetId,
            Widgets = [activeWidget]
        };

        TerminalRequest request = new(sessionId, userInput);

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, default))
            .ReturnsAsync(screen);

        // Act
        TerminalResponse response = await _sut.HandleInputAsync(request);

        // Assert
        activeWidget.Value
            .Should()
            .Be(userInput, "because the handler must apply raw input before executing business commands");

        // Verify command execution with proper context details
        commandMock.Verify(c => c.ExecuteAsync(It.Is<ICommandContext>(ctx =>
            ctx.SessionId == sessionId &&
            ctx.InputValue == userInput &&
            ctx.FocusedEntryWidget == activeWidget)), Times.Once);

        // Verify state persistence and rendering flow
        _sessionRepositoryMock.Verify(r => r.SaveActiveScreenAsync(sessionId, screen, default), Times.Once);
        _rendererMock.Verify(r => r.Draw(screen, It.IsAny<Pixel[]>()), Times.Once);

        response
            .Should()
            .NotBeNull("because the pipeline handler must return a generated terminal response framework structure")
            .And.BeOfType<FullFrameResponse>("because the default mocked behavior yields a full frame representation layout");

        response.SessionId
            .Should()
            .Be(sessionId, "because the response identity must match the originating user interactive terminal session");
    }

    [Fact]
    public async Task HandleInputAsync_WhenHistoricalBufferExists_ShouldPassItToBuilderAndSaveNewBuffer()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid focusedWidgetId = Guid.NewGuid();
        uint[] mockHistoricalBuffer = [12, 14, 16];

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "BufferIntegrationScreen",
            Width = 3,
            Height = 1,
            FocusedEntryWidgetId = focusedWidgetId,
            Widgets = []
        };

        TerminalRequest request = new(sessionId, "AnyInput");

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, default))
            .ReturnsAsync(screen);

        _sessionRepositoryMock
            .Setup(r => r.GetHistoricalBufferAsync(sessionId))
            .ReturnsAsync(mockHistoricalBuffer);

        // Act
        TerminalResponse response = await _sut.HandleInputAsync(request);

        // Assert
        _sessionRepositoryMock.Verify(r => r.GetHistoricalBufferAsync(sessionId), Times.Once);

        _adaptiveResponseBuilderMock.Verify(b => b.Build(
            sessionId,
            It.Is<uint[]>(current => current.Length == 3),
            mockHistoricalBuffer,
            3,
            1), Times.Once);

        _sessionRepositoryMock.Verify(r => r.SaveHistoricalBufferAsync(
            sessionId,
            It.Is<uint[]>(current => current.Length == 3)), Times.Once);

        response
            .Should()
            .NotBeNull("because rendering workflow must complete and yield an interactive frame data bundle");
    }

    [Fact]
    public async Task HandleInputAsync_WhenStatelessValidationFails_ShouldShortCircuitToErrorNotificationScreen()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        string userInput = "ThisStringIsWayTooLongForThePhysicalTerminalBufferLimit";

        Mock<CommandBase> commandMock = new();
        TextEntryWidget activeWidget = new()
        {
            Id = Guid.NewGuid(),
            Name = "UsernameField",
            Value = string.Empty,
            Command = commandMock.Object
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "UsersScreen",
            Width = 10,
            Height = 5,
            FocusedEntryWidgetId = activeWidget.Id,
            Widgets = [activeWidget]
        };

        // Создаем заглушку экрана ошибки, которую вернет фабрика
        SimpleMessageScreen mockErrorScreen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "ErrorNotificationView",
            Width = 10,
            Height = 5,
            Visible = true,
            Widgets = []
        };

        TerminalRequest request = new(sessionId, userInput);

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, default))
            .ReturnsAsync(screen);

        // Configure the validation provider stub to explicitly return a failing validator delegate for this screen
        ValidationDelegate failingValidator = (terminalScreen, input) => ValidationResult.Fail("Input exceeds physical buffer limit!");

        _validationProviderMock
            .Setup(p => p.GetValidatorsForScreen("UsersScreen"))
            .Returns(new ValidationDelegate[] { failingValidator });

        // Настраиваем мок фабрики ошибок, чтобы избежать NullReferenceException
        _errorScreenFactoryMock
            .Setup(f => f.BuildErrorScreen(sessionId, screen, It.IsAny<string>()))
            .Returns(mockErrorScreen);

        _rendererMock
            .Setup(r => r.Draw(It.Is<TerminalScreen>(f => f.Name == "ErrorNotificationView"), It.IsAny<Pixel[]>()));

        // Act
        TerminalResponse response = await _sut.HandleInputAsync(request);

        // Assert
        // Verify core logic domains are completely bypassed upon data validation crashes
        commandMock.Verify(c => c.ExecuteAsync(It.IsAny<ICommandContext>()), Times.Never);

        // Verify that instead of saving the corrupted parent screen state, we persisted the error notification view mapping
        _sessionRepositoryMock.Verify(r => r.SaveActiveScreenAsync(sessionId, It.Is<TerminalScreen>(f => f.Name == "ErrorNotificationView"), default), Times.Once);

        // Verify that the rendering pipeline explicitly processed the generated window overlay stack error layer using a flat buffer
        _rendererMock.Verify(r => r.Draw(It.Is<TerminalScreen>(f => f.Name == "ErrorNotificationView"), It.IsAny<Pixel[]>()), Times.Once);

        response.SessionId
            .Should()
            .Be(sessionId, "because even inside short-circuit fault flows the core terminal context must maintain user routing session continuity");
    }

    [Fact]
    public async Task HandleInputAsync_WhenCommandExecutionFails_ShouldSaveErrorScreenAndShortCircuit()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid focusedWidgetId = Guid.NewGuid();
        string userInput = "INVALID-PALLET-ID";

        Mock<CommandBase> commandMock = new();
        commandMock
            .Setup(c => c.ExecuteAsync(It.IsAny<ICommandContext>()))
            .ReturnsAsync(false);

        TextEntryWidget activeWidget = new()
        {
            Id = focusedWidgetId,
            Name = "PalletEditor",
            Value = string.Empty,
            Visible = true,
            Command = commandMock.Object
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "UnknownScreenWithNoValidators",
            Width = 10,
            Height = 5,
            FocusedEntryWidgetId = focusedWidgetId,
            Widgets = [activeWidget]
        };

        // Создаем заглушку экрана ошибки, которую вернет фабрика
        SimpleMessageScreen mockErrorScreen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "ErrorNotificationView",
            Width = 10,
            Height = 5,
            Visible = true,
            Widgets = []
        };

        TerminalRequest request = new(sessionId, userInput);

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, default))
            .ReturnsAsync(screen);

        // Настраиваем мок фабрики ошибок, чтобы избежать NullReferenceException
        _errorScreenFactoryMock
            .Setup(f => f.BuildErrorScreen(sessionId, screen, It.IsAny<string>()))
            .Returns(mockErrorScreen);

        _rendererMock
            .Setup(r => r.Draw(It.Is<TerminalScreen>(f => f.Name == "ErrorNotificationView"), It.IsAny<Pixel[]>()));

        // Act
        TerminalResponse response = await _sut.HandleInputAsync(request);

        // Assert
        _sessionRepositoryMock.Verify(r => r.SaveActiveScreenAsync(sessionId, It.Is<TerminalScreen>(f => f.Name == "ErrorNotificationView"), default), Times.Once,
            "the pipeline handler must push an error notification view blueprint onto the session storage stack when business workflows fail");

        _rendererMock.Verify(r => r.Draw(It.Is<TerminalScreen>(f => f.Name == "ErrorNotificationView"), It.IsAny<Pixel[]>()), Times.Once,
            "the core engine must render the error message screen back onto the physical client hardware");

        response.SessionId
            .Should()
            .Be(sessionId, "because routing sessions continuity must survive step execution failures");
    }

    [Fact]
    public async Task HandleInputAsync_WhenActiveWidgetHasNoCommand_ShouldStillPersistAndRenderWithoutCrashing()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid focusedWidgetId = Guid.NewGuid();
        string userInput = "Some Input";

        TextEntryWidget activeWidget = new()
        {
            Id = focusedWidgetId,
            Name = "StandardField",
            Value = string.Empty,
            Visible = true,
            Command = null
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "ScreenWithoutCommand",
            Width = 10,
            Height = 5,
            FocusedEntryWidgetId = focusedWidgetId,
            Widgets = [activeWidget]
        };

        TerminalRequest request = new(sessionId, userInput);

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, default))
            .ReturnsAsync(screen);

        // Act
        TerminalResponse response = await _sut.HandleInputAsync(request);

        // Assert
        activeWidget.Value
            .Should()
            .Be(userInput, "because raw data values must be stamped onto active field layers even if no command processors are bound");

        _sessionRepositoryMock.Verify(r => r.SaveActiveScreenAsync(sessionId, screen, default), Times.Once,
            "the core orchestration pipeline must persist the modified layout screen state data successfully");

        _rendererMock.Verify(r => r.Draw(screen, It.IsAny<Pixel[]>()), Times.Once,
            "the engine must re-render the view model grid buffer data across the network transport stream");

        response
            .Should()
            .NotBeNull();
    }

    [Fact]
    public async Task HandleInputAsync_WhenActiveWidgetHasCommand_ShouldSetActiveWidgetValue()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid focusedWidgetId = Guid.NewGuid();
        string userInput = "Clean Input String";

        Mock<CommandBase> commandMock = new();
        commandMock
            .Setup(c => c.ExecuteAsync(It.IsAny<ICommandContext>()))
            .ReturnsAsync(true);

        TextEntryWidget activeWidget = new()
        {
            Id = focusedWidgetId,
            Name = "ActiveDataField",
            Value = string.Empty,
            Visible = true,
            Command = commandMock.Object
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "StandardInteractiveView",
            Width = 10,
            Height = 5,
            FocusedEntryWidgetId = focusedWidgetId,
            Widgets = [activeWidget]
        };

        TerminalRequest request = new(sessionId, userInput);

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, default))
            .ReturnsAsync(screen);

        // Act
        TerminalResponse response = await _sut.HandleInputAsync(request);

        // Assert
        activeWidget.Value
            .Should()
            .Be(userInput, "because the pipeline must write verified user inputs directly into the active tracking widget state buffer slots");

        _sessionRepositoryMock.Verify(r => r.SaveActiveScreenAsync(sessionId, screen, default), Times.Once,
            "successful pipeline passes must commit updated states nodes transformations graph blocks directly into the session cache");

        _rendererMock.Verify(r => r.Draw(screen, It.IsAny<Pixel[]>()), Times.Once);

        response
            .Should()
            .NotBeNull();
    }

    [Fact]
    public async Task HandleInputAsync_WhenSessionDoesNotExist_ShouldPerformColdStartAndReturnInitialScreen()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        TerminalRequest request = new(sessionId, "Any Input");

        Mock<IStatelessRenderer> rendererMock = new();
        Mock<ITerminalSessionRepository> sessionRepositoryMock = new();
        Mock<IStartupScreenFactory> startupScreenFactoryMock = new();
        Mock<IAdaptiveResponseBuilder> adaptiveResponseBuilderMock = _adaptiveResponseBuilderMock;
        Mock<IScreenValidationProvider> validationProviderMock = _validationProviderMock;

        rendererMock
            .Setup(r => r.Draw(It.IsAny<TerminalScreen>(), It.IsAny<Pixel[]>()));

        SimpleMessageScreen dummyStartupScreen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "StartupScreen",
            Width = 8,
            Height = 3,
            Visible = true,
            Widgets = []
        };

        sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, default))
            .ReturnsAsync((TerminalScreen?)null);

        startupScreenFactoryMock
            .Setup(f => f.CreateScreen(sessionId))
            .Returns(dummyStartupScreen);

        RequestPipelineHandler sut = new(
            _defaultOptions,
            rendererMock.Object,
            sessionRepositoryMock.Object,
            startupScreenFactoryMock.Object,
            _errorScreenFactoryMock.Object,
            adaptiveResponseBuilderMock.Object,
            validationProviderMock.Object);

        // Act
        TerminalResponse response = await sut.HandleInputAsync(request);

        // Assert
        startupScreenFactoryMock.Verify(f => f.CreateScreen(sessionId), Times.Once,
            "the framework must invoke the startup factory registration logic when encountering uninitialized users tracking contexts");

        sessionRepositoryMock.Verify(r => r.SaveActiveScreenAsync(sessionId, dummyStartupScreen, default), Times.Once,
            "the pipeline must snapshot the newly instantiated cold start screen back down into the persistent document layer");

        rendererMock.Verify(r => r.Draw(dummyStartupScreen, It.IsAny<Pixel[]>()), Times.Once,
            "the orchestration engine must render the fresh startup template straight onto the output matrix transport array");

        response.SessionId
            .Should()
            .Be(sessionId, "because even inside initial allocation sequences the network packet routing boundary must sustain contextual identification");
    }

    [Fact]
    public async Task HandleInputAsync_WhenSendingEmptyEnter_ShouldCoordinateHandlersAndShiftFocusForward()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid firstFieldId = Guid.NewGuid();
        Guid secondFieldId = Guid.NewGuid();

        TextEntryWidget firstWidget = new()
        {
            Id = firstFieldId,
            Name = "FirstInputBuffer",
            Left = 0,
            Top = 0,
            Width = 5,
            Visible = true,
            Value = string.Empty,
            Required = false
        };

        TextEntryWidget secondWidget = new()
        {
            Id = secondFieldId,
            Name = "SecondInputBuffer",
            Left = 0,
            Top = 1,
            Width = 5,
            Value = string.Empty,
            Visible = true
        };

        SimpleMessageScreen parentScreen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "InventoryTransferScreen",
            Width = 10,
            Height = 3,
            Visible = true,
            FocusedEntryWidgetId = firstFieldId,
            Widgets = [firstWidget, secondWidget]
        };

        TerminalRequest emptyEnterRequest = new(sessionId, string.Empty);

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, default))
            .ReturnsAsync(parentScreen);

        // Act
        TerminalResponse response = await _sut.HandleInputAsync(emptyEnterRequest);

        // Assert
        parentScreen.FocusedEntryWidgetId
            .Should()
            .Be(secondFieldId, "because empty entries register as navigational forwarding signals which shift active focus pointers downward");

        _sessionRepositoryMock.Verify(r => r.SaveActiveScreenAsync(sessionId, It.Is<TerminalScreen>(f => f.FocusedEntryWidgetId == secondFieldId), default), Times.Once,
            "the pipeline repository wrapper must persist updated navigational changes directly onto the active session entry document");

        _rendererMock.Verify(r => r.Draw(It.Is<TerminalScreen>(f => f.FocusedEntryWidgetId == secondFieldId), It.IsAny<Pixel[]>()), Times.Once,
            "the stateless renderer must receive the screen after the navigation loop changes focused identifiers coordinates values");

        response
            .Should()
            .NotBeNull("because the client terminal application requires a valid response data packet stream container to paint refreshed viewports");
    }

    [Fact]
    public async Task HandleInputAsync_WithCancelSymbol_ShouldRestoreParentScreenAndMaintainOriginalFocusCheckpoint()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid parentScreenId = Guid.NewGuid();
        Guid childScreenId = Guid.NewGuid();
        Guid originalFocusedWidgetId = Guid.NewGuid();

        TextEntryWidget originalActiveWidget = new()
        {
            Id = originalFocusedWidgetId,
            Name = "BarcodeScannerField",
            Value = "PRE-FILLED-DATA",
            Visible = true
        };

        SimpleMessageScreen parentScreen = new()
        {
            Id = parentScreenId,
            SessionId = sessionId,
            Name = "MainReceiptScreen",
            Width = 10,
            Height = 3,
            Visible = true,
            FocusedEntryWidgetId = originalFocusedWidgetId,
            Widgets = [originalActiveWidget]
        };

        SimpleMessageScreen currentChildErrorScreen = new()
        {
            Id = childScreenId,
            SessionId = sessionId,
            Name = "ErrorNotificationView",
            Width = 10,
            Height = 3,
            Visible = true,
            ParentScreenId = parentScreenId,
            Widgets = []
        };

        TerminalRequest cancelRequest = new(sessionId, "-x");

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, default))
            .ReturnsAsync(currentChildErrorScreen);

        _sessionRepositoryMock
            .Setup(r => r.GetScreenByIdAsync(sessionId, parentScreenId, default))
            .ReturnsAsync(parentScreen);

        // Act
        TerminalResponse response = await _sut.HandleInputAsync(cancelRequest);

        // Assert
        _sessionRepositoryMock.Verify(r => r.SaveActiveScreenAsync(sessionId, It.Is<TerminalScreen>(f => f.Id == parentScreenId), default), Times.Once,
            "the persistence workflow layer must switch the active tracking document slot back onto the root screen container definition");

        _sessionRepositoryMock.Verify(r => r.RemoveScreenAsync(sessionId, childScreenId, default), Times.Once,
            "the pipeline handler must evict the transient error screen metadata from the storage engine collection upon backward navigation passes");

        _rendererMock.Verify(r => r.Draw(It.Is<TerminalScreen>(f => f.Id == parentScreenId && f.FocusedEntryWidgetId == originalFocusedWidgetId), It.IsAny<Pixel[]>()), Times.Once,
            "the engine must re-render the parent view model while perfectly preserving its original input focus checkpoint identifier");

        response
            .Should()
            .NotBeNull("because the pipeline handler must return a generated terminal response framework structure");
    }

    [Fact]
    public async Task HandleInputAsync_ShouldInterceptAndRenderErrorScreen_WhenCommandExecutionFails()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid targetInputId = Guid.NewGuid();

        Mock<CommandBase> failingCommandMock = new();
        failingCommandMock
            .Setup(c => c.ExecuteAsync(It.IsAny<ICommandContext>()))
            .ReturnsAsync(false);

        TextEntryWidget inputField = new()
        {
            Id = targetInputId,
            Name = "ClassSelectionInput",
            Width = 10,
            Visible = true,
            Command = failingCommandMock.Object,
            Value = string.Empty
        };

        SimpleMessageScreen originatingScreen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "CharacterCreationScreen",
            Width = 40,
            Height = 12,
            Visible = true,
            Widgets = [inputField],
            FocusedEntryWidgetId = targetInputId
        };

        SimpleMessageScreen mockErrorScreen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "ErrorNotificationView",
            Width = 40,
            Height = 12,
            Visible = true,
            Widgets = []
        };

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(originatingScreen);

        // Setup the new infrastructure error factory mock to return a valid instance instead of throwing NullReferenceException
        _errorScreenFactoryMock
            .Setup(f => f.BuildErrorScreen(sessionId, originatingScreen, It.IsAny<string>()))
            .Returns(mockErrorScreen);

        _rendererMock
            .Setup(r => r.Draw(It.Is<TerminalScreen>(f => f.Name == "ErrorNotificationView"), It.IsAny<Pixel[]>()));

        TerminalRequest request = new(sessionId, "INVALID_INPUT_VALUE");

        // Act
        TerminalResponse executionResponse = await _sut.HandleInputAsync(request);

        // Assert
        executionResponse
            .Should()
            .NotBeNull("because a failing execution route must yield a valid error state layout container");

        executionResponse.SessionId
            .Should()
            .Be(sessionId, "because the response identity must match the originating user interactive session");

        _sessionRepositoryMock.Verify(r => r.SaveActiveScreenAsync(sessionId, It.Is<TerminalScreen>(f => f.Name == "ErrorNotificationView"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleInputAsync_WhenDismissErrorCommandIsExecuted_ShouldRestoreAndRenderHistoricalParentScreen()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid errorInputId = Guid.NewGuid();
        Guid originalParentScreenId = Guid.NewGuid();

        SimpleMessageScreen historicalParentScreen = new()
        {
            Id = originalParentScreenId,
            SessionId = sessionId,
            Name = "CharacterCreationScreen",
            Width = 40,
            Height = 12,
            Visible = true,
            Widgets = [],
            FocusedEntryWidgetId = Guid.NewGuid()
        };

        DismissErrorCommand systemDismissCommand = new();

        TextEntryWidget escapeInput = new()
        {
            Id = errorInputId,
            Name = "ErrorAcknowledgeInput",
            Width = 5,
            Visible = true,
            Command = systemDismissCommand,
            Value = string.Empty
        };

        SimpleMessageScreen activeErrorScreen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "ErrorNotificationView",
            Width = 40,
            Height = 12,
            Visible = true,
            ParentScreenId = originalParentScreenId,
            Widgets = [escapeInput],
            FocusedEntryWidgetId = errorInputId
        };

        systemDismissCommand.WidgetId = errorInputId;

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, default))
            .ReturnsAsync(activeErrorScreen);

        _sessionRepositoryMock
            .Setup(r => r.GetScreenByIdAsync(sessionId, originalParentScreenId, default))
            .ReturnsAsync(historicalParentScreen);

        _rendererMock
            .Setup(r => r.Draw(historicalParentScreen, It.IsAny<Pixel[]>()));

        // Simulate sending the explicit cancel trigger token instead of an empty input to fire the dismiss routine
        TerminalRequest request = new(sessionId, "-x");

        // Act
        TerminalResponse executionResponse = await _sut.HandleInputAsync(request);

        // Assert
        executionResponse
            .Should()
            .NotBeNull("because executing the dismiss command must trigger a successful return frame conversion payload");

        _sessionRepositoryMock.Verify(r => r.SaveActiveScreenAsync(sessionId, historicalParentScreen, default), Times.Once);

        _rendererMock.Verify(r => r.Draw(historicalParentScreen, It.IsAny<Pixel[]>()), Times.Once,
            "the stateless engine must render the restored historical screen context layout upon successful error dismiss evaluation cycles");
    }

    [Fact]
    public async Task HandleInputAsync_WhenDoubleBufferingIsEnabled_ShouldFetchAndSaveHistoricalBuffers()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid focusedWidgetId = Guid.NewGuid();

        PixelTerminalUIOptions enabledOptions = new()
        {
            EnableDoubleBuffering = true
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "DoubleBufferingActiveScreen",
            Width = 10,
            Height = 5,
            FocusedEntryWidgetId = focusedWidgetId,
            Widgets = []
        };

        TerminalRequest request = new(sessionId, "StandardInput");

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, default))
            .ReturnsAsync(screen);

        // Injecting the enabled options configuration descriptor block explicitly into a temporary test pipeline
        RequestPipelineHandler handlerWithBuffering = new(
            enabledOptions,
            _rendererMock.Object,
            _sessionRepositoryMock.Object,
            _startupScreenFactoryMock.Object,
            _errorScreenFactoryMock.Object,
            _adaptiveResponseBuilderMock.Object,
            _validationProviderMock.Object);

        // Act
        TerminalResponse response = await handlerWithBuffering.HandleInputAsync(request);

        // Assert
        _sessionRepositoryMock.Verify(r => r.GetHistoricalBufferAsync(sessionId), Times.Once,
            "the core orchestration logic must fetch the historical frame snapshot when double buffering optimization is enabled");

        _sessionRepositoryMock.Verify(r => r.SaveHistoricalBufferAsync(sessionId, It.IsAny<uint[]>()), Times.Once,
            "the data persistence layer must overwrite the back buffer snapshot upon completing successful render operations loop updates");

        response
            .Should()
            .NotBeNull("because a fully processed data request route must complete execution and return an interactive framework packet");
    }

    [Fact]
    public async Task HandleInputAsync_WhenDoubleBufferingIsDisabled_ShouldBypassHistoricalBufferInfrastructureCalls()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        Guid focusedWidgetId = Guid.NewGuid();

        PixelTerminalUIOptions disabledOptions = new()
        {
            EnableDoubleBuffering = false
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "DoubleBufferingDisabledScreen",
            Width = 10,
            Height = 5,
            FocusedEntryWidgetId = focusedWidgetId,
            Widgets = []
        };

        TerminalRequest request = new(sessionId, "StandardInput");

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(sessionId, default))
            .ReturnsAsync(screen);

        // Injecting the disabled options reference to test the optimized shortcut bypass routing
        RequestPipelineHandler handlerWithoutBuffering = new(
            disabledOptions,
            _rendererMock.Object,
            _sessionRepositoryMock.Object,
            _startupScreenFactoryMock.Object,
            _errorScreenFactoryMock.Object,
            _adaptiveResponseBuilderMock.Object,
            _validationProviderMock.Object);

        // Act
        TerminalResponse response = await handlerWithoutBuffering.HandleInputAsync(request);

        // Assert
        _sessionRepositoryMock.Verify(r => r.GetHistoricalBufferAsync(It.IsAny<Guid>()), Times.Never,
            "the request pipeline must shortcut and skip database frame buffer reads entirely to preserve infrastructure networking costs");

        _sessionRepositoryMock.Verify(r => r.SaveHistoricalBufferAsync(It.IsAny<Guid>(), It.IsAny<uint[]>()), Times.Never,
            "the request pipeline must drop database write transactions for technical buffer mutations when caching is disabled globally");

        response
            .Should()
            .NotBeNull("because dropping double buffering performance configurations should never disrupt basic request processing streams operations");
    }
}
