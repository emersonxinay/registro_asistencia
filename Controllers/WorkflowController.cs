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
public class WorkflowController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAttendanceService _attendanceService;
    private readonly IQrService _qrService;
    private readonly IConfiguration _configuration;

    public WorkflowController(
        ApplicationDbContext context, 
        IAttendanceService attendanceService,
        IQrService qrService,
        IConfiguration configuration)
    {
        _context = context;
        _attendanceService = attendanceService;
        _qrService = qrService;
        _configuration = configuration;
    }

    // PASO 1: Seleccionar Curso
    [Route("workflow")]
    [Route("workflow/cursos")]
    [Route("workflow/inicio")]
    public async Task<IActionResult> SeleccionarCurso()
    {
        var docenteId = GetCurrentUserId();
        
        // Obtener cursos del docente (propios y asignados)
        var cursosDocente = await _context.DocenteCursos
            .Where(dc => dc.DocenteId == docenteId && dc.Activo)
            .Include(dc => dc.Curso)
            .Select(dc => new CursoViewModel
            {
                Id = dc.Curso.Id,
                Nombre = dc.Curso.Nombre,
                Codigo = dc.Curso.Codigo,
                Descripcion = dc.Curso.Descripcion,
                EsPropietario = dc.EsPropietario,
                TotalRamos = dc.Curso.Ramos.Count(r => r.Activo)
            })
            .ToListAsync();

        ViewData["Title"] = "Crear Nueva Clase - Paso 1: Seleccionar Curso";
        return View(cursosDocente);
    }

    // PASO 2: Seleccionar Ramo/Asignatura
    [Route("workflow/curso/{cursoId:int}/ramos")]
    [Route("workflow/ramos/{cursoId:int}")] // Compatibilidad
    public async Task<IActionResult> SeleccionarRamo(int cursoId)
    {
        var docenteId = GetCurrentUserId();

        // Verificar que el docente tiene acceso al curso
        var tieneAcceso = await _context.DocenteCursos
            .AnyAsync(dc => dc.DocenteId == docenteId && dc.CursoId == cursoId && dc.Activo);

        if (!tieneAcceso)
        {
            TempData["ErrorMessage"] = "No tienes acceso a este curso";
            return RedirectToAction("SeleccionarCurso");
        }

        var curso = await _context.Cursos.FindAsync(cursoId);
        if (curso == null)
        {
            return NotFound();
        }

        // Obtener ramos del curso que el docente puede enseñar
        var ramosDisponibles = await _context.DocenteRamos
            .Where(dr => dr.DocenteId == docenteId && dr.Activo)
            .Include(dr => dr.Ramo)
                .ThenInclude(r => r.Clases)
            .Where(dr => dr.Ramo.CursoId == cursoId && dr.Ramo.Activo)
            .Select(dr => new RamoViewModel
            {
                Id = dr.Ramo.Id,
                Nombre = dr.Ramo.Nombre,
                Codigo = dr.Ramo.Codigo,
                Descripcion = dr.Ramo.Descripcion,
                TotalClasesActivas = dr.Ramo.Clases.Count(c => c.FinUtc == null),
                TotalClases = dr.Ramo.Clases.Count(),
                TotalEstudiantes = _context.AlumnoCursos.Count(ac => ac.CursoId == cursoId && ac.Activo),
                HorasSemanales = 0 // Por defecto 0, ya que el modelo Ramo no tiene esta propiedad
            })
            .ToListAsync();

        // Verificar si el docente es propietario del curso
        var esPropietario = await _context.DocenteCursos
            .AnyAsync(dc => dc.DocenteId == docenteId && dc.CursoId == cursoId && dc.Activo && dc.EsPropietario);

        // Crear el ViewModel correcto
        var viewModel = new SeleccionarRamoViewModel
        {
            Curso = new CursoViewModel
            {
                Id = curso.Id,
                Nombre = curso.Nombre,
                Codigo = curso.Codigo,
                Descripcion = curso.Descripcion,
                EsPropietario = esPropietario,
                TotalRamos = ramosDisponibles.Count()
            },
            Ramos = ramosDisponibles
        };

        ViewData["Title"] = "Crear Nueva Clase - Paso 2: Seleccionar Ramo";
        
        return View(viewModel);
    }

    // PASO 3: Configurar y Crear Clase
    [Route("workflow/curso/{cursoId:int}/ramo/{ramoId:int}/crear-clase")]
    [Route("workflow/crear-clase/{cursoId:int}/{ramoId:int}")] // Compatibilidad
    public async Task<IActionResult> CrearClase(int cursoId, int ramoId)
    {
        var docenteId = GetCurrentUserId();

        // Verificar accesos
        var tieneAcceso = await VerificarAccesosCursoRamo(docenteId, cursoId, ramoId);
        if (!tieneAcceso.success)
        {
            TempData["ErrorMessage"] = tieneAcceso.message;
            return RedirectToAction("SeleccionarCurso");
        }

        var viewModel = new CrearClaseViewModel
        {
            CursoId = cursoId,
            RamoId = ramoId,
            CursoNombre = tieneAcceso.curso!.Nombre,
            RamoNombre = tieneAcceso.ramo!.Nombre,
            FechaClase = DateTime.Today,
            HoraInicio = DateTime.Now.TimeOfDay,
            DuracionMinutos = 90,
            ConfiguracionAsistencia = new ConfiguracionAsistenciaViewModel
            {
                LimitePresenteMinutos = 20,
                PermiteRegistroManual = false,
                NotificarTardanzas = true,
                MarcarAusenteAutomatico = true
            }
        };

        ViewData["Title"] = "Crear Nueva Clase - Paso 3: Configurar Clase";
        return View(viewModel);
    }

    // Vista para generar QRs físicos de estudiantes
    [Route("workflow/qr-students")]
    [Route("workflow/codigos-qr")]
    public IActionResult QRStudents()
    {
        ViewData["Title"] = "Códigos QR de Estudiantes";
        ViewData["Subtitle"] = "Genera códigos QR físicos para estudiantes sin internet";
        return View();
    }

    [HttpPost]
    [Route("workflow/crear-clase")]
    [Route("workflow/procesar-clase")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearClase([FromForm] CrearClaseViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var tieneAcceso = await VerificarAccesosCursoRamo(GetCurrentUserId(), model.CursoId, model.RamoId);
            model.CursoNombre = tieneAcceso.curso?.Nombre ?? "";
            model.RamoNombre = tieneAcceso.ramo?.Nombre ?? "";
            return View(model);
        }

        try
        {
            var docenteId = GetCurrentUserId();
            var inicioUtc = model.FechaClase.Date.Add(model.HoraInicio).ToUniversalTime();

            // Crear la clase
            var clase = new Clase
            {
                RamoId = model.RamoId,
                DocenteId = docenteId,
                Asignatura = model.NombreClase, // Mantener compatibilidad
                InicioUtc = inicioUtc,
                Descripcion = model.Descripcion
            };

            _context.Clases.Add(clase);
            await _context.SaveChangesAsync();

            // Crear configuración de asistencia
            var config = new ConfiguracionAsistencia
            {
                ClaseId = clase.Id,
                LimitePresenteMinutos = model.ConfiguracionAsistencia.LimitePresenteMinutos,
                PermiteRegistroManual = model.ConfiguracionAsistencia.PermiteRegistroManual,
                NotificarTardanzas = model.ConfiguracionAsistencia.NotificarTardanzas,
                MarcarAusenteAutomatico = model.ConfiguracionAsistencia.MarcarAusenteAutomatico
            };

            await _attendanceService.CreateOrUpdateConfiguracionAsync(clase.Id, config);

            TempData["SuccessMessage"] = $"Clase '{model.NombreClase}' creada exitosamente";
            return RedirectToAction("ClaseCreada", new { claseId = clase.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al crear la clase: {ex.Message}");
            return View(model);
        }
    }

    // PASO 4: Clase Creada - Mostrar QR y Opciones
    [Route("workflow/clase/{claseId:int}/creada")]
    [Route("workflow/clase-creada/{claseId:int}")] // Compatibilidad
    public async Task<IActionResult> ClaseCreada(int claseId)
    {
        var docenteId = GetCurrentUserId();

        var clase = await _context.Clases
            .Include(c => c.Ramo)
            .ThenInclude(r => r!.Curso)
            .Include(c => c.Docente)
            .FirstOrDefaultAsync(c => c.Id == claseId && c.DocenteId == docenteId);

        if (clase == null)
        {
            return NotFound();
        }

        // Generar QR de la clase
        var nonce = await GenerarTokenClase(claseId);
        var publicBaseUrl = _configuration["PublicBaseUrl"];
        var host = !string.IsNullOrWhiteSpace(publicBaseUrl) ? publicBaseUrl : $"{Request.Scheme}://{Request.Host}";
        var qrUrl = $"{host}/scan?claseId={claseId}&nonce={nonce}";
        var qrBase64 = _qrService.GenerateBase64Qr(qrUrl);

        // Obtener total de estudiantes inscritos
        var totalEstudiantes = await _context.AlumnoCursos
            .CountAsync(ac => ac.CursoId == clase.Ramo!.Curso.Id && ac.Activo);

        var viewModel = new ClaseCreadaViewModel
        {
            ClaseId = clase.Id,
            NombreClase = clase.Asignatura,
            CursoNombre = clase.Ramo?.Curso.Nombre ?? "",
            RamoNombre = clase.Ramo?.Nombre ?? "",
            DocenteNombre = clase.Docente.Nombre,
            FechaHoraInicio = clase.InicioUtc.ToLocalTime(),
            QrCodeBase64 = qrBase64,
            QrUrl = qrUrl,
            TotalEstudiantes = totalEstudiantes,
            ClaseActiva = clase.Activa
        };

        ViewData["Title"] = "Clase Creada Exitosamente";
        return View(viewModel);
    }

    // Métodos auxiliares
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 1;
    }

    private async Task<(bool success, string message, Curso? curso, Ramo? ramo)> VerificarAccesosCursoRamo(int docenteId, int cursoId, int ramoId)
    {
        var curso = await _context.Cursos.FindAsync(cursoId);
        if (curso == null)
            return (false, "Curso no encontrado", null, null);

        var ramo = await _context.Ramos.FindAsync(ramoId);
        if (ramo == null)
            return (false, "Ramo no encontrado", null, null);

        var tieneAccesoCurso = await _context.DocenteCursos
            .AnyAsync(dc => dc.DocenteId == docenteId && dc.CursoId == cursoId && dc.Activo);

        if (!tieneAccesoCurso)
            return (false, "No tienes acceso a este curso", null, null);

        var tieneAccesoRamo = await _context.DocenteRamos
            .AnyAsync(dr => dr.DocenteId == docenteId && dr.RamoId == ramoId && dr.Activo);

        if (!tieneAccesoRamo)
            return (false, "No tienes acceso a este ramo", null, null);

        return (true, "", curso, ramo);
    }

    private async Task<string> GenerarTokenClase(int claseId)
    {
        var nonce = Guid.NewGuid().ToString("N")[..8];
        var token = new QrClaseToken
        {
            ClaseId = claseId,
            Nonce = nonce,
            ExpiraUtc = DateTime.UtcNow.AddMinutes(300) // 5 horas
        };

        // Limpiar tokens expirados
        var tokensExpirados = await _context.QrClaseTokens
            .Where(t => t.ExpiraUtc < DateTime.UtcNow)
            .ToListAsync();
        
        _context.QrClaseTokens.RemoveRange(tokensExpirados);

        _context.QrClaseTokens.Add(token);
        await _context.SaveChangesAsync();
        
        return nonce;
    }

    // RUTAS ADICIONALES PARA NAVEGACIÓN Y GESTIÓN

    // Ver asistencias por curso
    [Route("workflow/curso/{cursoId:int}/asistencias")]
    public async Task<IActionResult> AsistenciasPorCurso(int cursoId)
    {
        var docenteId = GetCurrentUserId();

        // Verificar que el docente tiene acceso al curso
        var tieneAcceso = await _context.DocenteCursos
            .AnyAsync(dc => dc.DocenteId == docenteId && dc.CursoId == cursoId && dc.Activo);

        if (!tieneAcceso)
        {
            TempData["ErrorMessage"] = "No tienes acceso a este curso";
            return RedirectToAction("SeleccionarCurso");
        }

        var curso = await _context.Cursos
            .Include(c => c.Ramos.Where(r => r.Activo))
                .ThenInclude(r => r.Clases)
                    .ThenInclude(c => c.Asistencias)
            .FirstOrDefaultAsync(c => c.Id == cursoId);

        if (curso == null)
            return NotFound();

        ViewData["Title"] = $"Asistencias - {curso.Nombre}";
        return View("AsistenciasCurso", curso);
    }

    // Ver asistencias por ramo
    [Route("workflow/curso/{cursoId:int}/ramo/{ramoId:int}/asistencias")]
    public async Task<IActionResult> AsistenciasPorRamo(int cursoId, int ramoId)
    {
        var docenteId = GetCurrentUserId();

        // Verificar accesos
        var tieneAcceso = await VerificarAccesosCursoRamo(docenteId, cursoId, ramoId);
        if (!tieneAcceso.success)
        {
            TempData["ErrorMessage"] = tieneAcceso.message;
            return RedirectToAction("SeleccionarCurso");
        }

        var ramo = await _context.Ramos
            .Include(r => r.Curso)
            .Include(r => r.Clases)
                .ThenInclude(c => c.Asistencias)
                    .ThenInclude(a => a.Alumno)
            .FirstOrDefaultAsync(r => r.Id == ramoId);

        if (ramo == null)
            return NotFound();

        ViewData["Title"] = $"Asistencias - {ramo.Nombre}";
        return View("AsistenciasRamo", ramo);
    }

    // Ver asistencias por clase específica
    [Route("workflow/clase/{claseId:int}/asistencias")]
    public async Task<IActionResult> AsistenciasPorClase(int claseId)
    {
        var docenteId = GetCurrentUserId();

        // Primero obtener la clase básica
        var clase = await _context.Clases
            .Include(c => c.Ramo)
                .ThenInclude(r => r!.Curso)
            .FirstOrDefaultAsync(c => c.Id == claseId && c.DocenteId == docenteId);

        if (clase == null)
            return NotFound();

        // Obtener las asistencias por separado para evitar problemas de SQL
        var asistencias = await _context.Asistencias
            .Include(a => a.Alumno)
            .Where(a => a.ClaseId == claseId)
            .OrderBy(a => a.Alumno.Nombre)
            .ToListAsync();

        ViewData["Title"] = $"Asistencias - {clase.NombreCompleto}";
        ViewBag.Clase = clase;
        ViewBag.ClaseId = claseId;

        return View("Asistencias", asistencias);
    }

    // Gestionar clase específica (editar, finalizar, etc.)
    [Route("workflow/clase/{claseId:int}/gestionar")]
    public async Task<IActionResult> GestionarClase(int claseId)
    {
        var docenteId = GetCurrentUserId();

        var clase = await _context.Clases
            .Include(c => c.Ramo)
                .ThenInclude(r => r!.Curso)
            .Include(c => c.Docente)
            .FirstOrDefaultAsync(c => c.Id == claseId && c.DocenteId == docenteId);

        if (clase == null)
            return NotFound();

        // Obtener estadísticas de asistencia
        var totalAsistencias = await _context.Asistencias
            .CountAsync(a => a.ClaseId == claseId);

        var totalEstudiantes = 0;
        var esClaseLibre = clase.RamoId == null;

        if (!esClaseLibre && clase.Ramo?.Curso != null)
        {
            // Clase con curso - obtener estudiantes matriculados
            totalEstudiantes = await _context.AlumnoCursos
                .CountAsync(ac => ac.CursoId == clase.Ramo.Curso.Id && ac.Activo);
        }
        else
        {
            // Clase libre - usar el número de estudiantes que registraron asistencia
            totalEstudiantes = totalAsistencias;
        }

        var viewModel = new GestionarClaseViewModel
        {
            Clase = clase,
            TotalAsistencias = totalAsistencias,
            TotalEstudiantes = totalEstudiantes,
            PorcentajeAsistencia = totalEstudiantes > 0 ? (double)totalAsistencias / totalEstudiantes * 100 : 0,
            EsClaseLibre = esClaseLibre
        };

        ViewData["Title"] = $"Gestionar - {clase.Asignatura}";
        return View("GestionarClase", viewModel);
    }

    // Finalizar clase
    [HttpPost]
    [Route("workflow/clase/{claseId:int}/finalizar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalizarClase(int claseId)
    {
        var docenteId = GetCurrentUserId();

        var clase = await _context.Clases
            .FirstOrDefaultAsync(c => c.Id == claseId && c.DocenteId == docenteId);

        if (clase == null)
            return NotFound();

        clase.FinUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Clase finalizada exitosamente";
        return RedirectToAction("GestionarClase", new { claseId });
    }

    // Lista de clases del docente
    [Route("workflow/mis-clases")]
    [Route("workflow/misclases")]
    public async Task<IActionResult> MisClases()
    {
        var docenteId = GetCurrentUserId();

        var clases = await _context.Clases
            .Include(c => c.Ramo)
                .ThenInclude(r => r!.Curso)
            .Where(c => c.DocenteId == docenteId)
            .OrderByDescending(c => c.InicioUtc)
            .Take(50) // Limitar a 50 clases más recientes
            .ToListAsync();

        ViewData["Title"] = "Mis Clases";
        return View("MisClases", clases);
    }
}

// ViewModels
public class CursoViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool EsPropietario { get; set; }
    public int TotalRamos { get; set; }
}

public class RamoViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Descripcion { get; set; }
    public int TotalClasesActivas { get; set; }
    public int TotalClases { get; set; }
    public int TotalEstudiantes { get; set; }
    public int HorasSemanales { get; set; }
}

public class SeleccionarRamoViewModel
{
    public CursoViewModel Curso { get; set; } = new();
    public List<RamoViewModel> Ramos { get; set; } = new();
}

public class CrearClaseViewModel
{
    public int CursoId { get; set; }
    public int RamoId { get; set; }
    public string CursoNombre { get; set; } = "";
    public string RamoNombre { get; set; } = "";
    
    [Required(ErrorMessage = "El nombre de la clase es requerido")]
    public string NombreClase { get; set; } = "";
    
    [Required]
    public DateTime FechaClase { get; set; } = DateTime.Today;
    
    [Required]
    public TimeSpan HoraInicio { get; set; }
    
    [Range(30, 300, ErrorMessage = "La duración debe ser entre 30 y 300 minutos")]
    public int DuracionMinutos { get; set; } = 90;
    
    public string? Descripcion { get; set; }
    
    public ConfiguracionAsistenciaViewModel ConfiguracionAsistencia { get; set; } = new();
    
    public CursoViewModel Curso { get; set; } = new();
    public RamoViewModel Ramo { get; set; } = new();
}

public class ConfiguracionAsistenciaViewModel
{
    [Range(5, 60, ErrorMessage = "El límite debe ser entre 5 y 60 minutos")]
    public int LimitePresenteMinutos { get; set; } = 20;
    
    public bool PermiteRegistroManual { get; set; } = false;
    public bool NotificarTardanzas { get; set; } = true;
    public bool MarcarAusenteAutomatico { get; set; } = true;
}

public class ClaseCreadaViewModel
{
    public int ClaseId { get; set; }
    public string NombreClase { get; set; } = "";
    public string CursoNombre { get; set; } = "";
    public string RamoNombre { get; set; } = "";
    public string DocenteNombre { get; set; } = "";
    public DateTime FechaHoraInicio { get; set; }
    public string QrCodeBase64 { get; set; } = "";
    public string QrUrl { get; set; } = "";
    public int TotalEstudiantes { get; set; }
    public bool ClaseActiva { get; set; }
    
    // Propiedades adicionales para la vista
    public ClaseDetailViewModel Clase { get; set; } = new();
    public CursoViewModel Curso { get; set; } = new();
    public RamoViewModel Ramo { get; set; } = new();
    public string QrClaseBase64 { get; set; } = "";
}

public class ClaseDetailViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Asignatura { get; set; } = "";
    public string? Descripcion { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime? FinUtc { get; set; }
    public bool Activa { get; set; }
}

public class GestionarClaseViewModel
{
    public Models.Clase Clase { get; set; } = new();
    public int TotalAsistencias { get; set; }
    public int TotalEstudiantes { get; set; }
    public double PorcentajeAsistencia { get; set; }
    public bool EsClaseLibre { get; set; }
}