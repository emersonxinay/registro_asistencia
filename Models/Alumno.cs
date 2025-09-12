namespace registroAsistencia.Models;

public class Alumno
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string QrAlumnoBase64 { get; set; } = "";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    
    // Navegación hacia cursos
    public virtual ICollection<AlumnoCurso> AlumnoCursos { get; set; } = new List<AlumnoCurso>();
}

public record AlumnoCreateDto(string Codigo, string Nombre);