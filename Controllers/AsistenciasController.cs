using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using registroAsistencia.Models;
using registroAsistencia.Services;

namespace registroAsistencia.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AsistenciasController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly ICsvService _csvService;

    public AsistenciasController(IDataService dataService, ICsvService csvService)
    {
        _dataService = dataService;
        _csvService = csvService;
    }

    [HttpPost("profesor-scan")]
    public async Task<IActionResult> ProfesorScan([FromBody] ProfesorScanDto dto)
    {
        var clase = await _dataService.GetClaseAsync(dto.ClaseId);
        if (clase == null)
            return NotFound("Clase no existe");
        
        if (!clase.Activa)
            return BadRequest("Clase no activa");

        var alumno = await _dataService.GetAlumnoAsync(dto.AlumnoId);
        if (alumno == null)
            return NotFound("Alumno no existe");

        if (await _dataService.ExisteAsistenciaAsync(dto.AlumnoId, dto.ClaseId))
            return Ok(new { mensaje = "Asistencia ya registrada" });

        await _dataService.RegistrarAsistenciaAsync(dto.AlumnoId, dto.ClaseId, "PROFESOR_ESCANEA");
        return Ok(new { mensaje = "Asistencia registrada (profesor escanea)" });
    }

    [HttpPost("alumno-scan")]
    [AllowAnonymous]
    public async Task<IActionResult> AlumnoScan([FromBody] AlumnoScanDto dto)
    {
        try
        {
            var clase = await _dataService.GetClaseAsync(dto.ClaseId);
            if (clase == null)
                return BadRequest(new { message = $"La clase {dto.ClaseId} no existe" });

            if (!clase.Activa)
                return BadRequest(new { message = "La clase no está activa" });

            var alumno = await _dataService.GetAlumnoAsync(dto.AlumnoId);
            if (alumno == null)
                return BadRequest(new { message = $"El alumno con ID {dto.AlumnoId} no existe. Verifica tu ID de alumno." });

            if (!await _dataService.ValidarTokenAsync(dto.Nonce, dto.ClaseId))
                return BadRequest(new { message = "Token QR inválido o expirado. Escanea nuevamente el código QR." });

            await _dataService.ConsumeTokenAsync(dto.Nonce);

            if (await _dataService.ExisteAsistenciaAsync(dto.AlumnoId, dto.ClaseId))
                return Ok(new { mensaje = $"¡Hola {alumno.Nombre}! Tu asistencia ya fue registrada anteriormente." });

            await _dataService.RegistrarAsistenciaAsync(dto.AlumnoId, dto.ClaseId, "ALUMNO_ESCANEA");
            return Ok(new { mensaje = $"¡Perfecto {alumno.Nombre}! Tu asistencia ha sido registrada exitosamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error al procesar la solicitud: {ex.Message}" });
        }
    }

    // FUTURO: Endpoint para estudiantes autenticados (registro automático)
    [HttpPost("alumno-scan-auto")]
    [Authorize] // Requiere autenticación de estudiante cuando se implemente
    public async Task<IActionResult> AlumnoScanAuto([FromBody] AlumnoScanAutoDto dto)
    {
        try
        {
            // Obtener ID del estudiante desde la autenticación
            var alumnoIdFromAuth = GetCurrentStudentId(); // Método a implementar cuando haya auth de estudiantes

            if (alumnoIdFromAuth == null)
                return BadRequest(new { message = "No se pudo identificar al estudiante autenticado" });

            var clase = await _dataService.GetClaseAsync(dto.ClaseId);
            if (clase == null)
                return BadRequest(new { message = $"La clase {dto.ClaseId} no existe" });

            if (!clase.Activa)
                return BadRequest(new { message = "La clase no está activa" });

            if (!await _dataService.ValidarTokenAsync(dto.Nonce, dto.ClaseId))
                return BadRequest(new { message = "Token QR inválido o expirado. Escanea nuevamente el código QR." });

            await _dataService.ConsumeTokenAsync(dto.Nonce);

            var alumno = await _dataService.GetAlumnoAsync(alumnoIdFromAuth.Value);
            if (alumno == null)
                return BadRequest(new { message = "Estudiante no encontrado en el sistema" });

            if (await _dataService.ExisteAsistenciaAsync(alumnoIdFromAuth.Value, dto.ClaseId))
                return Ok(new {
                    mensaje = $"¡Hola {alumno.Nombre}! Tu asistencia ya fue registrada anteriormente.",
                    automatic = true
                });

            await _dataService.RegistrarAsistenciaAsync(alumnoIdFromAuth.Value, dto.ClaseId, "ALUMNO_ESCANEA_AUTO");
            return Ok(new {
                mensaje = $"¡Perfecto {alumno.Nombre}! Tu asistencia ha sido registrada automáticamente.",
                automatic = true,
                studentInfo = new {
                    id = alumno.Id,
                    nombre = alumno.Nombre,
                    codigo = alumno.Codigo
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Error al procesar la solicitud automática: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var asistencias = await _dataService.GetAsistenciasAsync();
        return Ok(asistencias);
    }

    [HttpGet("clase/{claseId}")]
    [HttpGet("/api/clases/{claseId}/asistencias")] // Ruta alternativa para compatibilidad
    public async Task<IActionResult> GetByClase(int claseId)
    {
        try
        {
            var asistencias = await _dataService.GetAsistenciasPorClaseAsync(claseId);
            return Ok(asistencias);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener asistencias: " + ex.Message });
        }
    }

    [HttpGet("csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var asistencias = await _dataService.GetAsistenciasAsync();
        var bytes = _csvService.GenerateAsistenciasCsv(asistencias);
        return File(bytes, "text/csv; charset=utf-8", "asistencias.csv");
    }

    [HttpGet("clase/{claseId}/csv")]
    [HttpGet("/api/clases/{claseId}/asistencias.csv")] // Ruta alternativa para compatibilidad
    public async Task<IActionResult> ExportCsvByClase(int claseId)
    {
        var asistencias = await _dataService.GetAsistenciasPorClaseAsync(claseId);
        var bytes = _csvService.GenerateAsistenciasCsv(asistencias);
        return File(bytes, "text/csv; charset=utf-8", $"asistencias-clase-{claseId}.csv");
    }

    [HttpGet("clase/{claseId}/csv-completo")]
    public async Task<IActionResult> ExportCsvCompletoByClase(int claseId)
    {
        // Obtener información de la clase
        var clase = await _dataService.GetClaseAsync(claseId);
        if (clase == null)
            return NotFound("Clase no encontrada");

        // Obtener asistencias de la clase
        var asistenciasClase = await _dataService.GetAsistenciasPorClaseAsync(claseId);

        // Obtener todos los estudiantes del curso
        if (clase.RamoId.HasValue)
        {
            var ramo = await _dataService.GetRamoAsync(clase.RamoId.Value);
            if (ramo != null)
            {
                var estudiantesCurso = await _dataService.GetAlumnosCursoAsync(ramo.CursoId);
                var bytes = _csvService.GenerateAsistenciasCompletoCsv(clase, estudiantesCurso, asistenciasClase);
                return File(bytes, "text/csv; charset=utf-8", $"asistencias-completa-clase-{claseId}.csv");
            }
        }

        // Fallback: usar el CSV normal si no hay información del ramo
        var normalBytes = _csvService.GenerateAsistenciasCsv(asistenciasClase);
        return File(normalBytes, "text/csv; charset=utf-8", $"asistencias-clase-{claseId}.csv");
    }

    // FUTURO: Método para obtener ID de estudiante autenticado
    // Este método deberá implementarse cuando se agregue el sistema de autenticación de estudiantes
    private int? GetCurrentStudentId()
    {
        // TODO: Implementar cuando se agregue autenticación de estudiantes
        // Ejemplo de implementación futura:
        // var studentIdClaim = User.FindFirst("StudentId")?.Value;
        // return int.TryParse(studentIdClaim, out int studentId) ? studentId : null;

        return null; // Por ahora retorna null hasta que se implemente auth de estudiantes
    }
}

// DTO para registro automático de estudiantes autenticados
public class AlumnoScanAutoDto
{
    public int ClaseId { get; set; }
    public string Nonce { get; set; } = "";
    // No necesita AlumnoId porque se obtiene de la autenticación
}