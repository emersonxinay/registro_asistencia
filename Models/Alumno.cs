namespace registroAsistencia.Models;

public class Alumno
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string QrAlumnoBase64 { get; set; } = "";
}

public record AlumnoCreateDto(string Codigo, string Nombre);