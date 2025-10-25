using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using registroAsistencia.Models;
using registroAsistencia.Services;
using registroAsistencia.Data;

namespace registroAsistencia.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlumnosController : ControllerBase
{
    private readonly IDataService _dataService;
    private readonly IQrService _qrService;
    private readonly ICsvService _csvService;
    private readonly ApplicationDbContext _context;

    public AlumnosController(IDataService dataService, IQrService qrService, ICsvService csvService, ApplicationDbContext context)
    {
        _dataService = dataService;
        _qrService = qrService;
        _csvService = csvService;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AlumnoCreateDto dto)
    {
        try
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return BadRequest(new { message = "Nombre es requerido" });
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
    public async Task<IActionResult> GetAll([FromQuery] string? codigo = null, [FromQuery] bool soloMisCursos = false)
    {
        try
        {
            var alumnos = await _dataService.GetAlumnosAsync();

            // Si soloMisCursos es true, filtrar solo estudiantes de cursos del docente
            if (soloMisCursos && !IsAdmin())
            {
                var userId = GetCurrentUserId();

                // Obtener los IDs de los cursos del docente a través de DocenteCurso
                var misCursosIds = await _context.DocenteCursos
                    .Where(dc => dc.DocenteId == userId && dc.Activo)
                    .Select(dc => dc.CursoId)
                    .ToListAsync();

                // Obtener los IDs de los alumnos inscritos en esos cursos
                var alumnosIds = await _context.AlumnoCursos
                    .Where(ac => misCursosIds.Contains(ac.CursoId))
                    .Select(ac => ac.AlumnoId)
                    .Distinct()
                    .ToListAsync();

                // Filtrar solo los alumnos que están en mis cursos
                alumnos = alumnos.Where(a => alumnosIds.Contains(a.Id));
            }

            // Si se proporciona un código, filtrar por él
            if (!string.IsNullOrEmpty(codigo))
            {
                alumnos = alumnos.Where(a => a.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));
            }

            return Ok(alumnos ?? new List<Alumno>());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener alumnos: " + ex.Message });
        }
    }

    // Método helper para verificar si el usuario es Admin
    private bool IsAdmin()
    {
        return User.IsInRole("Administrador");
    }

    // Método helper para obtener el ID del usuario actual
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 1;
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
            if (dto == null || string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return BadRequest(new { message = "Nombre es requerido" });
            }

            // SEGURIDAD: Obtener alumno actual para validar que el código no cambió
            var alumnoActual = await _dataService.GetAlumnoAsync(id);
            if (alumnoActual == null)
                return NotFound(new { message = "Alumno no encontrado" });

            // IMPORTANTE: Prevenir cambio de código (seguridad)
            // Ignorar el código enviado desde el frontend y usar el existente
            if (!string.IsNullOrEmpty(dto.Codigo) && dto.Codigo != alumnoActual.Codigo)
            {
                return BadRequest(new {
                    message = "No se permite cambiar el código del estudiante por seguridad",
                    codigoActual = alumnoActual.Codigo,
                    codigoIntentado = dto.Codigo
                });
            }

            // Forzar que el código sea el actual (prevenir manipulación desde inspector)
            dto.Codigo = alumnoActual.Codigo;

            var updated = await _dataService.UpdateAlumnoAsync(id, dto);
            if (!updated)
                return NotFound(new { message = "Error al actualizar alumno" });

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

    // Regenerar QR para estudiante específico
    [HttpPost("{id}/regenerate-qr")]
    public async Task<IActionResult> RegenerateQr(int id)
    {
        try
        {
            var alumno = await _dataService.GetAlumnoAsync(id);
            if (alumno == null)
                return NotFound(new { message = "Estudiante no encontrado" });

            // Regenerar QR con formato mejorado
            var qrPayload = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "student",
                alumnoId = alumno.Id,
                codigo = alumno.Codigo,
                nombre = alumno.Nombre,
                generated = DateTime.UtcNow.ToString("O")
            });

            alumno.QrAlumnoBase64 = _qrService.GenerateBase64Qr(qrPayload);
            await _dataService.UpdateAlumnoQrAsync(id, alumno.QrAlumnoBase64);

            return Ok(new { message = "QR regenerado exitosamente", qrCode = alumno.QrAlumnoBase64 });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Error regenerando QR: " + ex.Message });
        }
    }

    // Exportar lista de estudiantes con QRs
    [HttpGet("export")]
    public async Task<IActionResult> ExportStudents()
    {
        try
        {
            var alumnos = await _dataService.GetAlumnosAsync();
            var export = alumnos.Select(a => new
            {
                id = a.Id,
                codigo = a.Codigo,
                nombre = a.Nombre,
                qrCode = a.QrAlumnoBase64,
                qrDataUrl = !string.IsNullOrEmpty(a.QrAlumnoBase64) ? $"data:image/png;base64,{a.QrAlumnoBase64}" : null,
                created = a.FechaCreacion.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return Ok(export);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error exportando estudiantes: " + ex.Message });
        }
    }

    // Endpoint para generar PDF con QRs para impresión
    [HttpGet("print-qrs")]
    public async Task<IActionResult> PrintQRs([FromQuery] string? ids = null)
    {
        try
        {
            var alumnos = await _dataService.GetAlumnosAsync();

            if (!string.IsNullOrEmpty(ids))
            {
                var selectedIds = ids.Split(',').Select(int.Parse).ToList();
                alumnos = alumnos.Where(a => selectedIds.Contains(a.Id));
            }

            // Por ahora retornamos JSON, pero aquí se podría generar un PDF
            var printData = alumnos.Select(a => new
            {
                id = a.Id,
                codigo = a.Codigo,
                nombre = a.Nombre,
                qrDataUrl = !string.IsNullOrEmpty(a.QrAlumnoBase64) ? $"data:image/png;base64,{a.QrAlumnoBase64}" : null
            });

            return Ok(printData);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error preparando QRs para impresión: " + ex.Message });
        }
    }

    // Importar alumnos masivamente desde CSV/Excel
    [HttpPost("import")]
    public async Task<IActionResult> ImportFromCsv(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No se ha proporcionado ningún archivo" });
            }

            // Validar extensión del archivo
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".csv" && extension != ".txt")
            {
                return BadRequest(new { message = "Solo se permiten archivos CSV (.csv, .txt)" });
            }

            // Validar tamaño (máximo 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "El archivo no puede superar los 5MB" });
            }

            // Parsear el CSV
            List<AlumnoCreateDto> alumnosDto;
            using (var stream = file.OpenReadStream())
            {
                alumnosDto = await _csvService.ParseAlumnosCsvAsync(stream);
            }

            if (alumnosDto.Count == 0)
            {
                return BadRequest(new { message = "No se encontraron alumnos válidos en el archivo" });
            }

            // Crear alumnos en batch
            var createdAlumnos = new List<Alumno>();
            var errors = new List<string>();

            foreach (var dto in alumnosDto)
            {
                try
                {
                    var alumno = await _dataService.CreateAlumnoAsync(dto);
                    createdAlumnos.Add(alumno);
                }
                catch (Exception ex)
                {
                    errors.Add($"Error creando '{dto.Nombre}': {ex.Message}");
                }
            }

            return Ok(new
            {
                message = $"Importación completada: {createdAlumnos.Count} alumnos creados",
                created = createdAlumnos.Count,
                total = alumnosDto.Count,
                errors = errors,
                alumnos = createdAlumnos.Select(a => new
                {
                    id = a.Id,
                    codigo = a.Codigo,
                    nombre = a.Nombre
                })
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Error procesando archivo: " + ex.Message });
        }
    }

    // Descargar plantilla CSV de ejemplo
    [HttpGet("template")]
    [AllowAnonymous]
    public IActionResult DownloadTemplate()
    {
        var csvContent = @"nombre
Juan Pérez García
María López González
Carlos Rodríguez Sánchez
Ana Martínez Torres
Pedro González Ramírez";

        var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        return File(bytes, "text/csv", "plantilla_alumnos.csv");
    }

    // Obtener estudiantes agrupados por curso
    [HttpGet("grouped-by-courses")]
    public async Task<IActionResult> GetGroupedByCourses()
    {
        try
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();

            // Obtener todos los cursos relevantes (todos si es Admin, solo los del docente si no)
            IQueryable<Curso> cursosQuery = _context.Cursos;

            if (!isAdmin)
            {
                // Filtrar solo cursos del docente
                var misCursosIds = await _context.DocenteCursos
                    .Where(dc => dc.DocenteId == userId && dc.Activo)
                    .Select(dc => dc.CursoId)
                    .ToListAsync();

                cursosQuery = cursosQuery.Where(c => misCursosIds.Contains(c.Id));
            }

            var cursos = await cursosQuery.ToListAsync();

            // Obtener todos los alumnos
            var alumnos = await _dataService.GetAlumnosAsync();

            // Agrupar estudiantes por curso
            var cursosConEstudiantes = new List<object>();

            foreach (var curso in cursos)
            {
                // Obtener IDs de alumnos en este curso
                var alumnosIds = await _context.AlumnoCursos
                    .Where(ac => ac.CursoId == curso.Id)
                    .Select(ac => ac.AlumnoId)
                    .ToListAsync();

                var estudiantesCurso = alumnos
                    .Where(a => alumnosIds.Contains(a.Id))
                    .ToList();

                cursosConEstudiantes.Add(new
                {
                    cursoId = curso.Id,
                    cursoNombre = curso.Nombre,
                    totalEstudiantes = estudiantesCurso.Count,
                    estudiantes = estudiantesCurso
                });
            }

            // Obtener estudiantes sin curso asignado
            var todosLosAlumnosEnCursos = await _context.AlumnoCursos
                .Select(ac => ac.AlumnoId)
                .Distinct()
                .ToListAsync();

            var estudiantesSinCurso = alumnos
                .Where(a => !todosLosAlumnosEnCursos.Contains(a.Id))
                .ToList();

            return Ok(new
            {
                cursos = cursosConEstudiantes,
                sinCurso = new
                {
                    totalEstudiantes = estudiantesSinCurso.Count,
                    estudiantes = estudiantesSinCurso
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener estudiantes agrupados: " + ex.Message });
        }
    }
}