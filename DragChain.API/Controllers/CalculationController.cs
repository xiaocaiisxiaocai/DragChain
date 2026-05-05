using Microsoft.AspNetCore.Mvc;
using DragChain.API.Models.DTOs;
using DragChain.API.Services;

namespace DragChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalculationController : ControllerBase
{
    private readonly ICalculationService _calcService;

    public CalculationController(ICalculationService calcService)
    {
        _calcService = calcService;
    }

    [HttpPost("calc")]
    public async Task<ActionResult<CalculationResponse>> Calculate([FromBody] CalculationRequest request)
    {
        var result = await _calcService.CalculateAsync(request);
        return Ok(result);
    }
}
