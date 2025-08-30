namespace registroAsistencia.Models;

public class Clase
{
    public int Id { get; set; }
    public string Asignatura { get; set; } = "";
    public DateTime InicioUtc { get; set; }
    public DateTime? FinUtc { get; set; }
    public bool Activa => FinUtc is null;
}

public record ClaseCreateDto(string Asignatura);