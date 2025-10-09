using System.Text;

namespace registroAsistencia.Services;

public interface ICsvService
{
    byte[] GenerateAsistenciasCsv(IEnumerable<dynamic> asistencias);
    byte[] GenerateAsistenciasCompletoCsv(Models.Clase clase, IEnumerable<Models.AlumnoCursoDto> estudiantes, IEnumerable<dynamic> asistencias);
    string EscapeCsvField(string? field);
    Task<List<Models.AlumnoCreateDto>> ParseAlumnosCsvAsync(Stream fileStream);
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

    public byte[] GenerateAsistenciasCompletoCsv(Models.Clase clase, IEnumerable<Models.AlumnoCursoDto> estudiantes, IEnumerable<dynamic> asistencias)
    {
        var header = "alumnoId,codigo,nombre,estado,fechaRegistro,horaRegistro,minutosRetraso,metodo,claseId,asignatura,fechaClase,horaClase";
        var estudiantesList = estudiantes.ToList();
        var asistenciasList = asistencias.ToList();

        if (!estudiantesList.Any())
        {
            var emptyCsv = header + "\n# No hay estudiantes inscritos en este curso";
            return ToCsvBytes(emptyCsv);
        }

        // Obtener información adicional de cada alumno para el CSV
        var rows = new List<string>();

        foreach (var estudiante in estudiantesList)
        {
            // Buscar si tiene asistencia registrada
            var asistencia = asistenciasList.FirstOrDefault(a =>
                (a.AlumnoId ?? a.alumnoId) == estudiante.AlumnoId);

            string estado;
            string fechaRegistro = "";
            string horaRegistro = "";
            string minutosRetraso = "0";
            string metodo = "";

            if (asistencia != null)
            {
                // Convertir estado enum a texto
                var estadoNum = asistencia.Estado ?? asistencia.estado;
                estado = ConvertirEstadoATexto(estadoNum);

                // Obtener datos de registro
                var marcadaUtc = asistencia.MarcadaUtc ?? asistencia.marcadaUtc;
                if (marcadaUtc != null)
                {
                    var fechaHora = DateTime.Parse(marcadaUtc.ToString());
                    fechaRegistro = fechaHora.ToString("yyyy-MM-dd");
                    horaRegistro = fechaHora.ToString("HH:mm:ss");
                }

                minutosRetraso = (asistencia.MinutosRetraso ?? asistencia.minutosRetraso ?? 0).ToString();
                metodo = ConvertirMetodoATexto(asistencia.Metodo ?? asistencia.metodo);
            }
            else
            {
                estado = "AUSENTE";
            }

            // Información de la clase
            var fechaClase = clase.InicioUtc.ToString("yyyy-MM-dd");
            var horaClase = clase.InicioUtc.ToString("HH:mm:ss");

            var row = string.Join(",", new[]
            {
                estudiante.AlumnoId.ToString(),
                EscapeCsvField($"ALU-{estudiante.AlumnoId}"), // Código temporal
                EscapeCsvField(estudiante.AlumnoNombre),
                EscapeCsvField(estado),
                EscapeCsvField(fechaRegistro),
                EscapeCsvField(horaRegistro),
                EscapeCsvField(minutosRetraso),
                EscapeCsvField(metodo),
                clase.Id.ToString(),
                EscapeCsvField(clase.Asignatura ?? clase.NombreCompleto),
                EscapeCsvField(fechaClase),
                EscapeCsvField(horaClase)
            });

            rows.Add(row);
        }

        var csv = header + "\n" + string.Join("\n", rows);
        return ToCsvBytes(csv);
    }

    private string ConvertirEstadoATexto(object? estado)
    {
        if (estado == null) return "DESCONOCIDO";

        if (int.TryParse(estado.ToString(), out int estadoNum))
        {
            return estadoNum switch
            {
                1 => "PRESENTE",
                2 => "TARDANZA",
                3 => "AUSENTE",
                4 => "EXCUSADO",
                5 => "PENDIENTE",
                _ => "DESCONOCIDO"
            };
        }

        return estado.ToString() ?? "DESCONOCIDO";
    }

    private string ConvertirMetodoATexto(object? metodo)
    {
        if (metodo == null) return "";

        if (int.TryParse(metodo.ToString(), out int metodoNum))
        {
            return metodoNum switch
            {
                1 => "QR Estudiante",
                2 => "QR Docente",
                3 => "Manual",
                4 => "Auto Ausente",
                _ => "Desconocido"
            };
        }

        return metodo.ToString() ?? "";
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

    public async Task<List<Models.AlumnoCreateDto>> ParseAlumnosCsvAsync(Stream fileStream)
    {
        var alumnos = new List<Models.AlumnoCreateDto>();

        using var reader = new StreamReader(fileStream, Encoding.UTF8);

        // Leer y validar header
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new Exception("El archivo CSV está vacío");
        }

        // Header esperado: nombre o nombre,codigo (el código es opcional)
        var headers = headerLine.Split(',').Select(h => h.Trim().ToLower()).ToArray();
        var nombreIndex = Array.IndexOf(headers, "nombre");
        var codigoIndex = Array.IndexOf(headers, "codigo");

        if (nombreIndex == -1)
        {
            throw new Exception("El archivo CSV debe contener una columna 'nombre'");
        }

        // Leer líneas de datos
        int lineNumber = 1;
        while (!reader.EndOfStream)
        {
            lineNumber++;
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var values = ParseCsvLine(line);

            if (values.Length <= nombreIndex)
            {
                throw new Exception($"Línea {lineNumber}: Formato inválido, faltan columnas");
            }

            var nombre = values[nombreIndex].Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new Exception($"Línea {lineNumber}: El nombre no puede estar vacío");
            }

            var alumno = new Models.AlumnoCreateDto(null, nombre);

            // El código se ignora si existe, se generará automáticamente
            alumnos.Add(alumno);
        }

        if (alumnos.Count == 0)
        {
            throw new Exception("No se encontraron alumnos válidos en el archivo");
        }

        return alumnos;
    }

    private string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        values.Add(currentValue.ToString());
        return values.ToArray();
    }
}