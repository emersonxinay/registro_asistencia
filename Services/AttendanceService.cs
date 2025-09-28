using Microsoft.EntityFrameworkCore;
using registroAsistencia.Data;
using registroAsistencia.Models;
using System.Security.Claims;

namespace registroAsistencia.Services;

public interface IAttendanceService
{
    Task<Asistencia> RegistrarAsistenciaAsync(int alumnoId, int claseId, MetodoRegistro metodo, int? docenteId = null, string? justificacion = null);
    Task<EstadoAsistencia> CalcularEstadoAsistencia(DateTime marcadaUtc, DateTime inicioClaseUtc, DateTime? finClaseUtc = null);
    Task<bool> CerrarClaseYMarcarAusentesAsync(int claseId);
    Task<IEnumerable<AsistenciaResumen>> GetAsistenciasResumenAsync(int claseId);
    Task<bool> ModificarAsistenciaManualAsync(int asistenciaId, EstadoAsistencia nuevoEstado, string justificacion, int docenteId);
    Task<ConfiguracionAsistencia> GetConfiguracionAsync(int claseId);
    Task<ConfiguracionAsistencia> CreateOrUpdateConfiguracionAsync(int claseId, ConfiguracionAsistencia config);
}

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AttendanceService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Asistencia> RegistrarAsistenciaAsync(int alumnoId, int claseId, MetodoRegistro metodo, int? docenteId = null, string? justificacion = null)
    {
        // Verificar si ya existe asistencia
        var existeAsistencia = await _context.Asistencias
            .AnyAsync(a => a.AlumnoId == alumnoId && a.ClaseId == claseId);
        
        if (existeAsistencia)
            throw new InvalidOperationException("Ya existe registro de asistencia para este estudiante");

        // Obtener información de la clase
        var clase = await _context.Clases.FindAsync(claseId);
        if (clase == null)
            throw new ArgumentException("Clase no encontrada");

        var ahora = DateTime.UtcNow;
        var minutosRetraso = (int)(ahora - clase.InicioUtc).TotalMinutes;
        var estado = await CalcularEstadoAsistencia(ahora, clase.InicioUtc, clase.FinUtc);

        // Crear registro de asistencia
        var asistencia = new Asistencia
        {
            AlumnoId = alumnoId,
            ClaseId = claseId,
            MarcadaUtc = ahora,
            ClaseInicioUtc = clase.InicioUtc,
            ClaseFinUtc = clase.FinUtc,
            MinutosRetraso = Math.Max(0, minutosRetraso),
            Estado = estado,
            Metodo = metodo,
            EsRegistroManual = metodo == MetodoRegistro.Manual,
            JustificacionManual = justificacion,
            DocenteQueRegistroId = docenteId,
            CreadoUtc = ahora
        };

        _context.Asistencias.Add(asistencia);
        await _context.SaveChangesAsync();

        // Log de auditoría
        if (metodo == MetodoRegistro.Manual)
        {
            await CrearLogAuditoriaAsync(asistencia.Id, "CREADO_MANUAL", docenteId ?? GetCurrentUserId(), justificacion ?? "");
        }

        return asistencia;
    }

    public async Task<EstadoAsistencia> CalcularEstadoAsistencia(DateTime marcadaUtc, DateTime inicioClaseUtc, DateTime? finClaseUtc = null)
    {
        var minutosRetraso = (int)(marcadaUtc - inicioClaseUtc).TotalMinutes;
        
        if (minutosRetraso <= 20)
        {
            return EstadoAsistencia.Presente;  // 🟢 Llegó en los primeros 20 min
        }
        else if (finClaseUtc == null || marcadaUtc <= finClaseUtc)
        {
            return EstadoAsistencia.Tardanza;  // 🟡 Llegó después de 20min pero durante clase
        }
        else
        {
            return EstadoAsistencia.Ausente;   // 🔴 Llegó después de cerrar clase
        }
    }

    public async Task<bool> CerrarClaseYMarcarAusentesAsync(int claseId)
    {
        var clase = await _context.Clases.FindAsync(claseId);
        if (clase == null || clase.FinUtc != null)
            return false;

        // Cerrar la clase
        clase.FinUtc = DateTime.UtcNow;

        // Obtener estudiantes inscritos en el curso de esta clase
        var estudiantesSinRegistrar = await GetEstudiantesSinAsistenciaAsync(claseId);

        // Marcar como ausentes automáticamente
        foreach (var estudiante in estudiantesSinRegistrar)
        {
            var asistencia = new Asistencia
            {
                AlumnoId = estudiante.Id,
                ClaseId = claseId,
                MarcadaUtc = clase.FinUtc.Value,
                ClaseInicioUtc = clase.InicioUtc,
                ClaseFinUtc = clase.FinUtc,
                MinutosRetraso = (int)(clase.FinUtc.Value - clase.InicioUtc).TotalMinutes,
                Estado = EstadoAsistencia.Ausente,
                Metodo = MetodoRegistro.AutoAusente,
                EsRegistroManual = false,
                CreadoUtc = DateTime.UtcNow
            };

            _context.Asistencias.Add(asistencia);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<AsistenciaResumen>> GetAsistenciasResumenAsync(int claseId)
    {
        var asistencias = await _context.Asistencias
            .Include(a => a.Alumno)
            .Where(a => a.ClaseId == claseId)
            .OrderBy(a => a.MarcadaUtc)
            .Select(a => new AsistenciaResumen
            {
                AsistenciaId = a.Id,
                AlumnoId = a.AlumnoId,
                AlumnoCodigo = a.Alumno.Codigo,
                AlumnoNombre = a.Alumno.Nombre,
                Estado = a.Estado,
                MarcadaUtc = a.MarcadaUtc,
                MinutosRetraso = a.MinutosRetraso,
                Metodo = a.Metodo,
                EsRegistroManual = a.EsRegistroManual,
                JustificacionManual = a.JustificacionManual
            })
            .ToListAsync();

        return asistencias;
    }

    public async Task<bool> ModificarAsistenciaManualAsync(int asistenciaId, EstadoAsistencia nuevoEstado, string justificacion, int docenteId)
    {
        var asistencia = await _context.Asistencias.FindAsync(asistenciaId);
        if (asistencia == null)
            return false;

        var estadoAnterior = asistencia.Estado;
        var datosAnteriores = System.Text.Json.JsonSerializer.Serialize(new
        {
            Estado = estadoAnterior,
            Justificacion = asistencia.JustificacionManual,
            ModificadoUtc = asistencia.ModificadoUtc
        });

        asistencia.Estado = nuevoEstado;
        asistencia.JustificacionManual = justificacion;
        asistencia.EsRegistroManual = true;
        asistencia.DocenteQueRegistroId = docenteId;
        asistencia.ModificadoUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log de auditoría
        await CrearLogAuditoriaAsync(asistenciaId, "MODIFICADO_MANUAL", docenteId, justificacion, datosAnteriores);

        return true;
    }

    public async Task<ConfiguracionAsistencia> GetConfiguracionAsync(int claseId)
    {
        var config = await _context.ConfiguracionesAsistencia
            .FirstOrDefaultAsync(c => c.ClaseId == claseId);

        if (config == null)
        {
            // Crear configuración por defecto
            config = new ConfiguracionAsistencia
            {
                ClaseId = claseId,
                LimitePresenteMinutos = 20,
                PermiteRegistroManual = false,
                NotificarTardanzas = true,
                MarcarAusenteAutomatico = true
            };

            _context.ConfiguracionesAsistencia.Add(config);
            await _context.SaveChangesAsync();
        }

        return config;
    }

    public async Task<ConfiguracionAsistencia> CreateOrUpdateConfiguracionAsync(int claseId, ConfiguracionAsistencia config)
    {
        var existeConfig = await _context.ConfiguracionesAsistencia
            .FirstOrDefaultAsync(c => c.ClaseId == claseId);

        if (existeConfig != null)
        {
            existeConfig.LimitePresenteMinutos = config.LimitePresenteMinutos;
            existeConfig.PermiteRegistroManual = config.PermiteRegistroManual;
            existeConfig.NotificarTardanzas = config.NotificarTardanzas;
            existeConfig.MarcarAusenteAutomatico = config.MarcarAusenteAutomatico;
        }
        else
        {
            config.ClaseId = claseId;
            _context.ConfiguracionesAsistencia.Add(config);
        }

        await _context.SaveChangesAsync();
        return existeConfig ?? config;
    }

    // Métodos privados auxiliares
    private async Task<List<Alumno>> GetEstudiantesSinAsistenciaAsync(int claseId)
    {
        var clase = await _context.Clases
            .Include(c => c.Ramo)
            .ThenInclude(r => r.Curso)
            .FirstOrDefaultAsync(c => c.Id == claseId);

        if (clase?.Ramo?.Curso == null)
            return new List<Alumno>();

        var alumnosConAsistencia = await _context.Asistencias
            .Where(a => a.ClaseId == claseId)
            .Select(a => a.AlumnoId)
            .ToListAsync();

        var alumnosInscritos = await _context.AlumnoCursos
            .Where(ac => ac.CursoId == clase.Ramo.Curso.Id && ac.Activo)
            .Include(ac => ac.Alumno)
            .Select(ac => ac.Alumno)
            .Where(a => !alumnosConAsistencia.Contains(a.Id))
            .ToListAsync();

        return alumnosInscritos;
    }

    private async Task CrearLogAuditoriaAsync(int asistenciaId, string accion, int usuarioId, string justificacion, string datosAnteriores = "")
    {
        var log = new LogAuditoria
        {
            AsistenciaId = asistenciaId,
            Accion = accion,
            UsuarioId = usuarioId,
            Justificacion = justificacion,
            DatosAnteriores = datosAnteriores,
            TimestampUtc = DateTime.UtcNow
        };

        _context.LogsAuditoria.Add(log);
        await _context.SaveChangesAsync();
    }

    private int GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
        }
        return 1; // Fallback
    }
}

// DTO para resumen de asistencia
public class AsistenciaResumen
{
    public int AsistenciaId { get; set; }
    public int AlumnoId { get; set; }
    public string AlumnoCodigo { get; set; } = "";
    public string AlumnoNombre { get; set; } = "";
    public EstadoAsistencia Estado { get; set; }
    public DateTime MarcadaUtc { get; set; }
    public int MinutosRetraso { get; set; }
    public MetodoRegistro Metodo { get; set; }
    public bool EsRegistroManual { get; set; }
    public string? JustificacionManual { get; set; }
}