using Kolos.DTOs;
using Kolos.Exceptions;
using Kolos.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kolos.Controllers;

[ApiController]
[Route("politycy")]
public class PolitykController(IDbService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPolitycyDetails()
    {
        return Ok(await service.GetPolitycyDetailsAsync());
    }

    [HttpPost]
    public async Task<IActionResult> CreatePolityk
        ([FromBody] PoliticianCreateDto polityk)
    {
        try
        {
            var pol = await service.CreatePolitykAsync(polityk);
            return Created($"politycy/{pol.Id}", pol);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}
