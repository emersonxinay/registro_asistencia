using Microsoft.AspNetCore.Mvc;
using registroAsistencia.Models;
using registroAsistencia.Services;

namespace registroAsistencia.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlumnosController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly IQrService _qrService;

    public AlumnosController(IDataService dataService, IQrService qrService)
    {
        _dataService = dataService;
        _qrService = qrService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AlumnoCreateDto dto)
    {
        try
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Codigo) || string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return BadRequest(new { message = "Código y nombre son requeridos" });
            }
            
            var alumno = await _dataService.CreateAlumnoAsync(dto);
            return Ok(alumno);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var alumnos = await _dataService.GetAlumnosAsync();
            return Ok(alumnos ?? new List<Alumno>());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener alumnos: " + ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var alumno = await _dataService.GetAlumnoAsync(id);
        if (alumno == null)
            return NotFound();
        return Ok(alumno);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] AlumnoCreateDto dto)
    {
        try
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Codigo) || string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return BadRequest(new { message = "Código y nombre son requeridos" });
            }
            
            var updated = await _dataService.UpdateAlumnoAsync(id, dto);
            if (!updated)
                return NotFound(new { message = "Alumno no encontrado" });
            
            var alumno = await _dataService.GetAlumnoAsync(id);
            return Ok(alumno);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _dataService.DeleteAlumnoAsync(id);
        if (!deleted)
            return NotFound();
        
        return NoContent();
    }
}