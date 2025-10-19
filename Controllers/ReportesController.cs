using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using registroAsistencia.Models;
using registroAsistencia.Services;

namespace registroAsistencia.Controllers;

[Authorize]
public class ReportesController : Controller
{
    private readonly IReportesService _reportesService;
    private readonly IDataService _dataService;
    private readonly ICsvService _csvService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReportesController(
        IReportesService reportesService,
        IDataService dataService,
        ICsvService csvService,
        IHttpContextAccessor httpContextAccessor)
    {
        _reportesService = reportesService;
        _dataService = dataService;
        _csvService = csvService;
        _httpContextAccessor = httpContextAccessor;
    }

    // ==========================================
    // VISTA PRINCIPAL
    // ==========================================
    [HttpGet]
    [Route("reportes")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Reportes de Asistencia";
        return View();
    }

    // ==========================================
    // API ENDPOINTS
    // ==========================================

    /// <summary>
    /// Obtener lista de cursos para el selector
    /// </summary>
    [HttpGet]
    [Route("api/reportes/cursos")]
    public async Task<IActionResult> GetCursos()
    {
        try
        {
            var cursos = await _dataService.GetCursosAsync();

            // TODO: Cuando se implemente la relación Curso-Docente, filtrar por docente aquí
            // Por ahora, todos los usuarios autenticados pueden ver todos los cursos

            var cursosDto = cursos.Select(c => new
            {
                id = c.Id,
                nombre = c.Nombre,
                codigo = c.Codigo
            });
            return Ok(cursosDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al obtener cursos: {ex.Message}" });
        }
    }

    /// <summary>
    /// Obtener lista de ramos por curso para el selector
    /// </summary>
    [HttpGet]
    [Route("api/reportes/cursos/{cursoId}/ramos")]
    public async Task<IActionResult> GetRamosByCurso(int cursoId)
    {
        try
        {
            var ramos = await _dataService.GetRamosByCursoAsync(cursoId);
            var ramosDto = ramos.Select(r => new
            {
                id = r.Id,
                nombre = r.Nombre,
                codigo = r.Codigo
            });
            return Ok(ramosDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al obtener ramos: {ex.Message}" });
        }
    }

    /// <summary>
    /// Obtener lista de alumnos por curso para el selector
    /// </summary>
    [HttpGet]
    [Route("api/reportes/cursos/{cursoId}/alumnos")]
    public async Task<IActionResult> GetAlumnosByCurso(int cursoId)
    {
        try
        {
            // Obtener todos los alumnos y filtrar por curso
            var todosAlumnos = await _dataService.GetAlumnosAsync();
            var alumnosCurso = await _dataService.GetAlumnosCursoAsync(cursoId);
            var alumnosIds = alumnosCurso.Select(ac => ac.AlumnoId).ToList();

            var alumnosDto = todosAlumnos
                .Where(a => alumnosIds.Contains(a.Id))
                .Select(a => new
                {
                    id = a.Id,
                    nombre = a.Nombre,
                    codigo = a.Codigo
                });

            return Ok(alumnosDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al obtener alumnos: {ex.Message}" });
        }
    }

    /// <summary>
    /// Generar reporte por curso
    /// </summary>
    [HttpGet]
    [Route("api/reportes/curso")]
    public async Task<IActionResult> ReportePorCurso(
        [FromQuery] int cursoId,
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin)
    {
        try
        {
            // TODO: Validar permisos cuando se implemente relación Curso-Docente
            var reporte = await _reportesService.GenerarReporteCursoAsync(cursoId, fechaInicio, fechaFin);
            return Ok(reporte);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al generar reporte: {ex.Message}" });
        }
    }

    /// <summary>
    /// Generar reporte por ramo/materia
    /// </summary>
    [HttpGet]
    [Route("api/reportes/ramo")]
    public async Task<IActionResult> ReportePorRamo(
        [FromQuery] int ramoId,
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin)
    {
        try
        {
            // TODO: Validar permisos cuando se implemente relación Curso-Docente
            var reporte = await _reportesService.GenerarReporteRamoAsync(ramoId, fechaInicio, fechaFin);
            return Ok(reporte);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al generar reporte: {ex.Message}" });
        }
    }

    /// <summary>
    /// Generar reporte por clase
    /// </summary>
    [HttpGet]
    [Route("api/reportes/clase/{claseId}")]
    public async Task<IActionResult> ReportePorClase(int claseId)
    {
        try
        {
            var reporte = await _reportesService.GenerarReporteClaseAsync(claseId);
            return Ok(reporte);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al generar reporte: {ex.Message}" });
        }
    }

    /// <summary>
    /// Generar reporte por alumno
    /// </summary>
    [HttpGet]
    [Route("api/reportes/alumno")]
    public async Task<IActionResult> ReportePorAlumno(
        [FromQuery] int alumnoId,
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin)
    {
        try
        {
            // TODO: Validar permisos cuando se implemente relación Curso-Docente
            var reporte = await _reportesService.GenerarReporteAlumnoAsync(alumnoId, fechaInicio, fechaFin);
            return Ok(reporte);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al generar reporte: {ex.Message}" });
        }
    }

    /// <summary>
    /// Generar estadísticas generales (Solo Administradores)
    /// </summary>
    [HttpGet]
    [Route("api/reportes/estadisticas-generales")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EstadisticasGenerales(
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin)
    {
        try
        {
            var estadisticas = await _reportesService.GenerarEstadisticasGeneralesAsync(fechaInicio, fechaFin);
            return Ok(estadisticas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al generar estadísticas: {ex.Message}" });
        }
    }

    // ==========================================
    // EXPORTACIÓN
    // ==========================================

    /// <summary>
    /// Exportar reporte de curso a CSV
    /// </summary>
    [HttpGet]
    [Route("api/reportes/curso/export-csv")]
    public async Task<IActionResult> ExportarReporteCursoCSV(
        [FromQuery] int cursoId,
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin)
    {
        try
        {
            // TODO: Validar permisos cuando se implemente relación Curso-Docente
            var reporte = await _reportesService.GenerarReporteCursoAsync(cursoId, fechaInicio, fechaFin);

            // Generar CSV (implementación básica)
            var csv = GenerarCSVReporteCurso(reporte);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", $"reporte-curso-{reporte.CursoCodigo}-{DateTime.Now:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al exportar: {ex.Message}" });
        }
    }

    /// <summary>
    /// Exportar reporte de ramo a CSV
    /// </summary>
    [HttpGet]
    [Route("api/reportes/ramo/export-csv")]
    public async Task<IActionResult> ExportarReporteRamoCSV(
        [FromQuery] int ramoId,
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin)
    {
        try
        {
            // TODO: Validar permisos cuando se implemente relación Curso-Docente
            var reporte = await _reportesService.GenerarReporteRamoAsync(ramoId, fechaInicio, fechaFin);

            // Generar CSV
            var csv = GenerarCSVReporteRamo(reporte);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", $"reporte-ramo-{reporte.RamoCodigo}-{DateTime.Now:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al exportar: {ex.Message}" });
        }
    }

    // ==========================================
    // HELPERS - GENERACIÓN DE CSV
    // ==========================================

    private string GenerarCSVReporteCurso(ReporteCursoDto reporte)
    {
        var csv = new System.Text.StringBuilder();

        // Header
        csv.AppendLine($"REPORTE DE ASISTENCIA - CURSO: {reporte.CursoNombre}");
        csv.AppendLine($"Periodo: {reporte.FechaInicio:dd/MM/yyyy} - {reporte.FechaFin:dd/MM/yyyy}");
        csv.AppendLine($"Total Estudiantes: {reporte.TotalEstudiantes}");
        csv.AppendLine($"Promedio General: {reporte.PromedioAsistenciaGeneral}%");
        csv.AppendLine();

        // Ramos
        csv.AppendLine("DESGLOSE POR RAMO");
        csv.AppendLine("Código,Nombre,Total Clases,Promedio Asistencia,Presentes,Tardanzas,Ausentes");
        foreach (var ramo in reporte.Ramos)
        {
            csv.AppendLine($"{ramo.RamoCodigo},{ramo.RamoNombre},{ramo.TotalClases},{ramo.PromedioAsistencia}%,{ramo.TotalPresentes},{ramo.TotalTardanzas},{ramo.TotalAusentes}");
        }

        csv.AppendLine();

        // Top estudiantes
        csv.AppendLine("TOP ESTUDIANTES");
        csv.AppendLine("Código,Nombre,Porcentaje,Total Clases,Presentes,Ausentes");
        foreach (var estudiante in reporte.TopEstudiantes)
        {
            csv.AppendLine($"{estudiante.AlumnoCodigo},{estudiante.AlumnoNombre},{estudiante.PorcentajeAsistencia}%,{estudiante.TotalClases},{estudiante.TotalPresentes},{estudiante.TotalAusentes}");
        }

        return csv.ToString();
    }

    private string GenerarCSVReporteRamo(ReporteRamoDetalladoDto reporte)
    {
        var csv = new System.Text.StringBuilder();

        // Header
        csv.AppendLine($"REPORTE DE ASISTENCIA - RAMO: {reporte.RamoNombre}");
        csv.AppendLine($"Curso: {reporte.CursoNombre}");
        csv.AppendLine($"Periodo: {reporte.FechaInicio:dd/MM/yyyy} - {reporte.FechaFin:dd/MM/yyyy}");
        csv.AppendLine($"Total Clases: {reporte.TotalClases}");
        csv.AppendLine($"Promedio: {reporte.PromedioAsistencia}%");
        csv.AppendLine();

        // Por alumno
        csv.AppendLine("ASISTENCIA POR ESTUDIANTE");
        csv.AppendLine("Código,Nombre,Total Clases,Presentes,Tardanzas,Ausentes,% Asistencia,Situación");
        foreach (var alumno in reporte.AsistenciasPorAlumno)
        {
            csv.AppendLine($"{alumno.AlumnoCodigo},{alumno.AlumnoNombre},{alumno.TotalClases},{alumno.TotalPresentes},{alumno.TotalTardanzas},{alumno.TotalAusentes},{alumno.PorcentajeAsistencia}%,{alumno.Situacion}");
        }

        return csv.ToString();
    }
}
