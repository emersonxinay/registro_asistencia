using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public async Task<IActionResult> ProfesorScan([FromBody] ProfesorScanDto dto)
    {
        // Verificar acceso a la clase
        var userId = GetCurrentUserId();
        if (!await TieneAccesoClase(dto.ClaseId, userId))
        {
            return Forbid();
        }

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
            // Validar clase
            var clase = await _dataService.GetClaseAsync(dto.ClaseId);
            if (clase == null)
                return BadRequest(new {
                    success = false,
                    message = "La clase no existe. Verifica el código QR escaneado."
                });

            if (!clase.Activa)
                return BadRequest(new {
                    success = false,
                    message = $"La clase '{clase.Asignatura}' ya fue cerrada. No se pueden registrar más asistencias."
                });

            // Validar alumno
            var alumno = await _dataService.GetAlumnoAsync(dto.AlumnoId);
            if (alumno == null)
                return BadRequest(new {
                    success = false,
                    message = $"Estudiante no encontrado (ID: {dto.AlumnoId}). Verifica tu código QR personal."
                });

            // Validar token (pero NO consumir aún)
            if (!await _dataService.ValidarTokenAsync(dto.Nonce, dto.ClaseId))
                return BadRequest(new {
                    success = false,
                    message = "El código QR ha expirado o es inválido. Solicita un nuevo código QR al profesor."
                });

            // Verificar si ya está registrado
            if (await _dataService.ExisteAsistenciaAsync(dto.AlumnoId, dto.ClaseId))
            {
                // Consumir token después de confirmar que todo está OK
                await _dataService.ConsumeTokenAsync(dto.Nonce);

                return Ok(new {
                    success = true,
                    yaRegistrado = true,
                    mensaje = $"¡Hola {alumno.Nombre}! Tu asistencia ya fue registrada anteriormente en esta clase."
                });
            }

            // Intentar registrar asistencia
            var registroExitoso = await _dataService.RegistrarAsistenciaAsync(dto.AlumnoId, dto.ClaseId, "ALUMNO_ESCANEA");

            if (!registroExitoso)
                return BadRequest(new {
                    success = false,
                    message = "No se pudo registrar la asistencia. Por favor intenta nuevamente o contacta al profesor."
                });

            // Solo consumir token después de registro exitoso
            await _dataService.ConsumeTokenAsync(dto.Nonce);

            return Ok(new {
                success = true,
                yaRegistrado = false,
                mensaje = $"¡Perfecto {alumno.Nombre}! Tu asistencia ha sido registrada exitosamente en '{clase.Asignatura}'."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new {
                success = false,
                message = "Error del servidor al procesar tu registro. Por favor intenta nuevamente.",
                detalle = ex.Message
            });
        }
    }

    [HttpPost("alumno-scan-codigo")]
    [AllowAnonymous]
    public async Task<IActionResult> AlumnoScanByCodigo([FromBody] AlumnoScanByCodigoDto dto)
    {
        try
        {
            // Validar clase
            var clase = await _dataService.GetClaseAsync(dto.ClaseId);
            if (clase == null)
                return BadRequest(new {
                    success = false,
                    message = "La clase no existe. Verifica el código QR escaneado."
                });

            if (!clase.Activa)
                return BadRequest(new {
                    success = false,
                    message = $"La clase '{clase.Asignatura}' ya fue cerrada. No se pueden registrar más asistencias."
                });

            // Validar alumno por código
            var alumno = await _dataService.GetAlumnoByCodigoAsync(dto.CodigoAlumno);
            if (alumno == null)
                return BadRequest(new {
                    success = false,
                    message = $"Estudiante con código '{dto.CodigoAlumno}' no encontrado. Verifica tu código de estudiante."
                });

            // Validar token (pero NO consumir aún)
            if (!await _dataService.ValidarTokenAsync(dto.Nonce, dto.ClaseId))
                return BadRequest(new {
                    success = false,
                    message = "El código QR ha expirado o es inválido. Solicita un nuevo código QR al profesor."
                });

            // Verificar si ya existe asistencia
            var yaRegistrado = await _dataService.ExisteAsistenciaAsync(alumno.Id, dto.ClaseId);

            if (yaRegistrado)
            {
                // Consumir token después de confirmar que todo está OK
                await _dataService.ConsumeTokenAsync(dto.Nonce);

                // Obtener la asistencia existente para mostrar el estado
                var asistencias = await _dataService.GetAsistenciasPorClaseAsync(dto.ClaseId);
                var miAsistencia = asistencias.FirstOrDefault(a => a.AlumnoId == alumno.Id);

                return Ok(new {
                    success = true,
                    yaRegistrado = true,
                    mensaje = $"¡Hola {alumno.Nombre}! Tu asistencia ya fue registrada anteriormente en esta clase.",
                    estudiante = new {
                        codigo = alumno.Codigo,
                        nombre = alumno.Nombre,
                        estado = miAsistencia?.Estado.ToString() ?? "Registrado",
                        minutosRetraso = miAsistencia?.MinutosRetraso ?? 0
                    }
                });
            }

            // Intentar registrar asistencia
            var registroExitoso = await _dataService.RegistrarAsistenciaAsync(alumno.Id, dto.ClaseId, "ALUMNO_ESCANEA");
            if (!registroExitoso)
                return BadRequest(new {
                    success = false,
                    message = "No se pudo registrar la asistencia. Por favor intenta nuevamente o contacta al profesor."
                });

            // Solo consumir token después de registro exitoso
            await _dataService.ConsumeTokenAsync(dto.Nonce);

            // Obtener la asistencia recién creada para devolver el estado
            var asistenciasActualizadas = await _dataService.GetAsistenciasPorClaseAsync(dto.ClaseId);
            var asistenciaRegistrada = asistenciasActualizadas.FirstOrDefault(a => a.AlumnoId == alumno.Id);

            return Ok(new {
                success = true,
                yaRegistrado = false,
                mensaje = $"¡Perfecto {alumno.Nombre}! Tu asistencia ha sido registrada exitosamente en '{clase.Asignatura}'.",
                estudiante = new {
                    codigo = alumno.Codigo,
                    nombre = alumno.Nombre,
                    estado = asistenciaRegistrada?.Estado.ToString() ?? "Presente",
                    minutosRetraso = asistenciaRegistrada?.MinutosRetraso ?? 0
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new {
                success = false,
                message = "Error del servidor al procesar tu registro. Por favor intenta nuevamente.",
                detalle = ex.Message
            });
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

            var registroExitoso = await _dataService.RegistrarAsistenciaAsync(alumnoIdFromAuth.Value, dto.ClaseId, "ALUMNO_ESCANEA_AUTO");
            if (!registroExitoso)
                return BadRequest(new { message = "No se pudo registrar la asistencia. Por favor intenta nuevamente." });

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
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var asistencias = await _dataService.GetAsistenciasAsync();

        // Si es Admin, devolver todas las asistencias
        if (IsAdmin())
        {
            return Ok(asistencias);
        }

        // Si es Docente, filtrar solo asistencias de sus clases
        var userId = GetCurrentUserId();
        var todasClases = await _dataService.GetClasesAsync();
        var misClasesIds = todasClases.Where(c => c.DocenteId == userId).Select(c => c.Id).ToHashSet();
        var misAsistencias = asistencias.Where(a => misClasesIds.Contains(a.ClaseId)).ToList();

        return Ok(misAsistencias);
    }

    [HttpGet("clase/{claseId}")]
    [HttpGet("/api/clases/{claseId}/asistencias")] // Ruta alternativa para compatibilidad
    [Authorize]
    public async Task<IActionResult> GetByClase(int claseId)
    {
        try
        {
            // Verificar acceso a la clase
            var userId = GetCurrentUserId();
            if (!await TieneAccesoClase(claseId, userId))
            {
                return Forbid();
            }

            var asistencias = await _dataService.GetAsistenciasPorClaseAsync(claseId);
            return Ok(asistencias);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener asistencias: " + ex.Message });
        }
    }

    [HttpGet("clase/{claseId}/lista-publica")]
    [AllowAnonymous]
    public async Task<IActionResult> GetListaPublica(int claseId, string? nonce)
    {
        try
        {
            // Validar que la clase existe
            var clase = await _dataService.GetClaseAsync(claseId);
            if (clase == null)
                return NotFound(new { message = "Clase no encontrada" });

            // Validar nonce si se proporciona (seguridad adicional)
            if (!string.IsNullOrEmpty(nonce))
            {
                var nonceValido = await _dataService.ValidarTokenAsync(nonce, claseId);
                if (!nonceValido)
                    return BadRequest(new { message = "Acceso denegado o expirado" });
            }

            // Obtener asistencias
            var asistencias = await _dataService.GetAsistenciasPorClaseAsync(claseId);

            // Obtener información del curso si existe
            var cursoInfo = new { nombre = "Sin curso", codigo = "" };
            if (clase.RamoId.HasValue)
            {
                var ramo = await _dataService.GetRamoAsync(clase.RamoId.Value);
                if (ramo != null)
                {
                    cursoInfo = new {
                        nombre = ramo.Nombre,
                        codigo = ramo.Codigo ?? ""
                    };
                }
            }

            return Ok(new
            {
                clase = new
                {
                    id = clase.Id,
                    asignatura = clase.Asignatura,
                    inicioUtc = clase.InicioUtc,
                    finUtc = clase.FinUtc,
                    activa = clase.Activa
                },
                curso = cursoInfo,
                asistencias = asistencias.Select(a => new
                {
                    alumnoNombre = a.Nombre ?? a.AlumnoNombre,
                    alumnoCodigo = a.Codigo ?? a.AlumnoCodigo,
                    estado = a.Estado,
                    minutosRetraso = a.MinutosRetraso,
                    marcadaUtc = a.MarcadaUtc ?? a.FechaHoraRegistro
                }).OrderBy(a => a.alumnoNombre)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener lista: " + ex.Message });
        }
    }

    [HttpGet("csv")]
    [Authorize]
    public async Task<IActionResult> ExportCsv()
    {
        var asistencias = await _dataService.GetAsistenciasAsync();

        // Si es Admin, exportar todas las asistencias
        if (IsAdmin())
        {
            var bytes = _csvService.GenerateAsistenciasCsv(asistencias);
            return File(bytes, "text/csv; charset=utf-8", "asistencias.csv");
        }

        // Si es Docente, filtrar solo asistencias de sus clases
        var userId = GetCurrentUserId();
        var todasClases = await _dataService.GetClasesAsync();
        var misClasesIds = todasClases.Where(c => c.DocenteId == userId).Select(c => c.Id).ToHashSet();
        var misAsistencias = asistencias.Where(a => misClasesIds.Contains(a.ClaseId)).ToList();
        var bytesDocente = _csvService.GenerateAsistenciasCsv(misAsistencias);

        return File(bytesDocente, "text/csv; charset=utf-8", "mis-asistencias.csv");
    }

    [HttpGet("clase/{claseId}/csv")]
    [HttpGet("/api/clases/{claseId}/asistencias.csv")] // Ruta alternativa para compatibilidad
    [Authorize]
    public async Task<IActionResult> ExportCsvByClase(int claseId)
    {
        // Verificar acceso a la clase
        var userId = GetCurrentUserId();
        if (!await TieneAccesoClase(claseId, userId))
        {
            return Forbid();
        }

        var asistencias = await _dataService.GetAsistenciasPorClaseAsync(claseId);
        var bytes = _csvService.GenerateAsistenciasCsv(asistencias);
        return File(bytes, "text/csv; charset=utf-8", $"asistencias-clase-{claseId}.csv");
    }

    [HttpGet("clase/{claseId}/csv-completo")]
    [Authorize]
    public async Task<IActionResult> ExportCsvCompletoByClase(int claseId)
    {
        // Verificar acceso a la clase
        var userId = GetCurrentUserId();
        if (!await TieneAccesoClase(claseId, userId))
        {
            return Forbid();
        }

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

    // Método helper para obtener el ID del usuario actual (docente)
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

    // Método helper para verificar si el usuario tiene acceso a una clase
    private async Task<bool> TieneAccesoClase(int claseId, int docenteId)
    {
        // Si es Admin, tiene acceso a todas las clases
        if (IsAdmin())
        {
            return true;
        }

        // Si es Docente, solo tiene acceso a sus propias clases
        var clase = await _dataService.GetClaseAsync(claseId);
        return clase != null && clase.DocenteId == docenteId;
    }
}

// DTO para registro automático de estudiantes autenticados
public class AlumnoScanAutoDto
{
    public int ClaseId { get; set; }
    public string Nonce { get; set; } = "";
    // No necesita AlumnoId porque se obtiene de la autenticación
}