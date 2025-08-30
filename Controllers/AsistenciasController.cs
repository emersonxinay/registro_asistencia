using Microsoft.AspNetCore.Mvc;
using registroAsistencia.Models;
using registroAsistencia.Services;

namespace registroAsistencia.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> AlumnoScan([FromBody] AlumnoScanDto dto)
    {
        try
        {
            var clase = await _dataService.GetClaseAsync(dto.ClaseId);
            if (clase == null)
                return BadRequest($"La clase {dto.ClaseId} no existe");
            
            if (!clase.Activa)
                return BadRequest("La clase no está activa");

            var alumno = await _dataService.GetAlumnoAsync(dto.AlumnoId);
            if (alumno == null)
                return BadRequest($"El alumno con ID {dto.AlumnoId} no existe. Verifica tu ID de alumno.");

            if (!await _dataService.ValidarTokenAsync(dto.Nonce, dto.ClaseId))
                return BadRequest("Token QR inválido o expirado. Escanea nuevamente el código QR.");

            await _dataService.ConsumeTokenAsync(dto.Nonce);

            if (await _dataService.ExisteAsistenciaAsync(dto.AlumnoId, dto.ClaseId))
                return Ok(new { mensaje = $"¡Hola {alumno.Nombre}! Tu asistencia ya fue registrada anteriormente." });

            await _dataService.RegistrarAsistenciaAsync(dto.AlumnoId, dto.ClaseId, "ALUMNO_ESCANEA");
            return Ok(new { mensaje = $"¡Perfecto {alumno.Nombre}! Tu asistencia ha sido registrada exitosamente." });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error al procesar la solicitud: {ex.Message}");
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
        var asistencias = await _dataService.GetAsistenciasPorClaseAsync(claseId);
        return Ok(asistencias);
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
}