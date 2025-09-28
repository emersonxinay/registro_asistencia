using System.ComponentModel.DataAnnotations;

namespace registroAsistencia.Models;

public class Asistencia
{
    public int Id { get; set; }
    public int AlumnoId { get; set; }
    public int ClaseId { get; set; }
    
    // Timestamps detallados
    public DateTime MarcadaUtc { get; set; }
    public DateTime ClaseInicioUtc { get; set; }
    public DateTime? ClaseFinUtc { get; set; }
    public int MinutosRetraso { get; set; }
    
    // Estado calculado automáticamente
    public EstadoAsistencia Estado { get; set; }
    
    // Metadatos
    public MetodoRegistro Metodo { get; set; }
    public bool EsRegistroManual { get; set; } = false;
    public string? JustificacionManual { get; set; }
    public int? DocenteQueRegistroId { get; set; }
    
    // Auditoría
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ModificadoUtc { get; set; }
    
    // Navegación
    public virtual Alumno Alumno { get; set; } = null!;
    public virtual Clase Clase { get; set; } = null!;
    public virtual Usuario? DocenteQueRegistro { get; set; }
}

// Enums para estados y métodos
public enum EstadoAsistencia
{
    Presente = 1,      // 0-20 minutos
    Tardanza = 2,      // >20 minutos pero durante clase
    Ausente = 3,       // Nunca se registró o llegó después de cerrar
    Excusado = 4,      // Manual con justificación
    Pendiente = 5      // Clase en curso, aún puede llegar
}

public enum MetodoRegistro
{
    QrEstudiante = 1,     // Estudiante escaneó QR de clase
    QrDocente = 2,        // Docente escaneó QR de estudiante
    Manual = 3,           // Registro manual excepcional
    AutoAusente = 4       // Marcado automáticamente como ausente
}

// DTOs
public record ProfesorScanDto(int AlumnoId, int ClaseId);
public record AlumnoScanDto(int AlumnoId, int ClaseId, string Nonce);
public record RegistroAsistenciaDto(int AlumnoId, int ClaseId, MetodoRegistro Metodo, string? Justificacion = null);

// Configuración de asistencia
public class ConfiguracionAsistencia
{
    public int Id { get; set; }
    public int ClaseId { get; set; }
    public int LimitePresenteMinutos { get; set; } = 20;
    public bool PermiteRegistroManual { get; set; } = false;
    public bool NotificarTardanzas { get; set; } = true;
    public bool MarcarAusenteAutomatico { get; set; } = true;
    
    // Navegación
    public virtual Clase Clase { get; set; } = null!;
}

// Log de auditoría
public class LogAuditoria
{
    public int Id { get; set; }
    public int AsistenciaId { get; set; }
    public string Accion { get; set; } = ""; // "CREADO", "MODIFICADO", "MANUAL"
    public int UsuarioId { get; set; }
    public string Justificacion { get; set; } = "";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string DatosAnteriores { get; set; } = ""; // JSON del estado anterior
    
    // Navegación
    public virtual Asistencia Asistencia { get; set; } = null!;
    public virtual Usuario Usuario { get; set; } = null!;
}

public class QrClaseToken
{
    public int ClaseId { get; set; }
    public string Nonce { get; set; } = "";
    public DateTime ExpiraUtc { get; set; }
}