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
            var esClaseLibre = clase?.RamoId == null;

            if (!esClaseLibre && clase?.Ramo?.Curso != null)
            {
                // Clase con curso asignado
                totalEstudiantes = await _context.AlumnoCursos
                    .CountAsync(ac => ac.CursoId == clase.Ramo.Curso.Id && ac.Activo);
            }
            else
            {
                // Clase libre - usar el número de estudiantes que han registrado asistencia
                totalEstudiantes = asistencias.Count();
            }

            var estadisticas = CalcularEstadisticas(asistencias, totalEstudiantes);

            return Ok(new
            {
                ClaseId = claseId,
                ClaseNombre = clase?.Asignatura,
                ClaseActiva = clase?.Activa ?? false,
                EsClaseLibre = esClaseLibre,
                CursoNombre = clase?.Ramo?.Curso?.Nombre,
                RamoNombre = clase?.Ramo?.Nombre,
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
                return BadRequest(new { message = "La clase ya está cerrada" });
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

            return BadRequest(new { message = "No se pudo cerrar la clase" });
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
                return BadRequest(new { message = "La clase ya está abierta" });
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

            return BadRequest(new { message = "No se pudo modificar la asistencia" });
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
                return BadRequest(new { message = "El registro manual no está habilitado para esta clase" });
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
            var esClaseLibre = clase.RamoId == null;
            var totalEstudiantes = 0;
            var estudiantesPendientes = new List<object>();

            if (!esClaseLibre && clase.Ramo?.Curso != null)
            {
                // Clase con curso asignado
                totalEstudiantes = await _context.AlumnoCursos
                    .CountAsync(ac => ac.CursoId == clase.Ramo.Curso.Id && ac.Activo);

                // Obtener estudiantes pendientes (sin registrar)
                var alumnosConAsistencia = asistencias.Select(a => a.AlumnoId).ToHashSet();
                estudiantesPendientes = await _context.AlumnoCursos
                    .Where(ac => ac.CursoId == clase.Ramo.Curso.Id && ac.Activo)
                    .Include(ac => ac.Alumno)
                    .Where(ac => !alumnosConAsistencia.Contains(ac.AlumnoId))
                    .Select(ac => new { ac.Alumno.Id, ac.Alumno.Codigo, ac.Alumno.Nombre })
                    .ToListAsync<object>();
            }
            else
            {
                // Clase libre - solo mostrar los que han registrado asistencia
                totalEstudiantes = asistencias.Count();
                estudiantesPendientes = new List<object>(); // No hay pendientes en clases libres
            }

            var estadisticas = CalcularEstadisticas(asistencias, totalEstudiantes);
            var minutosTranscurridos = (int)(DateTime.UtcNow - clase.InicioUtc).TotalMinutes;

            return Ok(new
            {
                ClaseInfo = new
                {
                    Id = clase.Id,
                    Nombre = clase.Asignatura,
                    CursoNombre = clase.Ramo?.Curso?.Nombre,
                    RamoNombre = clase.Ramo?.Nombre,
                    EsClaseLibre = esClaseLibre,
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

    // PRIORIDAD: Endpoint para scanner de docente que escanea QR de estudiantes
    [HttpPost("scan-student-qr")]
    public async Task<IActionResult> ScanStudentQr([FromBody] ScanStudentQrRequest request)
    {
        try
        {
            var docenteId = GetCurrentUserId();

            // Verificar acceso a la clase
            var clase = await _context.Clases
                .FirstOrDefaultAsync(c => c.Id == request.ClaseId && c.DocenteId == docenteId);

            if (clase == null)
            {
                return Forbid("No tienes acceso a esta clase");
            }

            if (!clase.Activa)
            {
                return BadRequest(new { message = "La clase no está activa" });
            }

            // Parsear el QR del estudiante - soportar múltiples formatos
            QrStudentPayload? qrData = null;

            // Intentar deserializar como JSON
            try
            {
                qrData = System.Text.Json.JsonSerializer.Deserialize<QrStudentPayload>(request.QrData);
            }
            catch
            {
                // Si falla el JSON, intentar como formato simple "STUDENT:ID:CODIGO"
                try
                {
                    var parts = request.QrData.Split(':');
                    if (parts.Length >= 3 && parts[0] == "STUDENT")
                    {
                        qrData = new QrStudentPayload
                        {
                            type = "student",
                            studentId = int.Parse(parts[1]),
                            studentCode = parts[2],
                            timestamp = DateTime.UtcNow.ToString("O"),
                            version = "1.0"
                        };
                    }
                }
                catch
                {
                    return BadRequest(new { message = "QR de estudiante inválido - formato no reconocido" });
                }
            }

            if (qrData == null || qrData.type != "student")
            {
                return BadRequest(new { message = "QR no es de un estudiante válido" });
            }

            // Verificar que el estudiante existe
            var alumno = await _context.Alumnos
                .FirstOrDefaultAsync(a => a.Id == qrData.studentId && a.Codigo == qrData.studentCode);

            if (alumno == null)
            {
                return BadRequest(new { message = $"Estudiante no encontrado: {qrData.studentCode}" });
            }

            // Verificar que no haya asistencia previa
            var asistenciaExistente = await _context.Asistencias
                .FirstOrDefaultAsync(a => a.ClaseId == request.ClaseId && a.AlumnoId == alumno.Id);

            if (asistenciaExistente != null)
            {
                return Ok(new
                {
                    success = true,
                    message = $"El estudiante {alumno.Nombre} ya tiene asistencia registrada",
                    asistencia = new
                    {
                        id = asistenciaExistente.Id,
                        alumnoId = alumno.Id,
                        alumnoCodigo = alumno.Codigo,
                        alumnoNombre = alumno.Nombre,
                        estado = asistenciaExistente.Estado.ToString(),
                        marcadaUtc = asistenciaExistente.MarcadaUtc,
                        minutosRetraso = asistenciaExistente.MinutosRetraso,
                        metodo = "YA_REGISTRADO"
                    }
                });
            }

            // Registrar asistencia con método QrDocente
            var asistencia = await _attendanceService.RegistrarAsistenciaAsync(
                alumno.Id,
                request.ClaseId,
                MetodoRegistro.QrDocente,
                docenteId,
                $"Escaneado por docente desde código QR físico del estudiante {alumno.Codigo}"
            );

            return Ok(new
            {
                success = true,
                message = $"Asistencia registrada exitosamente para {alumno.Nombre}",
                asistencia = new
                {
                    id = asistencia.Id,
                    alumnoId = alumno.Id,
                    alumnoCodigo = alumno.Codigo,
                    alumnoNombre = alumno.Nombre,
                    estado = asistencia.Estado.ToString(),
                    marcadaUtc = asistencia.MarcadaUtc,
                    minutosRetraso = asistencia.MinutosRetraso,
                    metodo = "QR_DOCENTE_ESCANEA_ESTUDIANTE"
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno: " + ex.Message });
        }
    }

    // Generar QRs físicos para estudiantes de una clase
    [HttpGet("class/{claseId:int}/student-qrs")]
    public async Task<IActionResult> GenerateStudentQrs(int claseId)
    {
        try
        {
            var docenteId = GetCurrentUserId();

            // Verificar acceso a la clase
            var clase = await _context.Clases
                .Include(c => c.Ramo)
                .ThenInclude(r => r!.Curso)
                .FirstOrDefaultAsync(c => c.Id == claseId && c.DocenteId == docenteId);

            if (clase == null)
            {
                return NotFound("Clase no encontrada o sin acceso");
            }

            var esClaseLibre = clase.RamoId == null;
            var alumnosData = new List<dynamic>();

            if (!esClaseLibre && clase.Ramo?.Curso != null)
            {
                // Clase con curso asignado - obtener estudiantes del curso
                alumnosData = await _context.AlumnoCursos
                    .Where(ac => ac.CursoId == clase.Ramo.Curso.Id && ac.Activo)
                    .Include(ac => ac.Alumno)
                    .Select(ac => new { ac.Alumno.Id, ac.Alumno.Codigo, ac.Alumno.Nombre })
                    .OrderBy(a => a.Codigo)
                    .ToListAsync<dynamic>();
            }
            else
            {
                // Clase libre - obtener todos los alumnos del sistema para poder escanear cualquiera
                alumnosData = await _context.Alumnos
                    .Select(a => new { a.Id, Codigo = a.Codigo, Nombre = a.Nombre })
                    .OrderBy(a => a.Codigo)
                    .ToListAsync<dynamic>();
            }

            var currentTimestamp = DateTime.UtcNow.ToString("O");
            var estudiantes = alumnosData.Select(a => new StudentQrData
            {
                Id = a.Id,
                Codigo = a.Codigo,
                Nombre = a.Nombre,
                QrDataJson = System.Text.Json.JsonSerializer.Serialize(new QrStudentPayload
                {
                    type = "student",
                    studentId = a.Id,
                    studentCode = a.Codigo,
                    timestamp = currentTimestamp,
                    version = "1.0"
                }),
                QrDataSimple = $"STUDENT:{a.Id}:{a.Codigo}"
            }).ToList();

            return Ok(new
            {
                claseId = claseId,
                claseNombre = clase.Asignatura,
                cursoNombre = clase.Ramo?.Curso?.Nombre,
                ramoNombre = clase.Ramo?.Nombre,
                esClaseLibre = esClaseLibre,
                totalEstudiantes = estudiantes.Count,
                estudiantes = estudiantes
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // API para obtener clases activas (para la vista de QR Students)
    [HttpGet("/api/clases/activas")]
    public async Task<IActionResult> GetClasesActivas()
    {
        try
        {
            var docenteId = GetCurrentUserId();

            var clasesActivas = await _context.Clases
                .Where(c => c.DocenteId == docenteId && c.Activa)
                .Include(c => c.Ramo)
                .ThenInclude(r => r!.Curso)
                .Select(c => new
                {
                    id = c.Id,
                    nombre = c.Asignatura,
                    ramoNombre = c.Ramo != null ? c.Ramo.Nombre : null,
                    cursoNombre = c.Ramo != null && c.Ramo.Curso != null ? c.Ramo.Curso.Nombre : null,
                    esClaseLibre = c.RamoId == null,
                    tipoClase = c.RamoId == null ? "Clase Libre" : "Clase con Curso",
                    inicioUtc = c.InicioUtc,
                    minutosTranscurridos = (int)(DateTime.UtcNow - c.InicioUtc).TotalMinutes
                })
                .OrderByDescending(c => c.inicioUtc)
                .ToListAsync();

            return Ok(clasesActivas);
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

// PRIORIDAD: DTOs para scanner de docente
public class ScanStudentQrRequest
{
    public int ClaseId { get; set; }
    public string QrData { get; set; } = "";
}

public class QrStudentPayload
{
    public string type { get; set; } = "";
    public int studentId { get; set; }
    public string studentCode { get; set; } = "";
    public string timestamp { get; set; } = "";
    public string version { get; set; } = "";
}

public class StudentQrData
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string QrDataJson { get; set; } = "";
    public string QrDataSimple { get; set; } = "";
}