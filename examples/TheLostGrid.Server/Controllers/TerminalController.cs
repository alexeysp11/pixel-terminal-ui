using Microsoft.AspNetCore.Mvc;
using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.StatelessEngine.RequestPipeline;

namespace TheLostGrid.Server.Controllers;

[ApiController]
[Route("api/terminal")]
public class TerminalController(IRequestPipelineHandler pipelineHandler) : ControllerBase
{
    [HttpPost("input")]
    public async Task<ActionResult<TerminalResponse>> ProcessInput([FromBody] TerminalRequest request)
    {
        TerminalResponse response = await pipelineHandler.HandleInputAsync(request);
        return Ok(response);
    }
}
