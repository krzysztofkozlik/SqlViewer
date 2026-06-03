using Microsoft.AspNetCore.Mvc;
using SqlViewer.Api.Services;

namespace SqlViewer.Api.Controllers;

[ApiController]
[Route("api/session")]
public class SessionController : ControllerBase
{
    private readonly IMonitoringService _monitoring;

    public SessionController(IMonitoringService monitoring)
    {
        _monitoring = monitoring;
    }

    [HttpGet]
    public IActionResult GetState() => Ok(new { state = _monitoring.State.ToString() });

    [HttpPost("start")]
    public async Task<IActionResult> Start()
    {
        await _monitoring.StartAsync();
        return Ok(new { state = _monitoring.State.ToString() });
    }

    [HttpPost("pause")]
    public async Task<IActionResult> Pause()
    {
        await _monitoring.PauseAsync();
        return Ok(new { state = _monitoring.State.ToString() });
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop()
    {
        await _monitoring.StopAsync();
        return Ok(new { state = _monitoring.State.ToString() });
    }
}
