namespace registroAsistencia.Models;

public class Asistencia
{
    public int Id { get; set; }
    public int AlumnoId { get; set; }
    public int ClaseId { get; set; }
    public DateTime MarcadaUtc { get; set; }
    public string Metodo { get; set; } = "";
}

public record ProfesorScanDto(int AlumnoId, int ClaseId);
public record AlumnoScanDto(int AlumnoId, int ClaseId, string Nonce);

public class QrClaseToken
{
    public int ClaseId { get; set; }
    public string Nonce { get; set; } = "";
    public DateTime ExpiraUtc { get; set; }
}