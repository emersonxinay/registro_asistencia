using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using registroAsistencia.Data;
using registroAsistencia.Models;
using registroAsistencia.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace registroAsistencia.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ApplicationDbContext _context;

    public AttendanceController(IAttendanceService attendanceService, ApplicationDbContext context)
    {
        _attendanceService = attendanceService;
        _context = context;
    }

    // Obtener resumen de asistencia de una clase
    [HttpGet("class/{claseId:int}")]
    public async Task<IActionResult> GetAttendanceSummary(int claseId)
    {
        try
        {
            var docenteId = GetCurrentUserId();
            
            // Verificar acceso
            var tieneAcceso = await _context.Clases
                .AnyAsync(c => c.Id == claseId && c.DocenteId == docenteId);
            
            if (!tieneAcceso)
            {
                return Forbid();
            }

            var asistencias = await _attendanceService.GetAsistenciasResumenAsync(claseId);
            var clase = await _context.Clases
                .Include(c => c.Ramo)
                .ThenInclude(r => r!.Curso)
                .FirstOrDefaultAsync(c => c.Id == claseId);

            var totalEstudiantes = 0;
            if (clase?.Ramo?.Curso != null)
            {
                totalEstudiantes = await _context.AlumnoCursos
                    .CountAsync(ac => ac.CursoId == clase.Ramo.Curso.Id && ac.Activo);
            }

            var estadisticas = CalcularEstadisticas(asistencias, totalEstudiantes);

            return Ok(new
            {
                ClaseId = claseId,
                ClaseNombre = clase?.Asignatura,
                ClaseActiva = clase?.Activa ?? false,
                TotalEstudiantes = totalEstudiantes,
                Estadisticas = estadisticas,
                Asistencias = asistencias.Select(a => new
                {
                    a.AsistenciaId,
                    a.AlumnoId,
                    a.AlumnoCodigo,
                    a.AlumnoNombre,
                    Estado = a.Estado.ToString(),
                    EstadoNumerico = (int)a.Estado,
                    a.MarcadaUtc,
                    a.MinutosRetraso,
                    Metodo = a.Metodo.ToString(),
                    a.EsRegistroManual,
                    a.JustificacionManual
                })
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Cerrar clase y marcar ausentes
    [HttpPost("class/{claseId:int}/close")]
    public async Task<IActionResult> CloseClass(int claseId)
    {
        try
        {
            var docenteId = GetCurrentUserId();
            
            // Verificar acceso
            var clase = await _context.Clases
                .FirstOrDefaultAsync(c => c.Id == claseId && c.DocenteId == docenteId);
            
            if (clase == null)
            {
                return NotFound("Clase no encontrada o sin acceso");
            }

            if (clase.FinUtc != null)
            {
                return BadRequest("La clase ya está cerrada");
            }

            var success = await _attendanceService.CerrarClaseYMarcarAusentesAsync(claseId);
            
            if (success)
            {
                // Obtener estadísticas actualizadas
                var asistencias = await _attendanceService.GetAsistenciasResumenAsync(claseId);
                var totalEstudiantes = await _context.AlumnoCursos
                    .Include(ac => ac.Curso)
                    .ThenInclude(c => c.Ramos)
                    .CountAsync(ac => ac.Curso.Ramos.Any(r => r.Id == clase.RamoId) && ac.Activo);
                
                var estadisticas = CalcularEstadisticas(asistencias, totalEstudiantes);

                return Ok(new
                {
                    success = true,
                    message = "Clase cerrada exitosamente",
                    estadisticas = estadisticas,
                    totalMarcadosAusentes = estadisticas.Ausentes
                });
            }

            return BadRequest("No se pudo cerrar la clase");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Reabrir clase
    [HttpPost("class/{claseId:int}/reopen")]
    public async Task<IActionResult> ReopenClass(int claseId)
    {
        try
        {
            var docenteId = GetCurrentUserId();
            
            var clase = await _context.Clases
                .FirstOrDefaultAsync(c => c.Id == claseId && c.DocenteId == docenteId);
            
            if (clase == null)
            {
                return NotFound("Clase no encontrada o sin acceso");
            }

            if (clase.FinUtc == null)
            {
                return BadRequest("La clase ya está abierta");
            }

            clase.FinUtc = null;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Clase reabierta exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Modificar asistencia manualmente
    [HttpPost("modify")]
    public async Task<IActionResult> ModifyAttendance([FromBody] ModificarAsistenciaRequest request)
    {
        try
        {
            var docenteId = GetCurrentUserId();

            // Verificar que el docente tiene acceso a esta asistencia
            var asistencia = await _context.Asistencias
                .Include(a => a.Clase)
                .FirstOrDefaultAsync(a => a.Id == request.AsistenciaId);

            if (asistencia == null)
            {
                return NotFound("Registro de asistencia no encontrado");
            }

            if (asistencia.Clase.DocenteId != docenteId)
            {
                return Forbid();
            }

            var success = await _attendanceService.ModificarAsistenciaManualAsync(
                request.AsistenciaId,
                request.NuevoEstado,
                request.Justificacion,
                docenteId);

            if (success)
            {
                return Ok(new { success = true, message = "Asistencia modificada exitosamente" });
            }

            return BadRequest("No se pudo modificar la asistencia");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Registrar asistencia manual excepcional
    [HttpPost("manual")]
    public async Task<IActionResult> RegisterManualAttendance([FromBody] RegistroManualRequest request)
    {
        try
        {
            var docenteId = GetCurrentUserId();

            // Verificar que el docente tiene acceso a la clase
            var clase = await _context.Clases
                .FirstOrDefaultAsync(c => c.Id == request.ClaseId && c.DocenteId == docenteId);

            if (clase == null)
            {
                return NotFound("Clase no encontrada o sin acceso");
            }

            // Verificar que la configuración permite registro manual
            var config = await _attendanceService.GetConfiguracionAsync(request.ClaseId);
            if (!config.PermiteRegistroManual)
            {
                return BadRequest("El registro manual no está habilitado para esta clase");
            }

            var asistencia = await _attendanceService.RegistrarAsistenciaAsync(
                request.AlumnoId,
                request.ClaseId,
                MetodoRegistro.Manual,
                docenteId,
                request.Justificacion);

            return Ok(new
            {
                success = true,
                message = "Asistencia registrada manualmente",
                asistencia = new
                {
                    asistencia.Id,
                    Estado = asistencia.Estado.ToString(),
                    asistencia.MarcadaUtc,
                    asistencia.MinutosRetraso,
                    asistencia.JustificacionManual
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Obtener configuración de asistencia
    [HttpGet("class/{claseId:int}/config")]
    public async Task<IActionResult> GetAttendanceConfig(int claseId)
    {
        try
        {
            var docenteId = GetCurrentUserId();
            
            var tieneAcceso = await _context.Clases
                .AnyAsync(c => c.Id == claseId && c.DocenteId == docenteId);
            
            if (!tieneAcceso)
            {
                return Forbid();
            }

            var config = await _attendanceService.GetConfiguracionAsync(claseId);
            
            return Ok(config);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Actualizar configuración de asistencia
    [HttpPost("class/{claseId:int}/config")]
    public async Task<IActionResult> UpdateAttendanceConfig(int claseId, [FromBody] ConfiguracionAsistencia config)
    {
        try
        {
            var docenteId = GetCurrentUserId();
            
            var tieneAcceso = await _context.Clases
                .AnyAsync(c => c.Id == claseId && c.DocenteId == docenteId);
            
            if (!tieneAcceso)
            {
                return Forbid();
            }

            var updatedConfig = await _attendanceService.CreateOrUpdateConfiguracionAsync(claseId, config);
            
            return Ok(new { success = true, message = "Configuración actualizada", config = updatedConfig });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Dashboard en tiempo real para una clase
    [HttpGet("class/{claseId:int}/live")]
    public async Task<IActionResult> GetLiveDashboard(int claseId)
    {
        try
        {
            var docenteId = GetCurrentUserId();
            
            var clase = await _context.Clases
                .Include(c => c.Ramo)
                .ThenInclude(r => r!.Curso)
                .FirstOrDefaultAsync(c => c.Id == claseId && c.DocenteId == docenteId);
            
            if (clase == null)
            {
                return NotFound();
            }

            var asistencias = await _attendanceService.GetAsistenciasResumenAsync(claseId);
            var totalEstudiantes = await _context.AlumnoCursos
                .CountAsync(ac => ac.CursoId == clase.Ramo!.Curso.Id && ac.Activo);

            var estadisticas = CalcularEstadisticas(asistencias, totalEstudiantes);
            var minutosTranscurridos = (int)(DateTime.UtcNow - clase.InicioUtc).TotalMinutes;

            // Obtener estudiantes pendientes (sin registrar)
            var alumnosConAsistencia = asistencias.Select(a => a.AlumnoId).ToHashSet();
            var estudiantesPendientes = await _context.AlumnoCursos
                .Where(ac => ac.CursoId == clase.Ramo.Curso.Id && ac.Activo)
                .Include(ac => ac.Alumno)
                .Where(ac => !alumnosConAsistencia.Contains(ac.AlumnoId))
                .Select(ac => new { ac.Alumno.Id, ac.Alumno.Codigo, ac.Alumno.Nombre })
                .ToListAsync();

            return Ok(new
            {
                ClaseInfo = new
                {
                    Id = clase.Id,
                    Nombre = clase.Asignatura,
                    CursoNombre = clase.Ramo.Curso.Nombre,
                    RamoNombre = clase.Ramo.Nombre,
                    InicioUtc = clase.InicioUtc,
                    MinutosTranscurridos = minutosTranscurridos,
                    Activa = clase.Activa
                },
                Estadisticas = estadisticas,
                EstudiantesPendientes = estudiantesPendientes,
                UltimasAsistencias = asistencias.OrderByDescending(a => a.MarcadaUtc).Take(10)
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Métodos auxiliares
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 1;
    }

    private static EstadisticasAsistencia CalcularEstadisticas(IEnumerable<AsistenciaResumen> asistencias, int totalEstudiantes)
    {
        var asistenciasList = asistencias.ToList();
        var presentes = asistenciasList.Count(a => a.Estado == EstadoAsistencia.Presente);
        var tardanzas = asistenciasList.Count(a => a.Estado == EstadoAsistencia.Tardanza);
        var excusados = asistenciasList.Count(a => a.Estado == EstadoAsistencia.Excusado);
        var ausentes = asistenciasList.Count(a => a.Estado == EstadoAsistencia.Ausente);
        var pendientes = totalEstudiantes - asistenciasList.Count;

        return new EstadisticasAsistencia
        {
            TotalEstudiantes = totalEstudiantes,
            Presentes = presentes,
            Tardanzas = tardanzas,
            Ausentes = ausentes,
            Excusados = excusados,
            Pendientes = pendientes,
            PorcentajeAsistencia = totalEstudiantes > 0 ? Math.Round((double)(presentes + tardanzas + excusados) / totalEstudiantes * 100, 1) : 0
        };
    }
}

// ViewModels y DTOs
public class ModificarAsistenciaRequest
{
    public int AsistenciaId { get; set; }
    public EstadoAsistencia NuevoEstado { get; set; }
    
    [Required(ErrorMessage = "La justificación es requerida")]
    [StringLength(500, ErrorMessage = "La justificación no puede exceder 500 caracteres")]
    public string Justificacion { get; set; } = "";
}

public class RegistroManualRequest
{
    public int AlumnoId { get; set; }
    public int ClaseId { get; set; }
    
    [Required(ErrorMessage = "La justificación es requerida para registro manual")]
    [StringLength(500, ErrorMessage = "La justificación no puede exceder 500 caracteres")]
    public string Justificacion { get; set; } = "";
}

public class EstadisticasAsistencia
{
    public int TotalEstudiantes { get; set; }
    public int Presentes { get; set; }
    public int Tardanzas { get; set; }
    public int Ausentes { get; set; }
    public int Excusados { get; set; }
    public int Pendientes { get; set; }
    public double PorcentajeAsistencia { get; set; }
}