using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using registroAsistencia.Models;
using registroAsistencia.Services;

namespace registroAsistencia.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RamosController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly ILogger<RamosController> _logger;

    public RamosController(IDataService dataService, ILogger<RamosController> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ramo>>> GetRamos()
    {
        var ramos = await _dataService.GetRamosAsync();
        return Ok(ramos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Ramo>> GetRamo(int id)
    {
        var ramo = await _dataService.GetRamoAsync(id);
        if (ramo == null)
            return NotFound();
        
        return Ok(ramo);
    }

    [HttpGet("curso/{cursoId}")]
    public async Task<ActionResult<IEnumerable<Ramo>>> GetRamosByCurso(int cursoId)
    {
        var ramos = await _dataService.GetRamosByCursoAsync(cursoId);
        return Ok(ramos);
    }

    [HttpPost]
    public async Task<ActionResult<Ramo>> CreateRamo([FromBody] RamoCreateDto dto)
    {
        try
        {
            var ramo = await _dataService.CreateRamoAsync(dto);
            return CreatedAtAction(nameof(GetRamo), new { id = ramo.Id }, ramo);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRamo(int id, [FromBody] RamoUpdateDto dto)
    {
        try
        {
            var success = await _dataService.UpdateRamoAsync(id, dto);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRamo(int id)
    {
        var success = await _dataService.DeleteRamoAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }
}