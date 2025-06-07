using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Models;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("/[controller]")]
[Authorize]
public class ListeningController : ControllerBase
{
    private readonly IListeningService _listeningService;
    private readonly IProService _proService;

    public ListeningController(IListeningService listeningService, IProService proService)
    {
        _listeningService = listeningService;
        _proService = proService;
    }

    [HttpPost("submit")]
    public async Task<ActionResult<ResponseListeningModel>> SubmitOrGetDictation([FromBody] CreateListeningModel model)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!await _proService.IsProUserAsync(userId))
        {
            return Unauthorized("You need a Pro subscription to access this feature.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingDictation = await _listeningService.GetDictationAsync(model);

        if (existingDictation != null)
        {
            return Ok(existingDictation);
        }

        var result = await _listeningService.SubmitDictationAsync(model);
        return Ok(result);
    }

    [HttpGet("topics")]
    public async Task<ActionResult<List<ResponseListeningModel>>> GetTopics(
        [FromQuery] string topic,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!await _proService.IsProUserAsync(userId))
        {
            return Unauthorized("You need a Pro subscription to access this feature.");
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            return BadRequest("Topic is required.");
        }

        if (page < 1 || pageSize < 1)
        {
            return BadRequest("Page and pageSize must be positive integers.");
        }

        var topics = await _listeningService.GetTopicAsync(topic, page, pageSize);
        return Ok(topics);
    }
}