namespace registroAsistencia.Models;

public class AlumnoCurso
{
    public int Id { get; set; }
    public int AlumnoId { get; set; }
    public int CursoId { get; set; }
    public DateTime FechaInscripcion { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;
    
    // Navegación
    public virtual Alumno Alumno { get; set; } = null!;
    public virtual Curso Curso { get; set; } = null!;
}

public record AlumnoCursoCreateDto(int AlumnoId, int CursoId);
public record AlumnoCursoDto(int Id, int AlumnoId, int CursoId, string AlumnoNombre, string CursoNombre, DateTime FechaInscripcion, bool Activo);