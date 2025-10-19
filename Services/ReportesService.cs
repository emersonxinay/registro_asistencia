using Microsoft.EntityFrameworkCore;
using registroAsistencia.Data;
using registroAsistencia.Models;

namespace registroAsistencia.Services;

public interface IReportesService
{
    Task<ReporteCursoDto> GenerarReporteCursoAsync(int cursoId, DateTime fechaInicio, DateTime fechaFin);
    Task<ReporteRamoDetalladoDto> GenerarReporteRamoAsync(int ramoId, DateTime fechaInicio, DateTime fechaFin);
    Task<ReporteClaseDetalladoDto> GenerarReporteClaseAsync(int claseId);
    Task<ReporteAlumnoDto> GenerarReporteAlumnoAsync(int alumnoId, DateTime fechaInicio, DateTime fechaFin);
    Task<EstadisticasGeneralesDto> GenerarEstadisticasGeneralesAsync(DateTime fechaInicio, DateTime fechaFin);
}

public class ReportesService : IReportesService
{
    private readonly ApplicationDbContext _context;

    public ReportesService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // 1️⃣ REPORTE POR CURSO
    // ==========================================
    public async Task<ReporteCursoDto> GenerarReporteCursoAsync(int cursoId, DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            // Convertir fechas a UTC para PostgreSQL
            var fechaInicioUtc = DateTime.SpecifyKind(fechaInicio, DateTimeKind.Utc);
            var fechaFinUtc = DateTime.SpecifyKind(fechaFin.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

            var curso = await _context.Cursos
                .Include(c => c.Ramos)
                .Include(c => c.AlumnoCursos)
                .FirstOrDefaultAsync(c => c.Id == cursoId);

            if (curso == null)
                throw new InvalidOperationException($"Curso con ID {cursoId} no encontrado");

        // Obtener todos los estudiantes del curso
        var estudiantesIds = await _context.AlumnoCursos
            .Where(ac => ac.CursoId == cursoId)
            .Select(ac => ac.AlumnoId)
            .ToListAsync();

        var totalEstudiantes = estudiantesIds.Count;

        // Obtener todas las clases de los ramos del curso en el periodo
        var ramosIds = curso.Ramos.Select(r => r.Id).ToList();

        var clases = await _context.Clases
            .Where(c => c.RamoId.HasValue && ramosIds.Contains(c.RamoId.Value))
            .Where(c => c.InicioUtc >= fechaInicioUtc && c.InicioUtc <= fechaFinUtc)
            .ToListAsync();

        var totalClasesEnPeriodo = clases.Count;

        // Obtener todas las asistencias del periodo
        var clasesIds = clases.Select(c => c.Id).ToList();
        var asistencias = await _context.Asistencias
            .Include(a => a.Alumno)
            .Include(a => a.Clase)
            .Where(a => clasesIds.Contains(a.ClaseId))
            .Where(a => estudiantesIds.Contains(a.AlumnoId))
            .ToListAsync();

        // Calcular promedio general
        var totalAsistenciasEsperadas = totalClasesEnPeriodo * totalEstudiantes;
        var totalPresentes = asistencias.Count(a => a.Estado == EstadoAsistencia.Presente || a.Estado == EstadoAsistencia.Tardanza);
        var promedioGeneral = totalAsistenciasEsperadas > 0
            ? Math.Round((decimal)totalPresentes / totalAsistenciasEsperadas * 100, 2)
            : 0;

        // Generar reporte por ramo
        var reportesRamos = new List<ReporteRamoResumenDto>();
        foreach (var ramo in curso.Ramos)
        {
            var clasesRamo = clases.Where(c => c.RamoId == ramo.Id).ToList();
            var clasesRamoIds = clasesRamo.Select(c => c.Id).ToList();
            var asistenciasRamo = asistencias.Where(a => clasesRamoIds.Contains(a.ClaseId)).ToList();

            var totalClasesRamo = clasesRamo.Count;
            var totalPresentesRamo = asistenciasRamo.Count(a => a.Estado == EstadoAsistencia.Presente || a.Estado == EstadoAsistencia.Tardanza);
            var totalTardanzasRamo = asistenciasRamo.Count(a => a.Estado == EstadoAsistencia.Tardanza);
            var totalAusentesRamo = asistenciasRamo.Count(a => a.Estado == EstadoAsistencia.Ausente);

            var esperadasRamo = totalClasesRamo * totalEstudiantes;
            var promedioRamo = esperadasRamo > 0
                ? Math.Round((decimal)totalPresentesRamo / esperadasRamo * 100, 2)
                : 0;

            reportesRamos.Add(new ReporteRamoResumenDto
            {
                RamoId = ramo.Id,
                RamoNombre = ramo.Nombre,
                RamoCodigo = ramo.Codigo,
                TotalClases = totalClasesRamo,
                PromedioAsistencia = promedioRamo,
                TotalPresentes = totalPresentesRamo,
                TotalTardanzas = totalTardanzasRamo,
                TotalAusentes = totalAusentesRamo
            });
        }

        // Top estudiantes y en riesgo
        var estudiantesStats = await CalcularEstadisticasEstudiantesAsync(estudiantesIds, clasesIds, asistencias);
        var topEstudiantes = estudiantesStats.OrderByDescending(e => e.PorcentajeAsistencia).Take(5).ToList();
        var estudiantesEnRiesgo = estudiantesStats.Where(e => e.PorcentajeAsistencia < 75).OrderBy(e => e.PorcentajeAsistencia).ToList();

        return new ReporteCursoDto
        {
            CursoId = curso.Id,
            CursoNombre = curso.Nombre,
            CursoCodigo = curso.Codigo,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            TotalEstudiantes = totalEstudiantes,
            TotalClasesEnPeriodo = totalClasesEnPeriodo,
            PromedioAsistenciaGeneral = promedioGeneral,
            Ramos = reportesRamos,
            TopEstudiantes = topEstudiantes,
            EstudiantesEnRiesgo = estudiantesEnRiesgo
        };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al generar reporte de curso: {ex.Message}", ex);
        }
    }

    // ==========================================
    // 2️⃣ REPORTE POR RAMO
    // ==========================================
    public async Task<ReporteRamoDetalladoDto> GenerarReporteRamoAsync(int ramoId, DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            // Convertir fechas a UTC para PostgreSQL
            var fechaInicioUtc = DateTime.SpecifyKind(fechaInicio, DateTimeKind.Utc);
            var fechaFinUtc = DateTime.SpecifyKind(fechaFin.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

            var ramo = await _context.Ramos
                .Include(r => r.Curso)
                .ThenInclude(c => c.AlumnoCursos)
                .FirstOrDefaultAsync(r => r.Id == ramoId);

            if (ramo == null)
                throw new InvalidOperationException($"Ramo con ID {ramoId} no encontrado");

            if (ramo.Curso == null)
                throw new InvalidOperationException($"El ramo {ramoId} no tiene un curso asociado");

        // Obtener clases del ramo en el periodo
        var clases = await _context.Clases
            .Where(c => c.RamoId == ramoId)
            .Where(c => c.InicioUtc >= fechaInicioUtc && c.InicioUtc <= fechaFinUtc)
            .OrderBy(c => c.InicioUtc)
            .ToListAsync();

        var clasesIds = clases.Select(c => c.Id).ToList();

        // Obtener estudiantes del curso
        var estudiantesIds = ramo.Curso.AlumnoCursos.Select(ac => ac.AlumnoId).ToList();
        var estudiantes = await _context.Alumnos
            .Where(a => estudiantesIds.Contains(a.Id))
            .OrderBy(a => a.Nombre)
            .ToListAsync();

        // Obtener asistencias
        var asistencias = await _context.Asistencias
            .Where(a => clasesIds.Contains(a.ClaseId))
            .ToListAsync();

        // Calcular estadísticas por alumno
        var asistenciasPorAlumno = new List<AsistenciaAlumnoRamoDto>();
        foreach (var estudiante in estudiantes)
        {
            var asistenciasEstudiante = asistencias.Where(a => a.AlumnoId == estudiante.Id).ToList();

            var totalClases = clases.Count;
            var totalPresentes = asistenciasEstudiante.Count(a => a.Estado == EstadoAsistencia.Presente);
            var totalTardanzas = asistenciasEstudiante.Count(a => a.Estado == EstadoAsistencia.Tardanza);
            var totalAusentes = asistenciasEstudiante.Count(a => a.Estado == EstadoAsistencia.Ausente);
            var totalExcusados = asistenciasEstudiante.Count(a => a.Estado == EstadoAsistencia.Excusado);

            var porcentajeAsistencia = totalClases > 0
                ? Math.Round((decimal)(totalPresentes + totalTardanzas) / totalClases * 100, 2)
                : 0;
            var porcentajeTardanzas = totalClases > 0
                ? Math.Round((decimal)totalTardanzas / totalClases * 100, 2)
                : 0;
            var porcentajeAusencias = totalClases > 0
                ? Math.Round((decimal)totalAusentes / totalClases * 100, 2)
                : 0;

            asistenciasPorAlumno.Add(new AsistenciaAlumnoRamoDto
            {
                AlumnoId = estudiante.Id,
                AlumnoCodigo = estudiante.Codigo,
                AlumnoNombre = estudiante.Nombre,
                TotalClases = totalClases,
                TotalPresentes = totalPresentes,
                TotalTardanzas = totalTardanzas,
                TotalAusentes = totalAusentes,
                TotalExcusados = totalExcusados,
                PorcentajeAsistencia = porcentajeAsistencia,
                PorcentajeTardanzas = porcentajeTardanzas,
                PorcentajeAusencias = porcentajeAusencias
            });
        }

        // Resumen de clases
        var clasesResumen = clases.Select(c =>
        {
            var asistenciasClase = asistencias.Where(a => a.ClaseId == c.Id).ToList();
            var presentes = asistenciasClase.Count(a => a.Estado == EstadoAsistencia.Presente || a.Estado == EstadoAsistencia.Tardanza);
            var tardanzas = asistenciasClase.Count(a => a.Estado == EstadoAsistencia.Tardanza);
            var ausentes = asistenciasClase.Count(a => a.Estado == EstadoAsistencia.Ausente);
            var porcentaje = estudiantes.Count > 0
                ? Math.Round((decimal)presentes / estudiantes.Count * 100, 2)
                : 0;

            return new ClaseResumenDto
            {
                ClaseId = c.Id,
                FechaClase = c.InicioUtc,
                Descripcion = c.Descripcion ?? "",
                Activa = c.Activa,
                TotalPresentes = presentes,
                TotalTardanzas = tardanzas,
                TotalAusentes = ausentes,
                PorcentajeAsistencia = porcentaje
            };
        }).ToList();

        var totalAsistenciasEsperadas = clases.Count * estudiantes.Count;
        var totalPresentesGlobal = asistencias.Count(a => a.Estado == EstadoAsistencia.Presente || a.Estado == EstadoAsistencia.Tardanza);
        var promedioAsistencia = totalAsistenciasEsperadas > 0
            ? Math.Round((decimal)totalPresentesGlobal / totalAsistenciasEsperadas * 100, 2)
            : 0;

        return new ReporteRamoDetalladoDto
        {
            RamoId = ramo.Id,
            RamoNombre = ramo.Nombre,
            RamoCodigo = ramo.Codigo,
            CursoId = ramo.Curso.Id,
            CursoNombre = ramo.Curso.Nombre,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            TotalClases = clases.Count,
            TotalEstudiantes = estudiantes.Count,
            PromedioAsistencia = promedioAsistencia,
            AsistenciasPorAlumno = asistenciasPorAlumno,
            Clases = clasesResumen
        };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al generar reporte de ramo: {ex.Message}", ex);
        }
    }

    // ==========================================
    // 3️⃣ REPORTE POR CLASE
    // ==========================================
    public async Task<ReporteClaseDetalladoDto> GenerarReporteClaseAsync(int claseId)
    {
        var clase = await _context.Clases
            .Include(c => c.Ramo)
                .ThenInclude(r => r!.Curso)
            .Include(c => c.Docente)
            .FirstOrDefaultAsync(c => c.Id == claseId);

        if (clase == null)
            throw new InvalidOperationException("Clase no encontrada");

        // Obtener asistencias
        var asistencias = await _context.Asistencias
            .Include(a => a.Alumno)
            .Where(a => a.ClaseId == claseId)
            .OrderBy(a => a.Alumno.Nombre)
            .ToListAsync();

        // Obtener total de estudiantes esperados
        int totalEstudiantes = 0;
        if (clase.RamoId.HasValue && clase.Ramo?.Curso != null)
        {
            totalEstudiantes = await _context.AlumnoCursos
                .Where(ac => ac.CursoId == clase.Ramo.Curso.Id)
                .CountAsync();
        }

        var totalPresentes = asistencias.Count(a => a.Estado == EstadoAsistencia.Presente);
        var totalTardanzas = asistencias.Count(a => a.Estado == EstadoAsistencia.Tardanza);
        var totalAusentes = asistencias.Count(a => a.Estado == EstadoAsistencia.Ausente);
        var totalExcusados = asistencias.Count(a => a.Estado == EstadoAsistencia.Excusado);

        var porcentajeAsistencia = totalEstudiantes > 0
            ? Math.Round((decimal)(totalPresentes + totalTardanzas) / totalEstudiantes * 100, 2)
            : 0;

        var asistenciasDetalle = asistencias.Select(a => new AsistenciaDetalleDto
        {
            AsistenciaId = a.Id,
            AlumnoId = a.AlumnoId,
            AlumnoCodigo = a.Alumno.Codigo,
            AlumnoNombre = a.Alumno.Nombre,
            Estado = a.Estado,
            FechaRegistro = a.MarcadaUtc,
            MinutosRetraso = a.MinutosRetraso,
            Metodo = a.Metodo,
            EsRegistroManual = a.EsRegistroManual,
            JustificacionManual = a.JustificacionManual
        }).ToList();

        return new ReporteClaseDetalladoDto
        {
            ClaseId = clase.Id,
            Asignatura = clase.Asignatura,
            FechaClase = clase.InicioUtc,
            Descripcion = clase.Descripcion,
            Activa = clase.Activa,
            RamoId = clase.RamoId,
            RamoNombre = clase.Ramo?.Nombre,
            CursoId = clase.Ramo?.Curso?.Id,
            CursoNombre = clase.Ramo?.Curso?.Nombre,
            DocenteId = clase.DocenteId,
            DocenteNombre = clase.Docente.Nombre,
            TotalEstudiantes = totalEstudiantes,
            TotalPresentes = totalPresentes,
            TotalTardanzas = totalTardanzas,
            TotalAusentes = totalAusentes,
            TotalExcusados = totalExcusados,
            PorcentajeAsistencia = porcentajeAsistencia,
            Asistencias = asistenciasDetalle
        };
    }

    // ==========================================
    // 4️⃣ REPORTE POR ALUMNO
    // ==========================================
    public async Task<ReporteAlumnoDto> GenerarReporteAlumnoAsync(int alumnoId, DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            // Convertir fechas a UTC para PostgreSQL
            var fechaInicioUtc = DateTime.SpecifyKind(fechaInicio, DateTimeKind.Utc);
            var fechaFinUtc = DateTime.SpecifyKind(fechaFin.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

            var alumno = await _context.Alumnos
                .Include(a => a.AlumnoCursos)
                    .ThenInclude(ac => ac.Curso)
                .FirstOrDefaultAsync(a => a.Id == alumnoId);

            if (alumno == null)
                throw new InvalidOperationException($"Alumno con ID {alumnoId} no encontrado");

        // Obtener todas las asistencias del alumno en el periodo
        var asistencias = await _context.Asistencias
            .Include(a => a.Clase)
                .ThenInclude(c => c.Ramo)
                    .ThenInclude(r => r!.Curso)
            .Where(a => a.AlumnoId == alumnoId)
            .Where(a => a.MarcadaUtc >= fechaInicioUtc && a.MarcadaUtc <= fechaFinUtc)
            .ToListAsync();

        var totalPresentes = asistencias.Count(a => a.Estado == EstadoAsistencia.Presente);
        var totalTardanzas = asistencias.Count(a => a.Estado == EstadoAsistencia.Tardanza);
        var totalAusentes = asistencias.Count(a => a.Estado == EstadoAsistencia.Ausente);
        var totalExcusados = asistencias.Count(a => a.Estado == EstadoAsistencia.Excusado);

        var totalClasesEnPeriodo = asistencias.Count;
        var porcentajeGlobal = totalClasesEnPeriodo > 0
            ? Math.Round((decimal)(totalPresentes + totalTardanzas) / totalClasesEnPeriodo * 100, 2)
            : 0;

        // Agrupar por ramo
        var porRamo = asistencias
            .Where(a => a.Clase.RamoId.HasValue)
            .GroupBy(a => a.Clase.RamoId!.Value)
            .Select(g =>
            {
                var ramoAsistencias = g.ToList();
                var ramo = ramoAsistencias.First().Clase.Ramo;
                var presentes = ramoAsistencias.Count(a => a.Estado == EstadoAsistencia.Presente);
                var tardanzas = ramoAsistencias.Count(a => a.Estado == EstadoAsistencia.Tardanza);
                var ausentes = ramoAsistencias.Count(a => a.Estado == EstadoAsistencia.Ausente);
                var excusados = ramoAsistencias.Count(a => a.Estado == EstadoAsistencia.Excusado);
                var total = ramoAsistencias.Count;
                var porcentaje = total > 0
                    ? Math.Round((decimal)(presentes + tardanzas) / total * 100, 2)
                    : 0;

                return new AsistenciaAlumnoRamoDto
                {
                    AlumnoId = alumnoId,
                    AlumnoCodigo = alumno.Codigo,
                    AlumnoNombre = alumno.Nombre,
                    RamoId = ramo?.Id ?? 0,
                    RamoCodigo = ramo?.Codigo ?? "N/A",
                    RamoNombre = ramo?.Nombre ?? "Sin nombre",
                    CursoNombre = ramo?.Curso?.Nombre ?? "N/A",
                    TotalClases = total,
                    TotalPresentes = presentes,
                    TotalTardanzas = tardanzas,
                    TotalAusentes = ausentes,
                    TotalExcusados = excusados,
                    PorcentajeAsistencia = porcentaje,
                    PorcentajeTardanzas = total > 0 ? Math.Round((decimal)tardanzas / total * 100, 2) : 0,
                    PorcentajeAusencias = total > 0 ? Math.Round((decimal)ausentes / total * 100, 2) : 0
                };
            }).ToList();

        return new ReporteAlumnoDto
        {
            AlumnoId = alumno.Id,
            AlumnoCodigo = alumno.Codigo,
            AlumnoNombre = alumno.Nombre,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            TotalClasesEnPeriodo = totalClasesEnPeriodo,
            TotalPresentes = totalPresentes,
            TotalTardanzas = totalTardanzas,
            TotalAusentes = totalAusentes,
            TotalExcusados = totalExcusados,
            PorcentajeAsistenciaGlobal = porcentajeGlobal,
            PorRamo = porRamo
        };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al generar reporte de alumno: {ex.Message}", ex);
        }
    }

    // ==========================================
    // 5️⃣ ESTADÍSTICAS GENERALES
    // ==========================================
    public async Task<EstadisticasGeneralesDto> GenerarEstadisticasGeneralesAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        // Convertir fechas a UTC para PostgreSQL
        var fechaInicioUtc = DateTime.SpecifyKind(fechaInicio, DateTimeKind.Utc);
        var fechaFinUtc = DateTime.SpecifyKind(fechaFin.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var totalCursos = await _context.Cursos.CountAsync();
        var totalRamos = await _context.Ramos.CountAsync();
        var totalClases = await _context.Clases
            .Where(c => c.InicioUtc >= fechaInicioUtc && c.InicioUtc <= fechaFinUtc)
            .CountAsync();
        var totalEstudiantes = await _context.Alumnos.CountAsync();

        var asistencias = await _context.Asistencias
            .Where(a => a.MarcadaUtc >= fechaInicioUtc && a.MarcadaUtc <= fechaFinUtc)
            .ToListAsync();

        var totalAsistencias = asistencias.Count;
        var totalPresentes = asistencias.Count(a => a.Estado == EstadoAsistencia.Presente || a.Estado == EstadoAsistencia.Tardanza);
        var totalTardanzas = asistencias.Count(a => a.Estado == EstadoAsistencia.Tardanza);
        var totalAusentes = asistencias.Count(a => a.Estado == EstadoAsistencia.Ausente);

        var promedioGlobal = totalAsistencias > 0
            ? Math.Round((decimal)totalPresentes / totalAsistencias * 100, 2)
            : 0;

        return new EstadisticasGeneralesDto
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            TotalCursos = totalCursos,
            TotalRamos = totalRamos,
            TotalClases = totalClases,
            TotalEstudiantes = totalEstudiantes,
            TotalAsistencias = totalAsistencias,
            PromedioAsistenciaGlobal = promedioGlobal,
            TotalPresentes = totalPresentes,
            TotalTardanzas = totalTardanzas,
            TotalAusentes = totalAusentes
        };
    }

    // ==========================================
    // HELPERS
    // ==========================================
    private async Task<List<EstudianteResumenDto>> CalcularEstadisticasEstudiantesAsync(
        List<int> estudiantesIds,
        List<int> clasesIds,
        List<Asistencia> asistencias)
    {
        var estudiantes = await _context.Alumnos
            .Where(a => estudiantesIds.Contains(a.Id))
            .ToListAsync();

        var totalClases = clasesIds.Count;

        return estudiantes.Select(e =>
        {
            var asistenciasEstudiante = asistencias.Where(a => a.AlumnoId == e.Id).ToList();
            var presentes = asistenciasEstudiante.Count(a => a.Estado == EstadoAsistencia.Presente || a.Estado == EstadoAsistencia.Tardanza);
            var ausentes = asistenciasEstudiante.Count(a => a.Estado == EstadoAsistencia.Ausente);
            var porcentaje = totalClases > 0
                ? Math.Round((decimal)presentes / totalClases * 100, 2)
                : 0;

            return new EstudianteResumenDto
            {
                AlumnoId = e.Id,
                AlumnoCodigo = e.Codigo,
                AlumnoNombre = e.Nombre,
                PorcentajeAsistencia = porcentaje,
                TotalClases = totalClases,
                TotalPresentes = presentes,
                TotalAusentes = ausentes
            };
        }).ToList();
    }
}
