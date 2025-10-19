namespace registroAsistencia.Models;

// ==========================================
// DTOs PARA SISTEMA DE REPORTES MULTINIVEL
// ==========================================

/// <summary>
/// Filtro general para todos los reportes
/// </summary>
public class FiltroReporteDto
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int? CursoId { get; set; }
    public int? RamoId { get; set; }
    public int? ClaseId { get; set; }
    public int? AlumnoId { get; set; }
}

// ==========================================
// 1️⃣ REPORTE POR CURSO (Vista General)
// ==========================================

public class ReporteCursoDto
{
    public int CursoId { get; set; }
    public string CursoNombre { get; set; } = "";
    public string CursoCodigo { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    // Estadísticas generales del curso
    public int TotalEstudiantes { get; set; }
    public int TotalClasesEnPeriodo { get; set; }
    public decimal PromedioAsistenciaGeneral { get; set; }

    // Desglose por ramo
    public List<ReporteRamoResumenDto> Ramos { get; set; } = new();

    // Top estudiantes
    public List<EstudianteResumenDto> TopEstudiantes { get; set; } = new();
    public List<EstudianteResumenDto> EstudiantesEnRiesgo { get; set; } = new();
}

public class ReporteRamoResumenDto
{
    public int RamoId { get; set; }
    public string RamoNombre { get; set; } = "";
    public string RamoCodigo { get; set; } = "";
    public int TotalClases { get; set; }
    public decimal PromedioAsistencia { get; set; }
    public int TotalPresentes { get; set; }
    public int TotalTardanzas { get; set; }
    public int TotalAusentes { get; set; }
}

// ==========================================
// 2️⃣ REPORTE POR RAMO/MATERIA (Detalle Medio)
// ==========================================

public class ReporteRamoDetalladoDto
{
    public int RamoId { get; set; }
    public string RamoNombre { get; set; } = "";
    public string RamoCodigo { get; set; } = "";
    public int CursoId { get; set; }
    public string CursoNombre { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    // Estadísticas del ramo
    public int TotalClases { get; set; }
    public int TotalEstudiantes { get; set; }
    public decimal PromedioAsistencia { get; set; }

    // Asistencia por alumno en este ramo
    public List<AsistenciaAlumnoRamoDto> AsistenciasPorAlumno { get; set; } = new();

    // Detalle de cada clase
    public List<ClaseResumenDto> Clases { get; set; } = new();
}

public class AsistenciaAlumnoRamoDto
{
    public int AlumnoId { get; set; }
    public string AlumnoCodigo { get; set; } = "";
    public string AlumnoNombre { get; set; } = "";

    // Información del Ramo
    public int RamoId { get; set; }
    public string RamoCodigo { get; set; } = "";
    public string RamoNombre { get; set; } = "";
    public string CursoNombre { get; set; } = "";

    // Totales
    public int TotalClases { get; set; }
    public int TotalPresentes { get; set; }
    public int TotalTardanzas { get; set; }
    public int TotalAusentes { get; set; }
    public int TotalExcusados { get; set; }

    // Porcentajes
    public decimal PorcentajeAsistencia { get; set; }
    public decimal PorcentajeTardanzas { get; set; }
    public decimal PorcentajeAusencias { get; set; }

    // Indicadores
    public bool EnRiesgo => PorcentajeAsistencia < 75;
    public string Situacion => PorcentajeAsistencia switch
    {
        >= 90 => "Excelente",
        >= 80 => "Bueno",
        >= 70 => "Regular",
        >= 60 => "En Riesgo",
        _ => "Crítico"
    };
}

public class ClaseResumenDto
{
    public int ClaseId { get; set; }
    public DateTime FechaClase { get; set; }
    public string Descripcion { get; set; } = "";
    public bool Activa { get; set; }

    public int TotalPresentes { get; set; }
    public int TotalTardanzas { get; set; }
    public int TotalAusentes { get; set; }
    public decimal PorcentajeAsistencia { get; set; }
}

// ==========================================
// 3️⃣ REPORTE POR CLASE ESPECÍFICA (Detalle Fino)
// ==========================================

public class ReporteClaseDetalladoDto
{
    public int ClaseId { get; set; }
    public string Asignatura { get; set; } = "";
    public DateTime FechaClase { get; set; }
    public string? Descripcion { get; set; }
    public bool Activa { get; set; }

    // Información del ramo/curso
    public int? RamoId { get; set; }
    public string? RamoNombre { get; set; }
    public int? CursoId { get; set; }
    public string? CursoNombre { get; set; }

    // Información del docente
    public int DocenteId { get; set; }
    public string DocenteNombre { get; set; } = "";

    // Estadísticas
    public int TotalEstudiantes { get; set; }
    public int TotalPresentes { get; set; }
    public int TotalTardanzas { get; set; }
    public int TotalAusentes { get; set; }
    public int TotalExcusados { get; set; }
    public decimal PorcentajeAsistencia { get; set; }

    // Lista completa de asistencias
    public List<AsistenciaDetalleDto> Asistencias { get; set; } = new();
}

public class AsistenciaDetalleDto
{
    public int AsistenciaId { get; set; }
    public int AlumnoId { get; set; }
    public string AlumnoCodigo { get; set; } = "";
    public string AlumnoNombre { get; set; } = "";
    public EstadoAsistencia Estado { get; set; }
    public string EstadoTexto => Estado.ToString();
    public DateTime? FechaRegistro { get; set; }
    public int MinutosRetraso { get; set; }
    public MetodoRegistro Metodo { get; set; }
    public string MetodoTexto => Metodo switch
    {
        MetodoRegistro.QrEstudiante => "QR Estudiante",
        MetodoRegistro.QrDocente => "QR Docente",
        MetodoRegistro.Manual => "Manual",
        MetodoRegistro.AutoAusente => "Auto Ausente",
        _ => "Desconocido"
    };
    public bool EsRegistroManual { get; set; }
    public string? JustificacionManual { get; set; }
}

// ==========================================
// 4️⃣ REPORTE CONSOLIDADO POR ALUMNO
// ==========================================

public class ReporteAlumnoDto
{
    public int AlumnoId { get; set; }
    public string AlumnoCodigo { get; set; } = "";
    public string AlumnoNombre { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    // Estadísticas globales
    public int TotalClasesEnPeriodo { get; set; }
    public int TotalPresentes { get; set; }
    public int TotalTardanzas { get; set; }
    public int TotalAusentes { get; set; }
    public int TotalExcusados { get; set; }
    public decimal PorcentajeAsistenciaGlobal { get; set; }

    // Desglose por curso
    public List<AsistenciaAlumnoCursoDto> PorCurso { get; set; } = new();

    // Desglose por ramo
    public List<AsistenciaAlumnoRamoDto> PorRamo { get; set; } = new();
}

public class AsistenciaAlumnoCursoDto
{
    public int CursoId { get; set; }
    public string CursoNombre { get; set; } = "";
    public int TotalClases { get; set; }
    public int TotalPresentes { get; set; }
    public int TotalTardanzas { get; set; }
    public int TotalAusentes { get; set; }
    public decimal PorcentajeAsistencia { get; set; }
}

// ==========================================
// RESUMEN PARA DASHBOARDS
// ==========================================

public class EstudianteResumenDto
{
    public int AlumnoId { get; set; }
    public string AlumnoCodigo { get; set; } = "";
    public string AlumnoNombre { get; set; } = "";
    public decimal PorcentajeAsistencia { get; set; }
    public int TotalClases { get; set; }
    public int TotalPresentes { get; set; }
    public int TotalAusentes { get; set; }
}

// ==========================================
// ESTADÍSTICAS GENERALES
// ==========================================

public class EstadisticasGeneralesDto
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    public int TotalCursos { get; set; }
    public int TotalRamos { get; set; }
    public int TotalClases { get; set; }
    public int TotalEstudiantes { get; set; }
    public int TotalAsistencias { get; set; }

    public decimal PromedioAsistenciaGlobal { get; set; }
    public int TotalPresentes { get; set; }
    public int TotalTardanzas { get; set; }
    public int TotalAusentes { get; set; }

    // Top cursos por asistencia
    public List<CursoEstadisticaDto> TopCursosPorAsistencia { get; set; } = new();
}

public class CursoEstadisticaDto
{
    public int CursoId { get; set; }
    public string CursoNombre { get; set; } = "";
    public decimal PromedioAsistencia { get; set; }
    public int TotalClases { get; set; }
    public int TotalEstudiantes { get; set; }
}

// ==========================================
// PAGINACIÓN
// ==========================================

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PagedResult() { }

    public PagedResult(List<T> items, int totalItems, int page, int pageSize)
    {
        Items = items;
        TotalItems = totalItems;
        Page = page;
        PageSize = pageSize;
    }
}
