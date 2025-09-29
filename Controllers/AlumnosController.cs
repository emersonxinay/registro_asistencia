using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using registroAsistencia.Models;
using registroAsistencia.Services;

namespace registroAsistencia.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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

    // Generar QR del estudiante para que docente escanee
    [HttpGet("{id}/qr")]
    public async Task<IActionResult> GetStudentQr(int id)
    {
        try
        {
            var alumno = await _dataService.GetAlumnoAsync(id);
            if (alumno == null)
                return NotFound(new { message = "Estudiante no encontrado" });

            // Crear payload para QR de estudiante
            var qrPayload = new
            {
                type = "student",
                studentId = alumno.Id,
                studentCode = alumno.Codigo,
                timestamp = DateTime.UtcNow.ToString("O"),
                version = "1.0"
            };

            var qrData = System.Text.Json.JsonSerializer.Serialize(qrPayload);
            var base64Qr = _qrService.GenerateBase64Qr(qrData);

            return Ok(new
            {
                qrCode = base64Qr,
                studentInfo = new
                {
                    id = alumno.Id,
                    codigo = alumno.Codigo,
                    nombre = alumno.Nombre
                },
                payload = qrData
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Error generando QR: " + ex.Message });
        }
    }

    // Generar QR PNG para imprimir
    [HttpGet("{id}/qr.png")]
    public async Task<IActionResult> GetStudentQrPng(int id)
    {
        try
        {
            var alumno = await _dataService.GetAlumnoAsync(id);
            if (alumno == null)
                return NotFound();

            var qrPayload = new
            {
                type = "student",
                studentId = alumno.Id,
                studentCode = alumno.Codigo,
                timestamp = DateTime.UtcNow.ToString("O"),
                version = "1.0"
            };

            var qrData = System.Text.Json.JsonSerializer.Serialize(qrPayload);
            var pngBytes = _qrService.GeneratePngBytes(qrData);

            return File(pngBytes, "image/png", $"qr-estudiante-{alumno.Codigo}.png");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Error generando QR PNG: " + ex.Message });
        }
    }
}