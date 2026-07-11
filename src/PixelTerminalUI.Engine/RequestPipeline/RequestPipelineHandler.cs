using System.Buffers;
using Microsoft.Extensions.Logging;
using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.Contracts.Optimizations;
using PixelTerminalUI.Engine.Commands.CommandContexts;
using PixelTerminalUI.Engine.Extensions.ServiceCollectionExtensions;
using PixelTerminalUI.Engine.Factories.StartupScreen;
using PixelTerminalUI.Engine.Factories.TerminalErrorScreen;
using PixelTerminalUI.Engine.Navigation;
using PixelTerminalUI.Engine.Rendering.Core;
using PixelTerminalUI.Engine.Repositories;
using PixelTerminalUI.Engine.ResponseBuilders;
using PixelTerminalUI.Engine.Screens;
using PixelTerminalUI.Engine.SymbolHandling;
using PixelTerminalUI.Engine.Validators;
using PixelTerminalUI.Engine.Validators.ValidationProviders;
using PixelTerminalUI.Engine.Widgets;

namespace PixelTerminalUI.Engine.RequestPipeline;

/// <summary>
/// Orchestrates the primary structural connection pipeline by coordinating validation rules, 
/// executing state mutations via input special symbols and embedded components commands, and directing the final double-buffered view layout rendering sequences.
/// </summary>
/// <param name="options">The centralized configurations container used to selectively bypass or enforce performance optimizations.</param>
/// <param name="renderer">The stateless canvas viewport matrix compiler used to paint screen layouts into flat arrays containers.</param>
/// <param name="sessionRepository">The consolidated data storage abstraction boundary managing persistent multi-document screen trees and cache records states.</param>
/// <param name="startupScreenFactory">The activation delegate factory used to initialize fresh screen trees during cold start events.</param>
/// <param name="adaptiveResponseBuilder">The pure functional matrix difference computation utility used to branch transport packets based on change density threshold ratios.</param>
/// <param name="validationProvider">The dynamic registry repository storing screen-specific stateless verification constraints.</param>
public sealed class RequestPipelineHandler(
    ILogger<RequestPipelineHandler> logger,
    PixelTerminalUIOptions options,
    IStatelessRenderer renderer,
    ITerminalSessionRepository sessionRepository,
    IStartupScreenFactory startupScreenFactory,
    ITerminalErrorScreenFactory errorScreenFactory,
    IAdaptiveResponseBuilder adaptiveResponseBuilder,
    IScreenValidationProvider validationProvider,
    ISpecialSymbolHandler symbolHandler,
    IFocusManager focusManager) : IRequestPipelineHandler
{
    /// <inheritdoc/>
    public async Task<TerminalResponse> HandleInputAsync(TerminalRequest request)
    {
        try
        {
            TerminalScreen? screen = null;
            if (request.SessionId.HasValue)
            {
                screen = await sessionRepository.GetActiveScreenAsync(request.SessionId.Value);
            }

            Guid sessionId = request.SessionId ?? Guid.NewGuid();
            if (screen is null)
            {
                screen = startupScreenFactory.CreateScreen(sessionId);
                await sessionRepository.SaveActiveScreenAsync(sessionId, screen);
                return await RenderAndBuildResponseAsync(screen);
            }

            // Evaluate systemic industrial terminal navigational special symbols
            SymbolHandlingResult symbolResult = await symbolHandler.HandleSymbolAsync(screen, request.UserInput);

            if (symbolResult.Action == SymbolResultActionType.TerminateSession)
            {
                // Pack character payload into 32-bit unsigned primitives with default colors
                // (Foreground: White = 15, Background: Black = 0, Flags: 0)
                const byte background = (byte)ConsoleColor.Black;
                const int foreground = (byte)ConsoleColor.White;
                uint[] flatBuffer = [
                    PixelBitPacker.Pack('S', background, foreground, 0),
                PixelBitPacker.Pack('E', background, foreground, 0),
                PixelBitPacker.Pack('S', background, foreground, 0)
                ];

                return new TerminalResponse(sessionId, Width: 3, Height: 1, FullFrame: new FullFramePayload(flatBuffer));
            }

            if (symbolResult.Action == SymbolResultActionType.NavigateToParentScreen)
            {
                // Safety check: if the current screen layout container lacks parent identifiers pointers, freeze context
                if (!screen.ParentScreenId.HasValue)
                {
                    await sessionRepository.SaveActiveScreenAsync(sessionId, screen);
                    return await RenderAndBuildResponseAsync(screen);
                }

                Guid closingScreenId = screen.Id;
                Guid parentScreenId = screen.ParentScreenId.Value;

                // Pull the structural historical parent blueprint configuration using its explicit target instance checkpoint identifier
                TerminalScreen? parentScreen = await sessionRepository.GetScreenByIdAsync(sessionId, parentScreenId);

                if (parentScreen != null)
                {
                    await sessionRepository.SaveActiveScreenAsync(sessionId, parentScreen);
                    await sessionRepository.RemoveScreenAsync(sessionId, closingScreenId);
                    return await RenderAndBuildResponseAsync(parentScreen);
                }
            }

            if (symbolResult.Action == SymbolResultActionType.ShiftFocusForward)
            {
                screen.FocusedEntryWidgetId = focusManager.GetNextFocus(screen);
                await sessionRepository.SaveActiveScreenAsync(sessionId, screen);
                return await RenderAndBuildResponseAsync(screen);
            }

            if (symbolResult.Action == SymbolResultActionType.ShiftFocusBackward)
            {
                // Reset edit widget.
                TextWidget? focusedEntryWidget = screen.Widgets.FirstOrDefault(x => x.Id == screen.FocusedEntryWidgetId);
                if (focusedEntryWidget is not null)
                {
                    focusedEntryWidget.Value = string.Empty;
                }

                screen.FocusedEntryWidgetId = focusManager.GetPreviousFocus(screen);
                await sessionRepository.SaveActiveScreenAsync(sessionId, screen);
                return await RenderAndBuildResponseAsync(screen);
            }

            // Processing local screen state mutations (e.g. data scrub via -r token)
            if (symbolResult.Action == SymbolResultActionType.StayOnScreen)
            {
                await sessionRepository.SaveActiveScreenAsync(sessionId, screen);
                return await RenderAndBuildResponseAsync(screen);
            }

            if (symbolResult.Action == SymbolResultActionType.RefreshActiveScreen)
            {
                TerminalScreen? currentScreen = await sessionRepository.GetActiveScreenAsync(sessionId);
                if (currentScreen is null)
                {
                    await sessionRepository.SaveActiveScreenAsync(sessionId, screen);
                    return await RenderAndBuildResponseAsync(screen);
                }
                return await RenderAndBuildResponseAsync(currentScreen);
            }

            // Execute quick stateless validation rules from memory registry dictionaries maps
            IEnumerable<ValidationDelegate> screenValidators = validationProvider.GetValidatorsForScreen(screen.Name);
            foreach (ValidationDelegate validate in screenValidators)
            {
                ValidationResult validationResult = validate(screen, request.UserInput);
                if (!validationResult.IsValid)
                {
                    SimpleMessageScreen errorScreen = errorScreenFactory.BuildErrorScreen(sessionId, screen, validationResult.ErrorMessage ?? "Validation Fault!");
                    await sessionRepository.SaveActiveScreenAsync(sessionId, errorScreen);
                    return await RenderAndBuildResponseAsync(errorScreen);
                }
            }

            // Evaluate active editable fields business values inputs and workflows transitions
            TextWidget? focusedWidget = screen.Widgets.FirstOrDefault(c => c.Id == screen.FocusedEntryWidgetId);
            if (focusedWidget is TextEntryWidget entryWidget)
            {
                if (string.IsNullOrEmpty(entryWidget.Value) || !string.IsNullOrEmpty(request.UserInput))
                {
                    entryWidget.Value = request.UserInput;
                }

                if (entryWidget.Command != null)
                {
                    CommandContext commandContext = new(
                        sessionId: sessionId,
                        screen: screen,
                        focusedEntryWidget: entryWidget,
                        inputValue: entryWidget.Value,
                        sessionRepository: sessionRepository
                    );

                    bool isExecutionSuccessful = await entryWidget.Command.ExecuteAsync(commandContext);
                    if (!isExecutionSuccessful)
                    {
                        // Extract the mutated presentation layout snapshot to preserve dynamic resource changes
                        TerminalScreen actualScreen = await sessionRepository.GetActiveScreenAsync(sessionId) ?? screen;

                        // Locate the target interaction element within the freshly restored screen context boundaries
                        TextWidget? actualEntryWidget = actualScreen.Widgets.FirstOrDefault(w => w.Id == entryWidget.Id);
                        if (actualEntryWidget is TextEntryWidget textInput)
                        {
                            // Unconditionally scrub the raw textual value to prevent stale input retention upon errors
                            textInput.Value = string.Empty;
                        }

                        // Guarantee that the synchronized layout state with cleared input is persisted back to storage
                        await sessionRepository.SaveActiveScreenAsync(sessionId, actualScreen);

                        string errorMessage = commandContext.ErrorMessage ?? "Command Rejected Execution!";

                        // Pass the properly sanitized actualScreen reference so the historical parent state is clean
                        SimpleMessageScreen businessErrorScreen = errorScreenFactory.BuildErrorScreen(sessionId, actualScreen, errorMessage);

                        await sessionRepository.SaveActiveScreenAsync(sessionId, businessErrorScreen);
                        return await RenderAndBuildResponseAsync(businessErrorScreen);
                    }
                    screen = await sessionRepository.GetActiveScreenAsync(sessionId) ?? screen;
                }

                if (screen.FocusedEntryWidgetId == entryWidget.Id)
                {
                    screen.FocusedEntryWidgetId = focusManager.GetNextFocus(screen);
                }
            }
            await sessionRepository.SaveActiveScreenAsync(sessionId, screen);
            return await RenderAndBuildResponseAsync(screen);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to handle input");
            throw;
        }
    }

    private async Task<TerminalResponse> RenderAndBuildResponseAsync(TerminalScreen screen)
    {
        int width = screen.Width;
        int height = screen.Height;
        int totalCellsCount = width * height;

        // Conditionally fetch historical state data rows from database storage only if double buffering is explicitly enabled
        uint[]? historicalBuffer = null;
        if (options.EnableDoubleBuffering)
        {
            historicalBuffer = await sessionRepository.GetHistoricalBufferAsync(screen.SessionId);
        }

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

        // Builder maps data cleanly: if historical buffer is null, it immediately skips delta processing loop and yields a FullFrameResponse
        TerminalResponse response = adaptiveResponseBuilder.Build(
            screen.SessionId,
            currentFlatBuffer,
            historicalBuffer,
            width,
            height);

        // Conditionally bypass persistence write calls to save networking infrastructure bandwidth costs if the cache is disabled
        if (options.EnableDoubleBuffering)
        {
            await sessionRepository.SaveHistoricalBufferAsync(screen.SessionId, currentFlatBuffer);
        }

        return response;
    }
}
