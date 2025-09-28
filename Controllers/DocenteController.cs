using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using registroAsistencia.Data;
using registroAsistencia.Models;
using registroAsistencia.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace registroAsistencia.Controllers;

[Authorize]
[Route("docente")]
public class DocenteController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendanceService _attendanceService;
    private readonly IQrService _qrService;

    public DocenteController(ApplicationDbContext context, IAttendanceService attendanceService, IQrService qrService)
    {
        _context = context;
        _attendanceService = attendanceService;
        _qrService = qrService;
    }

    // Dashboard personalizado del docente
    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var docenteId = GetCurrentUserId();
        
        var estadisticas = await ObtenerEstadisticasDocente(docenteId);
        var clasesHoy = await ObtenerClasesHoy(docenteId);
        var cursosDocente = await ObtenerCursosDocente(docenteId);
        
        var viewModel = new DocenteDashboardViewModel
        {
            Estadisticas = estadisticas,
            ClasesHoy = clasesHoy,
            Cursos = cursosDocente
        };

        ViewData["Title"] = "Dashboard del Docente";
        return View(viewModel);
    }

    // Gestión de cursos del docente
    [HttpGet("cursos")]
    public async Task<IActionResult> Cursos()
    {
        var docenteId = GetCurrentUserId();
        
        var cursos = await _context.DocenteCursos
            .Where(dc => dc.DocenteId == docenteId && dc.Activo)
            .Include(dc => dc.Curso)
            .ThenInclude(c => c.Ramos)
            .Include(dc => dc.Curso)
            .ThenInclude(c => c.AlumnoCursos.Where(ac => ac.Activo))
            .Select(dc => new CursoDetalleViewModel
            {
                Id = dc.Curso.Id,
                Nombre = dc.Curso.Nombre,
                Codigo = dc.Curso.Codigo,
                Descripcion = dc.Curso.Descripcion,
                EsPropietario = dc.EsPropietario,
                TotalRamos = dc.Curso.Ramos.Count(r => r.Activo),
                TotalEstudiantes = dc.Curso.AlumnoCursos.Count(ac => ac.Activo),
                FechaCreacion = dc.Curso.FechaCreacion
            })
            .ToListAsync();

        ViewData["Title"] = "Mis Cursos";
        return View(cursos);
    }

    // Crear nuevo curso
    [HttpGet("cursos/crear")]
    public IActionResult CrearCurso()
    {
        ViewData["Title"] = "Crear Nuevo Curso";
        return View(new CursoCreateViewModel());
    }

    [HttpPost("cursos/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCurso([FromForm] CursoCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var docenteId = GetCurrentUserId();

        // Verificar que el código no exista
        var existeCodigo = await _context.Cursos.AnyAsync(c => c.Codigo == model.Codigo);
        if (existeCodigo)
        {
            ModelState.AddModelError("Codigo", "Ya existe un curso con este código");
            return View(model);
        }

        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Crear el curso
            var curso = new Curso
            {
                Nombre = model.Nombre,
                Codigo = model.Codigo,
                Descripcion = model.Descripcion,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Cursos.Add(curso);
            await _context.SaveChangesAsync();

            // Asignar el curso al docente como propietario
            var docenteCurso = new DocenteCurso
            {
                DocenteId = docenteId,
                CursoId = curso.Id,
                EsPropietario = true,
                AsignadoUtc = DateTime.UtcNow,
                Activo = true
            };

            _context.DocenteCursos.Add(docenteCurso);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Curso '{curso.Nombre}' creado exitosamente";
            return RedirectToAction("DetalleCurso", new { id = curso.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al crear el curso: {ex.Message}");
            return View(model);
        }
    }

    // Detalle del curso
    [HttpGet("cursos/{id:int}")]
    public async Task<IActionResult> DetalleCurso(int id)
    {
        var docenteId = GetCurrentUserId();

        var curso = await _context.DocenteCursos
            .Where(dc => dc.DocenteId == docenteId && dc.CursoId == id && dc.Activo)
            .Include(dc => dc.Curso)
            .ThenInclude(c => c.Ramos.Where(r => r.Activo))
            .Include(dc => dc.Curso)
            .ThenInclude(c => c.AlumnoCursos.Where(ac => ac.Activo))
            .ThenInclude(ac => ac.Alumno)
            .Select(dc => new CursoDetalleCompletoViewModel
            {
                Id = dc.Curso.Id,
                Nombre = dc.Curso.Nombre,
                Codigo = dc.Curso.Codigo,
                Descripcion = dc.Curso.Descripcion,
                EsPropietario = dc.EsPropietario,
                Ramos = dc.Curso.Ramos.Where(r => r.Activo).Select(r => new RamoResumenViewModel
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Codigo = r.Codigo,
                    TotalClases = r.Clases.Count(),
                    ClasesActivas = r.Clases.Count(c => c.FinUtc == null)
                }).ToList(),
                Estudiantes = dc.Curso.AlumnoCursos.Where(ac => ac.Activo).Select(ac => new EstudianteResumenViewModel
                {
                    Id = ac.Alumno.Id,
                    Codigo = ac.Alumno.Codigo,
                    Nombre = ac.Alumno.Nombre,
                    FechaInscripcion = ac.FechaInscripcion
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (curso == null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Curso: {curso.Nombre}";
        return View(curso);
    }

    // Mostrar formulario para crear ramo
    [HttpGet("cursos/{cursoId:int}/ramos")]
    public async Task<IActionResult> CrearRamo(int cursoId)
    {
        var docenteId = GetCurrentUserId();

        // Verificar acceso al curso
        var curso = await _context.DocenteCursos
            .Where(dc => dc.DocenteId == docenteId && dc.CursoId == cursoId && dc.Activo)
            .Include(dc => dc.Curso)
            .Select(dc => dc.Curso)
            .FirstOrDefaultAsync();

        if (curso == null)
        {
            return NotFound();
        }

        var viewModel = new CrearRamoFormViewModel
        {
            CursoId = cursoId,
            CursoNombre = curso.Nombre,
            CursoCodigo = curso.Codigo
        };

        ViewData["Title"] = $"Crear Ramo - {curso.Nombre}";
        return View(viewModel);
    }

    // Crear ramo en el curso
    [HttpPost("cursos/{cursoId:int}/ramos")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearRamo(int cursoId, [FromForm] RamoCreateViewModel model)
    {
        var docenteId = GetCurrentUserId();

        // Verificar acceso al curso
        var tieneAcceso = await _context.DocenteCursos
            .AnyAsync(dc => dc.DocenteId == docenteId && dc.CursoId == cursoId && dc.Activo);

        if (!tieneAcceso)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Crear el ramo
            var ramo = new Ramo
            {
                Nombre = model.Nombre,
                Codigo = model.Codigo,
                CursoId = cursoId,
                Descripcion = model.Descripcion,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Ramos.Add(ramo);
            await _context.SaveChangesAsync();

            // Asignar el ramo al docente
            var docenteRamo = new DocenteRamo
            {
                DocenteId = docenteId,
                RamoId = ramo.Id,
                AsignadoUtc = DateTime.UtcNow,
                Activo = true
            };

            _context.DocenteRamos.Add(docenteRamo);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Json(new { success = true, message = $"Ramo '{ramo.Nombre}' creado exitosamente", ramoId = ramo.Id });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al crear el ramo: {ex.Message}" });
        }
    }

    // Scanner de QR para asistencia
    [HttpGet("scanner")]
    public async Task<IActionResult> Scanner()
    {
        var docenteId = GetCurrentUserId();

        // Obtener clases activas del docente
        var clasesActivas = await _context.Clases
            .Where(c => c.DocenteId == docenteId && c.FinUtc == null)
            .Include(c => c.Ramo)
            .ThenInclude(r => r!.Curso)
            .Select(c => new ClaseActivaViewModel
            {
                Id = c.Id,
                Nombre = c.Asignatura,
                CursoNombre = c.Ramo!.Curso.Nombre,
                RamoNombre = c.Ramo.Nombre,
                InicioUtc = c.InicioUtc,
                MinutosTranscurridos = (int)(DateTime.UtcNow - c.InicioUtc).TotalMinutes
            })
            .ToListAsync();

        ViewData["Title"] = "Scanner de Asistencia";
        return View(clasesActivas);
    }

    [HttpGet("scanner/{claseId:int}")]
    public async Task<IActionResult> ScannerClase(int claseId)
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

        // Obtener resumen de asistencia
        var asistencias = await _attendanceService.GetAsistenciasResumenAsync(claseId);

        var viewModel = new ScannerClaseViewModel
        {
            ClaseId = clase.Id,
            NombreClase = clase.Asignatura,
            CursoNombre = clase.Ramo!.Curso.Nombre,
            RamoNombre = clase.Ramo.Nombre,
            FechaHoraInicio = clase.InicioUtc.ToLocalTime(),
            FechaHoraFin = clase.FinUtc?.ToLocalTime() ?? clase.InicioUtc.AddHours(2).ToLocalTime(),
            ClaseActiva = clase.Activa,
            Asistencias = asistencias.ToList()
        };

        ViewData["Title"] = $"Scanner - {clase.Asignatura}";
        return View(viewModel);
    }

    // API: Escanear QR de estudiante
    [HttpPost("api/scanner/escanear")]
    public async Task<IActionResult> EscanearQrEstudiante([FromBody] EscanearQrRequest request)
    {
        try
        {
            var docenteId = GetCurrentUserId();

            // Verificar que el docente tiene acceso a la clase
            var clase = await _context.Clases
                .FirstOrDefaultAsync(c => c.Id == request.ClaseId && c.DocenteId == docenteId);

            if (clase == null)
            {
                return Json(new { success = false, message = "No tienes acceso a esta clase" });
            }

            if (!clase.Activa)
            {
                return Json(new { success = false, message = "La clase no está activa" });
            }

            // Buscar estudiante por código QR o por código de estudiante directamente
            var qrEstudiante = await _context.QrEstudiantes
                .Include(qe => qe.Alumno)
                .FirstOrDefaultAsync(qe => qe.QrData == request.QrData && qe.Activo);

            // Si no se encuentra por QR, intentar buscar por código de estudiante directamente
            Alumno alumno = null;
            if (qrEstudiante != null)
            {
                alumno = qrEstudiante.Alumno;
            }
            else
            {
                // Buscar estudiante directamente por código
                alumno = await _context.Alumnos
                    .FirstOrDefaultAsync(a => a.Codigo == request.QrData);
                
                if (alumno == null)
                {
                    return Json(new { success = false, message = "Código QR no válido o estudiante no encontrado" });
                }
            }

            // Verificar que el estudiante esté inscrito en el curso
            var estaInscrito = await _context.AlumnoCursos
                .Include(ac => ac.Curso)
                .ThenInclude(c => c.Ramos)
                .AnyAsync(ac => ac.AlumnoId == alumno.Id && 
                               ac.Curso.Ramos.Any(r => r.Id == clase.RamoId) && 
                               ac.Activo);

            if (!estaInscrito)
            {
                return Json(new { success = false, message = "El estudiante no está inscrito en este curso" });
            }

            // Registrar asistencia
            var asistencia = await _attendanceService.RegistrarAsistenciaAsync(
                alumno.Id,
                request.ClaseId,
                MetodoRegistro.QrDocente,
                docenteId);

            return Json(new
            {
                success = true,
                message = $"Asistencia registrada para {alumno.Nombre}",
                estudiante = new
                {
                    nombre = alumno.Nombre,
                    codigo = alumno.Codigo,
                    estado = asistencia.Estado.ToString(),
                    minutosRetraso = asistencia.MinutosRetraso
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    // Gestión de estudiantes
    [HttpGet("cursos/{cursoId}/estudiantes")]
    public async Task<IActionResult> GestionarEstudiantes(int cursoId)
    {
        var docenteId = GetCurrentUserId();

        // Verificar acceso al curso
        var curso = await _context.DocenteCursos
            .Where(dc => dc.DocenteId == docenteId && dc.CursoId == cursoId && dc.Activo)
            .Include(dc => dc.Curso)
            .Select(dc => dc.Curso)
            .FirstOrDefaultAsync();

        if (curso == null)
        {
            return NotFound();
        }

        // Obtener estudiantes del curso
        var estudiantesInscritos = await _context.AlumnoCursos
            .Where(ac => ac.CursoId == cursoId && ac.Activo)
            .Include(ac => ac.Alumno)
            .Select(ac => new EstudianteInscritoViewModel
            {
                Id = ac.Alumno.Id,
                Codigo = ac.Alumno.Codigo,
                Nombre = ac.Alumno.Nombre,
                Email = "", // Agregar si existe en el modelo
                FechaInscripcion = ac.FechaInscripcion,
                Activo = ac.Activo
            })
            .OrderBy(e => e.Nombre)
            .ToListAsync();

        // Obtener todos los alumnos disponibles (no inscritos en este curso)
        var estudiantesDisponibles = await _context.Alumnos
            .Where(a => !_context.AlumnoCursos.Any(ac => ac.AlumnoId == a.Id && ac.CursoId == cursoId && ac.Activo))
            .Select(a => new EstudianteDisponibleViewModel
            {
                Id = a.Id,
                Codigo = a.Codigo,
                Nombre = a.Nombre
            })
            .OrderBy(e => e.Nombre)
            .ToListAsync();

        var viewModel = new GestionarEstudiantesViewModel
        {
            CursoId = cursoId,
            CursoNombre = curso.Nombre,
            CursoCodigo = curso.Codigo,
            EstudiantesInscritos = estudiantesInscritos,
            EstudiantesDisponibles = estudiantesDisponibles
        };

        ViewData["Title"] = $"Gestionar Estudiantes - {curso.Nombre}";
        return View(viewModel);
    }

    [HttpPost("cursos/{cursoId}/estudiantes/inscribir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InscribirEstudiante(int cursoId, int estudianteId)
    {
        var docenteId = GetCurrentUserId();

        // Verificar acceso al curso
        var tieneAcceso = await _context.DocenteCursos
            .AnyAsync(dc => dc.DocenteId == docenteId && dc.CursoId == cursoId && dc.Activo);

        if (!tieneAcceso)
        {
            return Forbid();
        }

        try
        {
            // Verificar que el estudiante existe
            var estudiante = await _context.Alumnos.FindAsync(estudianteId);
            if (estudiante == null)
            {
                return Json(new { success = false, message = "Estudiante no encontrado" });
            }

            // Verificar si ya está inscrito
            var yaInscrito = await _context.AlumnoCursos
                .AnyAsync(ac => ac.AlumnoId == estudianteId && ac.CursoId == cursoId && ac.Activo);

            if (yaInscrito)
            {
                return Json(new { success = false, message = "El estudiante ya está inscrito en este curso" });
            }

            // Inscribir estudiante
            var inscripcion = new AlumnoCurso
            {
                AlumnoId = estudianteId,
                CursoId = cursoId,
                FechaInscripcion = DateTime.UtcNow,
                Activo = true
            };

            _context.AlumnoCursos.Add(inscripcion);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Estudiante inscrito exitosamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al inscribir estudiante: {ex.Message}" });
        }
    }

    [HttpPost("cursos/{cursoId}/estudiantes/desinscribir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DesinscribirEstudiante(int cursoId, int estudianteId)
    {
        var docenteId = GetCurrentUserId();

        // Verificar acceso al curso
        var tieneAcceso = await _context.DocenteCursos
            .AnyAsync(dc => dc.DocenteId == docenteId && dc.CursoId == cursoId && dc.Activo);

        if (!tieneAcceso)
        {
            return Forbid();
        }

        try
        {
            var inscripcion = await _context.AlumnoCursos
                .FirstOrDefaultAsync(ac => ac.AlumnoId == estudianteId && ac.CursoId == cursoId && ac.Activo);

            if (inscripcion == null)
            {
                return Json(new { success = false, message = "La inscripción no existe" });
            }

            // Marcar como inactivo en lugar de eliminar
            inscripcion.Activo = false;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Estudiante desinscrito exitosamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al desinscribir estudiante: {ex.Message}" });
        }
    }

    // Métodos auxiliares
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 1;
    }

    private async Task<EstadisticasDocenteViewModel> ObtenerEstadisticasDocente(int docenteId)
    {
        var totalCursos = await _context.DocenteCursos.CountAsync(dc => dc.DocenteId == docenteId && dc.Activo);
        var totalRamos = await _context.DocenteRamos.CountAsync(dr => dr.DocenteId == docenteId && dr.Activo);
        var clasesHoy = await _context.Clases
            .CountAsync(c => c.DocenteId == docenteId && c.InicioUtc.Date == DateTime.UtcNow.Date);
        var totalEstudiantes = await _context.AlumnoCursos
            .Where(ac => _context.DocenteCursos.Any(dc => dc.DocenteId == docenteId && dc.CursoId == ac.CursoId && dc.Activo))
            .CountAsync(ac => ac.Activo);

        return new EstadisticasDocenteViewModel
        {
            TotalCursos = totalCursos,
            TotalRamos = totalRamos,
            ClasesHoy = clasesHoy,
            TotalEstudiantes = totalEstudiantes
        };
    }

    private async Task<List<ClaseHoyViewModel>> ObtenerClasesHoy(int docenteId)
    {
        var hoy = DateTime.UtcNow.Date;
        return await _context.Clases
            .Where(c => c.DocenteId == docenteId && c.InicioUtc.Date == hoy)
            .Include(c => c.Ramo)
            .ThenInclude(r => r!.Curso)
            .Select(c => new ClaseHoyViewModel
            {
                Id = c.Id,
                Nombre = c.Asignatura,
                RamoNombre = c.Ramo!.Nombre,
                CursoNombre = c.Ramo.Curso.Nombre,
                HoraInicio = c.InicioUtc.ToLocalTime(),
                Activa = c.FinUtc == null,
                TotalAsistencias = _context.Asistencias.Count(a => a.ClaseId == c.Id)
            })
            .OrderBy(c => c.HoraInicio)
            .ToListAsync();
    }

    private async Task<List<CursoResumenViewModel>> ObtenerCursosDocente(int docenteId)
    {
        return await _context.DocenteCursos
            .Where(dc => dc.DocenteId == docenteId && dc.Activo)
            .Include(dc => dc.Curso)
            .Select(dc => new CursoResumenViewModel
            {
                Id = dc.Curso.Id,
                Nombre = dc.Curso.Nombre,
                Codigo = dc.Curso.Codigo,
                EsPropietario = dc.EsPropietario,
                TotalRamos = dc.Curso.Ramos.Count(r => r.Activo)
            })
            .ToListAsync();
    }

    // ============= CRUD RAMOS =============
    
    // Editar ramo
    [HttpGet("ramos/{id:int}/editar")]
    public async Task<IActionResult> EditarRamo(int id)
    {
        var docenteId = GetCurrentUserId();
        
        var ramo = await _context.DocenteRamos
            .Where(dr => dr.DocenteId == docenteId && dr.RamoId == id && dr.Activo)
            .Include(dr => dr.Ramo)
            .ThenInclude(r => r.Curso)
            .Select(dr => new EditarRamoViewModel
            {
                Id = dr.Ramo.Id,
                CursoId = dr.Ramo.CursoId,
                CursoNombre = dr.Ramo.Curso.Nombre,
                Nombre = dr.Ramo.Nombre,
                Codigo = dr.Ramo.Codigo,
                Descripcion = dr.Ramo.Descripcion
            })
            .FirstOrDefaultAsync();

        if (ramo == null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Editar Ramo - {ramo.Nombre}";
        return View(ramo);
    }

    [HttpPost("ramos/{id:int}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarRamo(int id, [FromForm] EditarRamoViewModel model)
    {
        var docenteId = GetCurrentUserId();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ramo = await _context.DocenteRamos
            .Where(dr => dr.DocenteId == docenteId && dr.RamoId == id && dr.Activo)
            .Include(dr => dr.Ramo)
            .Select(dr => dr.Ramo)
            .FirstOrDefaultAsync();

        if (ramo == null)
        {
            return NotFound();
        }

        try
        {
            ramo.Nombre = model.Nombre;
            ramo.Codigo = model.Codigo;
            ramo.Descripcion = model.Descripcion;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Ramo '{ramo.Nombre}' actualizado exitosamente";
            return RedirectToAction("DetalleCurso", new { id = ramo.CursoId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al actualizar el ramo: {ex.Message}");
            return View(model);
        }
    }

    // Detalle del ramo
    [HttpGet("ramos/{id:int}")]
    public async Task<IActionResult> DetalleRamo(int id)
    {
        var docenteId = GetCurrentUserId();

        var ramo = await _context.DocenteRamos
            .Where(dr => dr.DocenteId == docenteId && dr.RamoId == id && dr.Activo)
            .Include(dr => dr.Ramo)
            .ThenInclude(r => r.Curso)
            .Include(dr => dr.Ramo)
            .ThenInclude(r => r.Clases)
            .Select(dr => new DetalleRamoViewModel
            {
                Id = dr.Ramo.Id,
                Nombre = dr.Ramo.Nombre,
                Codigo = dr.Ramo.Codigo,
                Descripcion = dr.Ramo.Descripcion,
                CursoId = dr.Ramo.CursoId,
                CursoNombre = dr.Ramo.Curso.Nombre,
                CursoCodigo = dr.Ramo.Curso.Codigo,
                Clases = dr.Ramo.Clases.Select(c => new ClaseResumenViewModel
                {
                    Id = c.Id,
                    Nombre = c.Asignatura,
                    FechaInicio = c.InicioUtc.ToLocalTime(),
                    Activa = c.FinUtc == null,
                    TotalAsistencias = _context.Asistencias.Count(a => a.ClaseId == c.Id),
                    PresentesCount = _context.Asistencias.Count(a => a.ClaseId == c.Id && a.Estado == EstadoAsistencia.Presente),
                    TardesCount = _context.Asistencias.Count(a => a.ClaseId == c.Id && a.Estado == EstadoAsistencia.Tardanza),
                    AusentesCount = _context.Asistencias.Count(a => a.ClaseId == c.Id && a.Estado == EstadoAsistencia.Ausente)
                }).OrderByDescending(c => c.FechaInicio).ToList()
            })
            .FirstOrDefaultAsync();

        if (ramo == null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Ramo: {ramo.Nombre}";
        return View(ramo);
    }

    // Eliminar ramo
    [HttpPost("ramos/{id:int}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarRamo(int id)
    {
        var docenteId = GetCurrentUserId();

        var ramo = await _context.DocenteRamos
            .Where(dr => dr.DocenteId == docenteId && dr.RamoId == id && dr.Activo)
            .Include(dr => dr.Ramo)
            .Select(dr => dr.Ramo)
            .FirstOrDefaultAsync();

        if (ramo == null)
        {
            return Json(new { success = false, message = "Ramo no encontrado" });
        }

        try
        {
            // Verificar si tiene clases activas
            var tieneClasesActivas = await _context.Clases
                .AnyAsync(c => c.RamoId == id && c.FinUtc == null);

            if (tieneClasesActivas)
            {
                return Json(new { success = false, message = "No se puede eliminar el ramo porque tiene clases activas" });
            }

            ramo.Activo = false;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Ramo '{ramo.Nombre}' eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al eliminar el ramo: {ex.Message}" });
        }
    }

    // ============= PERFIL ESTUDIANTE =============
    
    [HttpGet("estudiantes/{id:int}/perfil")]
    public async Task<IActionResult> PerfilEstudiante(int id, int? cursoId = null)
    {
        var docenteId = GetCurrentUserId();

        // Verificar que el docente tiene acceso al estudiante
        var estudiante = await _context.AlumnoCursos
            .Where(ac => ac.AlumnoId == id && ac.Activo)
            .Where(ac => _context.DocenteCursos.Any(dc => dc.DocenteId == docenteId && dc.CursoId == ac.CursoId && dc.Activo))
            .Include(ac => ac.Alumno)
            .Include(ac => ac.Curso)
            .Select(ac => new PerfilEstudianteViewModel
            {
                Id = ac.Alumno.Id,
                Codigo = ac.Alumno.Codigo,
                Nombre = ac.Alumno.Nombre,
                CursoId = ac.CursoId,
                CursoNombre = ac.Curso.Nombre,
                FechaInscripcion = ac.FechaInscripcion
            })
            .FirstOrDefaultAsync();

        if (estudiante == null)
        {
            return NotFound();
        }

        // Obtener estadísticas de asistencia del estudiante
        var asistencias = await _context.Asistencias
            .Where(a => a.AlumnoId == id)
            .Include(a => a.Clase)
            .ThenInclude(c => c.Ramo)
            .Where(a => _context.DocenteCursos.Any(dc => dc.DocenteId == docenteId && dc.CursoId == a.Clase.Ramo.CursoId && dc.Activo))
            .ToListAsync();

        estudiante.TotalClases = asistencias.Count;
        estudiante.ClasesPresente = asistencias.Count(a => a.Estado == EstadoAsistencia.Presente);
        estudiante.ClasesTarde = asistencias.Count(a => a.Estado == EstadoAsistencia.Tardanza);
        estudiante.ClasesAusente = asistencias.Count(a => a.Estado == EstadoAsistencia.Ausente);
        estudiante.PorcentajeAsistencia = estudiante.TotalClases > 0 ? 
            (double)(estudiante.ClasesPresente + estudiante.ClasesTarde) / estudiante.TotalClases * 100 : 0;

        ViewData["Title"] = $"Perfil - {estudiante.Nombre}";
        return View(estudiante);
    }

    // ============= ASISTENCIAS ESTUDIANTE =============
    
    [HttpGet("estudiantes/{estudianteId:int}/asistencias")]
    public async Task<IActionResult> AsistenciasEstudiante(int estudianteId, int cursoId)
    {
        var docenteId = GetCurrentUserId();

        // Verificar acceso
        var tieneAcceso = await _context.DocenteCursos
            .AnyAsync(dc => dc.DocenteId == docenteId && dc.CursoId == cursoId && dc.Activo);

        if (!tieneAcceso)
        {
            return Forbid();
        }

        var estudiante = await _context.Alumnos.FindAsync(estudianteId);
        if (estudiante == null)
        {
            return NotFound();
        }

        var curso = await _context.Cursos.FindAsync(cursoId);
        if (curso == null)
        {
            return NotFound();
        }

        // Obtener asistencias del estudiante en el curso
        var asistencias = await _context.Asistencias
            .Where(a => a.AlumnoId == estudianteId)
            .Include(a => a.Clase)
            .ThenInclude(c => c.Ramo)
            .Where(a => a.Clase.Ramo.CursoId == cursoId)
            .Select(a => new AsistenciaEstudianteViewModel
            {
                ClaseId = a.ClaseId,
                NombreClase = a.Clase.Asignatura,
                RamoNombre = a.Clase.Ramo.Nombre,
                FechaClase = a.Clase.InicioUtc.ToLocalTime(),
                Estado = a.Estado,
                MinutosRetraso = a.MinutosRetraso,
                FechaRegistro = a.CreadoUtc.ToLocalTime(),
                MetodoRegistro = a.Metodo
            })
            .OrderByDescending(a => a.FechaClase)
            .ToListAsync();

        var viewModel = new AsistenciasEstudianteViewModel
        {
            EstudianteId = estudianteId,
            EstudianteNombre = estudiante.Nombre,
            EstudianteCodigo = estudiante.Codigo,
            CursoId = cursoId,
            CursoNombre = curso.Nombre,
            Asistencias = asistencias
        };

        ViewData["Title"] = $"Asistencias - {estudiante.Nombre}";
        return View(viewModel);
    }

    // ============= EDITAR CURSO =============
    
    [HttpGet("cursos/{id:int}/editar")]
    public async Task<IActionResult> EditarCurso(int id)
    {
        var docenteId = GetCurrentUserId();

        var curso = await _context.DocenteCursos
            .Where(dc => dc.DocenteId == docenteId && dc.CursoId == id && dc.Activo && dc.EsPropietario)
            .Include(dc => dc.Curso)
            .Select(dc => new EditarCursoViewModel
            {
                Id = dc.Curso.Id,
                Nombre = dc.Curso.Nombre,
                Codigo = dc.Curso.Codigo,
                Descripcion = dc.Curso.Descripcion
            })
            .FirstOrDefaultAsync();

        if (curso == null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Editar Curso - {curso.Nombre}";
        return View(curso);
    }

    [HttpPost("cursos/{id:int}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCurso(int id, [FromForm] EditarCursoViewModel model)
    {
        var docenteId = GetCurrentUserId();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var curso = await _context.DocenteCursos
            .Where(dc => dc.DocenteId == docenteId && dc.CursoId == id && dc.Activo && dc.EsPropietario)
            .Include(dc => dc.Curso)
            .Select(dc => dc.Curso)
            .FirstOrDefaultAsync();

        if (curso == null)
        {
            return NotFound();
        }

        try
        {
            curso.Nombre = model.Nombre;
            curso.Codigo = model.Codigo;
            curso.Descripcion = model.Descripcion;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Curso '{curso.Nombre}' actualizado exitosamente";
            return RedirectToAction("DetalleCurso", new { id = curso.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al actualizar el curso: {ex.Message}");
            return View(model);
        }
    }

    // ============= CONFIGURACIÓN CURSO =============
    
    [HttpGet("cursos/{id:int}/configuracion")]
    public async Task<IActionResult> ConfiguracionCurso(int id)
    {
        var docenteId = GetCurrentUserId();

        var curso = await _context.DocenteCursos
            .Where(dc => dc.DocenteId == docenteId && dc.CursoId == id && dc.Activo && dc.EsPropietario)
            .Include(dc => dc.Curso)
            .ThenInclude(c => c.Ramos)
            .Select(dc => new ConfiguracionCursoViewModel
            {
                Id = dc.Curso.Id,
                Nombre = dc.Curso.Nombre,
                Codigo = dc.Curso.Codigo,
                Descripcion = dc.Curso.Descripcion,
                TotalRamos = dc.Curso.Ramos.Count(r => r.Activo),
                TotalEstudiantes = _context.AlumnoCursos.Count(ac => ac.CursoId == dc.CursoId && ac.Activo),
                FechaCreacion = dc.Curso.FechaCreacion
            })
            .FirstOrDefaultAsync();

        if (curso == null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Configuración - {curso.Nombre}";
        return View(curso);
    }
}

// ViewModels adicionales
public class DocenteDashboardViewModel
{
    public EstadisticasDocenteViewModel Estadisticas { get; set; } = new();
    public List<ClaseHoyViewModel> ClasesHoy { get; set; } = new();
    public List<CursoResumenViewModel> Cursos { get; set; } = new();
}

public class EstadisticasDocenteViewModel
{
    public int TotalCursos { get; set; }
    public int TotalRamos { get; set; }
    public int ClasesHoy { get; set; }
    public int TotalEstudiantes { get; set; }
}

public class ClaseHoyViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string RamoNombre { get; set; } = "";
    public string CursoNombre { get; set; } = "";
    public DateTime HoraInicio { get; set; }
    public bool Activa { get; set; }
    public int TotalAsistencias { get; set; }
}

public class CursoResumenViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public bool EsPropietario { get; set; }
    public int TotalRamos { get; set; }
}

public class CursoDetalleViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool EsPropietario { get; set; }
    public int TotalRamos { get; set; }
    public int TotalEstudiantes { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CursoDetalleCompletoViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool EsPropietario { get; set; }
    public List<RamoResumenViewModel> Ramos { get; set; } = new();
    public List<EstudianteResumenViewModel> Estudiantes { get; set; } = new();
}

public class RamoResumenViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public int TotalClases { get; set; }
    public int ClasesActivas { get; set; }
}

public class EstudianteResumenViewModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public DateTime FechaInscripcion { get; set; }
}

public class CursoCreateViewModel
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El código es requerido")]
    [StringLength(50, ErrorMessage = "El código no puede exceder 50 caracteres")]
    public string Codigo { get; set; } = "";

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string? Descripcion { get; set; }
}

public class RamoCreateViewModel
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El código es requerido")]
    [StringLength(50, ErrorMessage = "El código no puede exceder 50 caracteres")]
    public string Codigo { get; set; } = "";

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string? Descripcion { get; set; }
}

public class ClaseActivaViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string CursoNombre { get; set; } = "";
    public string RamoNombre { get; set; } = "";
    public DateTime InicioUtc { get; set; }
    public DateTime HoraInicio => InicioUtc;
    public int MinutosTranscurridos { get; set; }
}

public class ScannerClaseViewModel
{
    public int ClaseId { get; set; }
    public string NombreClase { get; set; } = "";
    public string CursoNombre { get; set; } = "";
    public string RamoNombre { get; set; } = "";
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public bool ClaseActiva { get; set; }
    public List<AsistenciaResumen> Asistencias { get; set; } = new();
}

public class EscanearQrRequest
{
    public int ClaseId { get; set; }
    public string QrData { get; set; } = "";
}

// ViewModels para gestión de estudiantes
public class GestionarEstudiantesViewModel
{
    public int CursoId { get; set; }
    public string CursoNombre { get; set; } = "";
    public string CursoCodigo { get; set; } = "";
    public List<EstudianteInscritoViewModel> EstudiantesInscritos { get; set; } = new();
    public List<EstudianteDisponibleViewModel> EstudiantesDisponibles { get; set; } = new();
}

public class EstudianteInscritoViewModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime FechaInscripcion { get; set; }
    public bool Activo { get; set; }
}

public class EstudianteDisponibleViewModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
}

public class CrearRamoFormViewModel
{
    public int CursoId { get; set; }
    public string CursoNombre { get; set; } = "";
    public string CursoCodigo { get; set; } = "";
    
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El código es requerido")]
    [StringLength(50, ErrorMessage = "El código no puede exceder 50 caracteres")]
    public string Codigo { get; set; } = "";

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string? Descripcion { get; set; }
}

// ============= NUEVOS VIEWMODELS =============

public class EditarRamoViewModel
{
    public int Id { get; set; }
    public int CursoId { get; set; }
    public string CursoNombre { get; set; } = "";
    
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El código es requerido")]
    [StringLength(50, ErrorMessage = "El código no puede exceder 50 caracteres")]
    public string Codigo { get; set; } = "";

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string? Descripcion { get; set; }
}

public class DetalleRamoViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Descripcion { get; set; }
    public int CursoId { get; set; }
    public string CursoNombre { get; set; } = "";
    public string CursoCodigo { get; set; } = "";
    public List<ClaseResumenViewModel> Clases { get; set; } = new();
}

public class ClaseResumenViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public bool Activa { get; set; }
    public int TotalAsistencias { get; set; }
    public int PresentesCount { get; set; }
    public int TardesCount { get; set; }
    public int AusentesCount { get; set; }
}

public class PerfilEstudianteViewModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int CursoId { get; set; }
    public string CursoNombre { get; set; } = "";
    public DateTime FechaInscripcion { get; set; }
    public int TotalClases { get; set; }
    public int ClasesPresente { get; set; }
    public int ClasesTarde { get; set; }
    public int ClasesAusente { get; set; }
    public double PorcentajeAsistencia { get; set; }
}

public class AsistenciaEstudianteViewModel
{
    public int ClaseId { get; set; }
    public string NombreClase { get; set; } = "";
    public string RamoNombre { get; set; } = "";
    public DateTime FechaClase { get; set; }
    public EstadoAsistencia Estado { get; set; }
    public int MinutosRetraso { get; set; }
    public DateTime FechaRegistro { get; set; }
    public MetodoRegistro MetodoRegistro { get; set; }
}

public class AsistenciasEstudianteViewModel
{
    public int EstudianteId { get; set; }
    public string EstudianteNombre { get; set; } = "";
    public string EstudianteCodigo { get; set; } = "";
    public int CursoId { get; set; }
    public string CursoNombre { get; set; } = "";
    public List<AsistenciaEstudianteViewModel> Asistencias { get; set; } = new();
}

public class EditarCursoViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El código es requerido")]
    [StringLength(50, ErrorMessage = "El código no puede exceder 50 caracteres")]
    public string Codigo { get; set; } = "";

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string? Descripcion { get; set; }
}

public class ConfiguracionCursoViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Descripcion { get; set; }
    public int TotalRamos { get; set; }
    public int TotalEstudiantes { get; set; }
    public DateTime FechaCreacion { get; set; }
}