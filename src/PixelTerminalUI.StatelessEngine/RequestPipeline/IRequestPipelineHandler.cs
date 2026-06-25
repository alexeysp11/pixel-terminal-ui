using PixelTerminalUI.Contracts.Dto;

namespace PixelTerminalUI.StatelessEngine.RequestPipeline;

/// <summary>
/// Defines the core pipeline processing orchestration boundary contract responsible for catching raw user inputs, 
/// validating states, applying operations commands, and triggering the downstream renderer.
/// </summary>
public interface IRequestPipelineHandler
{
    /// <summary>
    /// Processes incoming raw payload character triggers within an ongoing connection scope, 
    /// executing layout mutations and delivering a structural output frame package.
    /// </summary>
    /// <param name="request">The structural payload enclosing session identifiers tokens and raw input strings coordinates.</param>
    /// <returns>A generalized polymorphic response structure holding either fresh full view buffers arrays or precise token deltas arrays.</returns>
    Task<TerminalResponse> HandleInputAsync(TerminalRequest request);
}
