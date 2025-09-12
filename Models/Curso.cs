namespace registroAsistencia.Models;

public class Curso
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    
    // Navegación
    public virtual ICollection<Ramo> Ramos { get; set; } = new List<Ramo>();
    public virtual ICollection<AlumnoCurso> AlumnoCursos { get; set; } = new List<AlumnoCurso>();
}

public class CursoCreateDto
{
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Descripcion { get; set; }
}

public class CursoUpdateDto
{
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}