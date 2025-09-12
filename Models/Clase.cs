namespace registroAsistencia.Models;

public class Clase
{
    public int Id { get; set; }
    
    // Nueva estructura con Ramo
    public int? RamoId { get; set; }
    public virtual Ramo? Ramo { get; set; }
    
    // Compatibilidad hacia atrás (para clases existentes)
    public string Asignatura { get; set; } = "";
    
    public DateTime InicioUtc { get; set; }
    public DateTime? FinUtc { get; set; }
    public string? Descripcion { get; set; }
    
    public bool Activa => FinUtc is null;
    
    // Propiedad computada para mostrar el nombre del ramo o asignatura
    public string NombreCompleto => Ramo != null ? $"{Ramo.Nombre} ({Ramo.Curso.Nombre})" : Asignatura;
}

// DTOs actualizados
public record ClaseCreateDto(string Asignatura, int? RamoId = null, string? Descripcion = null);
public record ClaseCreateWithRamoDto(int RamoId, string? Descripcion = null);