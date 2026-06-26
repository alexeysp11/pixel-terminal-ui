using System.Net.Http.Json;
using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.Contracts.Optimizations;

namespace TheLostGrid.Client;

public static class Program
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5221/"),
        Timeout = new TimeSpan(hours: 0, minutes: 5, seconds: 0)
    };

    public static async Task Main()
    {
        Console.Title = "The Lost Grid Terminal UI";
        Console.CursorVisible = true;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Guid? currentSessionId = null;
        string nextUserInput = string.Empty;

        Console.Clear();

        while (true)
        {
            try
            {
                TerminalRequest request = new(currentSessionId, nextUserInput);
                HttpResponseMessage responseMessage = await HttpClient.PostAsJsonAsync("api/terminal/input", request);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[Server Error]: {responseMessage.StatusCode}");
                    break;
                }

                TerminalResponse? response = await responseMessage.Content.ReadFromJsonAsync<TerminalResponse>();
                if (response == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[Client Error]: Received empty or corrupted payload from server.");
                    break;
                }

                currentSessionId = response.SessionId;

                int bufferWidth = response.Width;
                int bufferHeight = response.Height;

                // Expand the rendering viewport boundaries by 2 to perfectly host the outer frame lines
                int frameWidth = bufferWidth + 2;
                int frameHeight = bufferHeight + 2;

                // Use pattern matching to branch rendering logic based on response type
                switch (response)
                {
                    case FullFrameResponse fullFrame:
                        {
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
                }
                int inputLineY = response.Height + 2;
                Console.SetCursorPosition(0, inputLineY);

                // Clean up any trailing text artifacts or ghosts from the previous command.
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, inputLineY);

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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[Fatal Network Failure]: {ex.Message}");
                break;
            }
        }

        Console.ResetColor();
        Console.WriteLine("\nSession closed. Press any key to exit terminal interface console...");
        Console.ReadKey();
    }
}
