using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend.Models;
using backend.Services;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("/[controller]")]
[Authorize]
public class WritingController : ControllerBase
{
    private readonly IWritingService _writingService;

    public WritingController(IWritingService writingService)
    {
        _writingService = writingService;
    }

    [HttpPost("submit")]
    public async Task<ActionResult<ResponseWritingModel>> SubmitEssay([FromBody] CreateWritingModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var result = await _writingService.SubmitEssayAsync(userId, model);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<PagedResponseWritingModel>> GetEssayHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1)
        {
            return BadRequest("Page and pageSize must be positive integers.");
        }
        if (pageSize > 30)
        {
            return BadRequest("PageSize must be less than or equal to 30.");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var essays = await _writingService.GetEssayHistoryAsync(userId, page, pageSize);

        var response = new PagedResponseWritingModel
        {
            Essays = essays,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }
}

public class PagedResponseWritingModel
{
    public List<ResponseWritingModel> Essays { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}