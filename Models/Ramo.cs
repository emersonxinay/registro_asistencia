namespace registroAsistencia.Models;

public class Ramo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    
    // Relación con Curso
    public int CursoId { get; set; }
    public virtual Curso Curso { get; set; } = null!;
    
    // Navegación
    public virtual ICollection<Clase> Clases { get; set; } = new List<Clase>();
}

public class RamoCreateDto
{
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public int CursoId { get; set; }
    public string? Descripcion { get; set; }
}

public class RamoUpdateDto
{
    public string Nombre { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}