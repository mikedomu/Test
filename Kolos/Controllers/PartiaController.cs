using Kolos.DTOs;
using Kolos.Exceptions;
using Kolos.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Kolos.Controllers;
[ApiController]
[Route("partie")]
public class PartiaController(IDbService service):ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreatePartia([FromBody] PartiaCreateDto partia)
    {
        try
        {
            var party = await service.CreatePartiaAsync(partia);
            return Created($"partie/{party.Id}", party);

        }catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}