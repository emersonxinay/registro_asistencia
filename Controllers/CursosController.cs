using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using registroAsistencia.Models;
using registroAsistencia.Services;

namespace registroAsistencia.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CursosController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly ILogger<CursosController> _logger;

    public CursosController(IDataService dataService, ILogger<CursosController> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Curso>>> GetCursos()
    {
        var cursos = await _dataService.GetCursosAsync();
        return Ok(cursos);
    }

    [HttpGet("mis-cursos")]
    public async Task<ActionResult<IEnumerable<Curso>>> GetMisCursos()
    {
        try
        {
            var userId = GetCurrentUserId();
            var todosCursos = await _dataService.GetCursosAsync();
            // Por ahora retornar todos los cursos, pero esto debería filtrarse por docente
            // cuando se implemente la relación DocenteCurso en el DataService
            return Ok(todosCursos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener mis cursos: " + ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Curso>> GetCurso(int id)
    {
        var curso = await _dataService.GetCursoAsync(id);
        if (curso == null)
            return NotFound();
        
        return Ok(curso);
    }

    [HttpPost]
    public async Task<ActionResult<Curso>> CreateCurso([FromBody] CursoCreateDto dto)
    {
        try
        {
            var curso = await _dataService.CreateCursoAsync(dto);
            return CreatedAtAction(nameof(GetCurso), new { id = curso.Id }, curso);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCurso(int id, [FromBody] CursoUpdateDto dto)
    {
        var success = await _dataService.UpdateCursoAsync(id, dto);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCurso(int id)
    {
        var success = await _dataService.DeleteCursoAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }

    // Endpoints específicos para gestión de estudiantes
    [HttpGet("{cursoId}/alumnos")]
    public async Task<ActionResult<IEnumerable<AlumnoCursoDto>>> GetAlumnosCurso(int cursoId)
    {
        var alumnos = await _dataService.GetAlumnosCursoAsync(cursoId);
        return Ok(alumnos);
    }

    [HttpPost("{cursoId}/alumnos")]
    public async Task<ActionResult<AlumnoCurso>> InscribirAlumno(int cursoId, [FromBody] AlumnoCursoCreateDto dto)
    {
        if (dto.CursoId != cursoId)
            return BadRequest("El ID del curso no coincide");

        try
        {
            var inscripcion = await _dataService.InscribirAlumnoEnCursoAsync(dto);
            return CreatedAtAction(nameof(GetAlumnosCurso), new { cursoId }, inscripcion);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{cursoId}/alumnos/{alumnoId}")]
    public async Task<IActionResult> DesinscribirAlumno(int cursoId, int alumnoId)
    {
        var success = await _dataService.DesinscribirAlumnoDelCursoAsync(alumnoId, cursoId);
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
}