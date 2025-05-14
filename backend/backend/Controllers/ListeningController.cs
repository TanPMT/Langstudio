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

    public ListeningController(IListeningService listeningService)
    {
        _listeningService = listeningService;
    }

    [HttpPost("submit")]
    public async Task<ActionResult<ResponseListeningModel>> SubmitOrGetDictation([FromBody] CreateListeningModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if the YouTube link already exists in the database
        var existingDictation = await _listeningService.GetDictationAsync(model);

        if (existingDictation != null)
        {
            // If the link exists, return the existing dictation
            return Ok(existingDictation);
        }

        // If the link doesn't exist, submit a new dictation
        var result = await _listeningService.SubmitDictationAsync(model);
        return Ok(result);
    }

    [HttpGet("topics")]
    public async Task<ActionResult<List<ResponseListeningModel>>> GetTopics(
        [FromQuery] string topic,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
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