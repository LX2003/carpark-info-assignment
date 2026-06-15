using CarparkInfo.Api.Importing;
using Microsoft.AspNetCore.Mvc;

namespace CarparkInfo.Api.Controllers;

[ApiController]
[Route("api/imports")]
public sealed class ImportsController : ControllerBase
{
    private readonly ICsvCarparkImportService _importService;

    public ImportsController(ICsvCarparkImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("carparks")]
    [ProducesResponseType(typeof(CsvImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CsvImportResult>> Import([FromQuery] string filePath, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _importService.ImportAsync(filePath, cancellationToken));
        }
        catch (CsvImportException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
