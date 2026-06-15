using CarparkInfo.Api.Carparks;
using Microsoft.AspNetCore.Mvc;

namespace CarparkInfo.Api.Controllers;

[ApiController]
[Route("api/carparks")]
public sealed class CarparksController : ControllerBase
{
    private readonly ICarparkService _carparkService;

    public CarparksController(ICarparkService carparkService)
    {
        _carparkService = carparkService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CarparkDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CarparkDto>>> Search(
        [FromQuery] bool? hasFreeParking,
        [FromQuery] bool? hasNightParking,
        [FromQuery] decimal? minimumVehicleHeight,
        CancellationToken cancellationToken)
    {
        var result = await _carparkService.SearchAsync(
            new CarparkSearchRequest(hasFreeParking, hasNightParking, minimumVehicleHeight),
            cancellationToken);

        return Ok(result);
    }
}
