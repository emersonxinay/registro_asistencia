using System.Text;

namespace registroAsistencia.Services;

public interface ICsvService
{
    byte[] GenerateAsistenciasCsv(IEnumerable<dynamic> asistencias);
    string EscapeCsvField(string? field);
}

public class CsvService : ICsvService
{
    public byte[] GenerateAsistenciasCsv(IEnumerable<dynamic> asistencias)
    {
        var header = "id,claseId,asignatura,alumnoId,codigo,nombre,metodo,marcadaUtc";
        var asistenciasList = asistencias.ToList();
        
        if (!asistenciasList.Any())
        {
            var emptyCsv = header + "\n# No hay asistencias registradas";
            return ToCsvBytes(emptyCsv);
        }
        
        var rows = asistenciasList.Select(a => string.Join(",", new[]
        {
            a.Id?.ToString() ?? "",
            a.ClaseId?.ToString() ?? "",
            EscapeCsvField(a.Asignatura?.ToString()),
            a.AlumnoId?.ToString() ?? "",
            EscapeCsvField(a.Codigo?.ToString()),
            EscapeCsvField(a.Nombre?.ToString()),
            EscapeCsvField(a.Metodo?.ToString()),
            a.MarcadaUtc?.ToString("o") ?? ""
        }));
        
        var csv = header + "\n" + string.Join("\n", rows);
        return ToCsvBytes(csv);
    }

    public string EscapeCsvField(string? field)
    {
        field ??= "";
        if (field.Contains('"') || field.Contains(',') || field.Contains('\n') || field.Contains('\r'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }

    private static byte[] ToCsvBytes(string csv)
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var data = Encoding.UTF8.GetBytes(csv);
        return bom.Concat(data).ToArray();
    }
}