using CarparkInfo.Api.Carparks;
using Microsoft.AspNetCore.Mvc;

namespace CarparkInfo.Api.Controllers;

[ApiController]
[Route("api/favourites")]
public sealed class FavouritesController : ControllerBase
{
    private readonly ICarparkService _carparkService;

    public FavouritesController(ICarparkService carparkService)
    {
        _carparkService = carparkService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CarparkDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CarparkDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _carparkService.GetFavouritesAsync(cancellationToken));
    }

    [HttpPost("{carParkNo}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(string carParkNo, CancellationToken cancellationToken)
    {
        try
        {
            await _carparkService.AddFavouriteAsync(carParkNo, cancellationToken);
            return NoContent();
        }
        catch (CarparkNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpDelete("{carParkNo}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(string carParkNo, CancellationToken cancellationToken)
    {
        var removed = await _carparkService.RemoveFavouriteAsync(carParkNo, cancellationToken);
        return removed ? NoContent() : NotFound(new { message = $"Carpark '{carParkNo}' is not in the favourites list." });
    }
}
