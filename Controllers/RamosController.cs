using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using registroAsistencia.Data;
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
    private readonly ApplicationDbContext _context;

    public RamosController(IDataService dataService, ILogger<RamosController> logger, ApplicationDbContext context)
    {
        _dataService = dataService;
        _logger = logger;
        _context = context;
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
        var userId = GetCurrentUserId();

        // Si es Admin, devolver todos los ramos del curso
        if (IsAdmin())
        {
            var ramos = await _dataService.GetRamosByCursoAsync(cursoId);
            return Ok(ramos);
        }

        // Si es Docente, filtrar solo los ramos que puede enseñar
        var misRamos = await _context.DocenteRamos
            .Where(dr => dr.DocenteId == userId && dr.Activo)
            .Include(dr => dr.Ramo)
            .Where(dr => dr.Ramo.CursoId == cursoId && dr.Ramo.Activo)
            .Select(dr => dr.Ramo)
            .ToListAsync();

        return Ok(misRamos);
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

    // Método helper para obtener el ID del usuario actual
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 1;
    }

    // Método helper para verificar si el usuario es Admin
    private bool IsAdmin()
    {
        return User.IsInRole("Administrador");
    }
}