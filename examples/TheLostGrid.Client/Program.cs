using System.Net.Http.Json;
using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.Contracts.Optimizations;
using Polly;
using Serilog;

namespace TheLostGrid.Client;

public static class Program
{
    private static readonly string ServerUrl =
        Environment.GetCommandLineArgs().Skip(1).FirstOrDefault() ??
        Environment.GetEnvironmentVariable("PIXEL_TERMINAL_SERVER_URL") ??
        throw new InvalidOperationException("Server URL is unspecified. Please provide the URL as the first command-line argument or set the 'PIXEL_TERMINAL_SERVER_URL' environment variable.");

    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri(ServerUrl),
        Timeout = new TimeSpan(hours: 0, minutes: 5, seconds: 0)
    };

    public static async Task Main()
    {
        // Store logs cleanly in the system's temporary directory to avoid cluttering source folders
        string logPath = Path.Combine(Path.GetTempPath(), "PixelTerminalUI", "client-.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        // Ensure logs are flushed when the application exits
        AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

        Console.Title = "The Lost Grid Terminal UI";
        Console.CursorVisible = true;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Guid? currentSessionId = null;
        string nextUserInput = string.Empty;

        // Configure Polly retry strategy for transient network errors
        ResiliencePipeline<HttpResponseMessage> pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>(),
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = static args =>
                {
                    Console.Write($"\r[Network Warning]: Connection lost. Retrying attempt {args.AttemptNumber + 1}/5...");
                    return default;
                }
            })
            .Build();

        Console.Clear();

        while (true)
        {
            TerminalRequest request = new(currentSessionId, nextUserInput);
            HttpResponseMessage responseMessage;

            Log.Information("Sending request to server. SessionId: {SessionId}, Input: '{Input}'",
                currentSessionId, nextUserInput);

            try
            {
                // Execute the HTTP POST request inside the resilient pipeline
                responseMessage = await pipeline.ExecuteAsync(
                    async token => await HttpClient.PostAsJsonAsync("api/terminal/input", request, token));
            }
            catch (Exception ex)
            {
                // Thrown only if all 5 retry attempts failed completely
                Log.Fatal(ex, "Permanent connection loss after maximum retry attempts. Destination: {Url}", ServerUrl);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[Fatal Network Failure]: Connection lost permanently. {ex.Message}");
                break;
            }

            // Evaluate the final response after all retry attempts are exhausted
            if (!responseMessage.IsSuccessStatusCode)
            {
                Log.Error("Server returned a non-success status code: {StatusCode}", responseMessage.StatusCode);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[Server Error]: {responseMessage.StatusCode}");
                break;
            }

            TerminalResponse? response;
            try
            {
                response = await responseMessage.Content.ReadFromJsonAsync<TerminalResponse>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to deserialize JSON response from the server.");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[Client Error]: Received empty or corrupted payload from server.");
                break;
            }

            if (response == null)
            {
                Log.Error("Server payload deserialized to null.");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[Client Error]: Received empty or corrupted payload from server.");
                break;
            }

            // Session lifecycle tracking
            if (currentSessionId != response.SessionId)
            {
                Log.Information("Session initialized or mutated. New SessionId: {SessionId}", response.SessionId);
                currentSessionId = response.SessionId;
            }

            ClearBottomConsoleLine(response.Height + 3);

            int bufferWidth = response.Width;
            int bufferHeight = response.Height;

            // Expand the rendering viewport boundaries by 2 to perfectly host the outer frame lines
            int frameWidth = bufferWidth + 2;
            int frameHeight = bufferHeight + 2;

            // Use pattern matching to branch rendering logic based on response type
            try
            {
                switch (response)
                {
                    case FullFrameResponse fullFrame:
                        {
                            Log.Information("Received FullFrameResponse. Redrawing entire TUI canvas ({Width}x{Height}).",
                                bufferWidth, bufferHeight);

                            // Fully redraw the screen along with the decorative border layout
                            Console.Clear();
                            Console.SetCursorPosition(0, 0);

                            for (int y = 0; y < frameHeight; y++)
                            {
                                for (int x = 0; x < frameWidth; x++)
                                {
                                    bool isTopOrBottomRow = y == 0 || y == frameHeight - 1;
                                    bool isLeftOrRightColumn = x == 0 || x == frameWidth - 1;

                                    if (isTopOrBottomRow && isLeftOrRightColumn)
                                    {
                                        Console.Write('+');
                                        continue;
                                    }
                                    if (isTopOrBottomRow)
                                    {
                                        Console.Write('-');
                                        continue;
                                    }
                                    if (isLeftOrRightColumn)
                                    {
                                        Console.Write('|');
                                        continue;
                                    }

                                    int serverX = x - 1;
                                    int serverY = y - 1;
                                    uint packedPixel = fullFrame.ScreenBuffer[serverY * bufferWidth + serverX];

                                    PixelBitPacker.Unpack(
                                        packedPixel,
                                        out char symbol,
                                        out byte foregroundByte,
                                        out byte backgroundByte,
                                        out byte flags);

                                    ConsoleColor fgColor = (ConsoleColor)foregroundByte;
                                    ConsoleColor bgColor = (ConsoleColor)backgroundByte;
                                    bool isInverted = flags == 1;

                                    // Apply inversion flag logic directly from the packed byte token
                                    if (isInverted)
                                    {
                                        Console.ForegroundColor = bgColor;
                                        Console.BackgroundColor = fgColor;
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = fgColor;
                                        Console.BackgroundColor = bgColor;
                                    }

                                    Console.Write(symbol);
                                }

                                Console.ResetColor();
                                Console.WriteLine();
                            }
                            break;
                        }

                    case DeltaResponse delta:
                        {
                            Log.Information("Received DeltaResponse containing {MutationCount} pixel mutations.",
                                delta.Mutations.Length);

                            // Mutate ONLY changed internal cells; borders remain completely untouched
                            foreach (PixelMutation mutation in delta.Mutations)
                            {
                                // Deconstruct 1D flat server array index into 2D grid coordinates
                                int serverX = mutation.Index % bufferWidth;
                                int serverY = mutation.Index / bufferWidth;

                                // Shift coordinates by +1 on the client to skip the left and top border lines
                                int clientX = serverX + 1;
                                int clientY = serverY + 1;

                                // Bounds safety check before moving hardware cursor console position
                                if (clientX >= 0 && clientX < Console.BufferWidth &&
                                    clientY >= 0 && clientY < Console.BufferHeight)
                                {
                                    Console.SetCursorPosition(clientX, clientY);

                                    PixelBitPacker.Unpack(
                                        mutation.PackedValue,
                                        out char symbol,
                                        out byte foregroundByte,
                                        out byte backgroundByte,
                                        out byte flags);

                                    ConsoleColor fgColor = (ConsoleColor)foregroundByte;
                                    ConsoleColor bgColor = (ConsoleColor)backgroundByte;
                                    bool isInverted = flags == 1;

                                    if (isInverted)
                                    {
                                        Console.ForegroundColor = bgColor;
                                        Console.BackgroundColor = fgColor;
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = fgColor;
                                        Console.BackgroundColor = bgColor;
                                    }

                                    Console.Write(symbol);
                                }
                            }
                            break;
                        }

                    default:
                        Log.Warning("Received unknown or unhandled response type: {Type}", response.GetType().Name);
                        break;
                }
                int inputLineY = response.Height + 2;
                ClearBottomConsoleLine(inputLineY);

                Console.ResetColor();
                Console.Write("> ");

                string? rawInput = Console.ReadLine();
                nextUserInput = rawInput ?? string.Empty;

                if (nextUserInput == "-q")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nDisconnecting neural link session connection wrapper...");
                    break;
                }
            }
            catch (Exception ex)
            {
                // Use pattern matching to split logging and user messages based on exception type
                if (ex is IndexOutOfRangeException or DivideByZeroException)
                {
                    Log.Error(ex, "Render pipeline failed due to corrupted server payload dimensions or indices.");

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[Client Error]: Severe rendering crash caused by corrupted server payload.");
                }
                else
                {
                    // Capture unexpected critical system/hardware failures without hardcoding specific error context
                    Log.Fatal(ex, "Unexpected fatal error occurred inside the rendering loop pipeline.");

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[Fatal Failure]: {ex.Message}");
                }

                break; // Terminate execution loop safely for any unhandled exception type
            }
        }

        Console.ResetColor();
        Console.WriteLine("\nSession closed. Press any key to exit terminal interface console...");
        Console.ReadKey();
    }

    /// <summary>
    /// Helper layout method to wipe any artifacts below the game matrix frame
    /// </summary>
    private static void ClearBottomConsoleLine(int targetY)
    {
        if (targetY >= 0 && targetY < Console.BufferHeight)
        {
            Console.SetCursorPosition(0, targetY);
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.SetCursorPosition(0, targetY);
        }
    }
}
