using System.ComponentModel.DataAnnotations;

namespace registroAsistencia.Models;

// Relación Docente-Curso (qué cursos maneja cada docente)
public class DocenteCurso
{
    public int Id { get; set; }
    public int DocenteId { get; set; }
    public int CursoId { get; set; }
    public bool EsPropietario { get; set; } = false; // true = creado por él, false = asignado por admin
    public DateTime AsignadoUtc { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;
    
    // Navegación
    public virtual Usuario Docente { get; set; } = null!;
    public virtual Curso Curso { get; set; } = null!;
}

// Relación Docente-Ramo (qué ramos enseña cada docente)
public class DocenteRamo
{
    public int Id { get; set; }
    public int DocenteId { get; set; }
    public int RamoId { get; set; }
    public DateTime AsignadoUtc { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;
    
    // Navegación
    public virtual Usuario Docente { get; set; } = null!;
    public virtual Ramo Ramo { get; set; } = null!;
}

// QR permanente del estudiante
public class QrEstudiante
{
    public int Id { get; set; }
    public int AlumnoId { get; set; }
    public string QrData { get; set; } = ""; // código único encriptado
    public DateTime GeneradoUtc { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;
    
    // Navegación
    public virtual Alumno Alumno { get; set; } = null!;
}

// Configuración de horarios
public class HorarioClase
{
    public int Id { get; set; }
    public int RamoId { get; set; }
    public DayOfWeek DiaSemana { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public string? Aula { get; set; }
    public bool Activo { get; set; } = true;
    
    // Navegación
    public virtual Ramo Ramo { get; set; } = null!;
}

// DTOs para la API
public record DocenteCursoCreateDto(int CursoId);
public record DocenteRamoCreateDto(int RamoId);
public record HorarioClaseCreateDto(int RamoId, DayOfWeek DiaSemana, TimeSpan HoraInicio, TimeSpan HoraFin, string? Aula = null);