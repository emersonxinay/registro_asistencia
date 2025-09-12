using System.Collections.Concurrent;
using System.Threading;
using registroAsistencia.Models;

namespace registroAsistencia.Services;

public interface IDataService
{
    // Alumnos
    Task<Alumno> CreateAlumnoAsync(AlumnoCreateDto dto);
    Task<Alumno?> GetAlumnoAsync(int id);
    Task<IEnumerable<Alumno>> GetAlumnosAsync();
    Task<bool> UpdateAlumnoAsync(int id, AlumnoCreateDto dto);
    Task<bool> DeleteAlumnoAsync(int id);
    
    // Clases
    Task<Clase> CreateClaseAsync(ClaseCreateDto dto);
    Task<Clase?> GetClaseAsync(int id);
    Task<IEnumerable<Clase>> GetClasesAsync();
    Task<bool> CerrarClaseAsync(int id);
    Task<bool> ReabrirClaseAsync(int id);
    Task<Clase> DuplicarClaseAsync(int id);
    Task<bool> UpdateClaseAsync(int id, ClaseCreateDto dto);
    Task<bool> DeleteClaseAsync(int id);
    
    // Cursos
    Task<Curso> CreateCursoAsync(CursoCreateDto dto);
    Task<Curso?> GetCursoAsync(int id);
    Task<IEnumerable<Curso>> GetCursosAsync();
    Task<bool> UpdateCursoAsync(int id, CursoUpdateDto dto);
    Task<bool> DeleteCursoAsync(int id);
    
    // Ramos
    Task<Ramo> CreateRamoAsync(RamoCreateDto dto);
    Task<Ramo?> GetRamoAsync(int id);
    Task<IEnumerable<Ramo>> GetRamosAsync();
    Task<IEnumerable<Ramo>> GetRamosByCursoAsync(int cursoId);
    Task<bool> UpdateRamoAsync(int id, RamoUpdateDto dto);
    Task<bool> DeleteRamoAsync(int id);
    
    // Alumnos-Cursos
    Task<AlumnoCurso> InscribirAlumnoEnCursoAsync(AlumnoCursoCreateDto dto);
    Task<IEnumerable<AlumnoCursoDto>> GetAlumnosCursoAsync(int cursoId);
    Task<IEnumerable<AlumnoCursoDto>> GetCursosAlumnoAsync(int alumnoId);
    Task<bool> DesinscribirAlumnoDelCursoAsync(int alumnoId, int cursoId);
    Task<bool> AlumnoPerteneceCursoAsync(int alumnoId, int cursoId);
    Task<bool> ValidarLimiteInscripcionAsync(int alumnoId); // Máximo 2 cursos
    
    // Asistencias
    Task<bool> RegistrarAsistenciaAsync(int alumnoId, int claseId, string metodo);
    Task<IEnumerable<dynamic>> GetAsistenciasPorClaseAsync(int claseId);
    Task<IEnumerable<dynamic>> GetAsistenciasAsync();
    Task<bool> ExisteAsistenciaAsync(int alumnoId, int claseId);
    
    // Tokens QR
    Task<string> GenerarTokenClaseAsync(int claseId);
    Task<bool> ValidarTokenAsync(string nonce, int claseId);
    Task ConsumeTokenAsync(string nonce);
}

public class InMemoryDataService : IDataService
{
    private readonly ConcurrentDictionary<int, Alumno> _alumnos = new();
    private readonly ConcurrentDictionary<int, Clase> _clases = new();
    private readonly ConcurrentBag<Asistencia> _asistencias = new();
    private readonly ConcurrentDictionary<string, QrClaseToken> _tokens = new();
    private readonly ConcurrentDictionary<int, Curso> _cursos = new();
    private readonly ConcurrentDictionary<int, Ramo> _ramos = new();
    private readonly ConcurrentDictionary<int, AlumnoCurso> _alumnoCursos = new();
    private readonly ILogger<InMemoryDataService> _logger;
    private int _alumnoSeq = 0;
    private int _claseSeq = 0;
    private int _cursoSeq = 0;
    private int _ramoSeq = 0;
    private int _alumnoCursoSeq = 0;

    public InMemoryDataService(ILogger<InMemoryDataService> logger)
    {
        _logger = logger;
    }

    public Task<Alumno> CreateAlumnoAsync(AlumnoCreateDto dto)
    {
        var id = Interlocked.Increment(ref _alumnoSeq);
        var alumno = new Alumno
        {
            Id = id,
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            QrAlumnoBase64 = "" // Se generará en el servicio QR
        };
        _alumnos[id] = alumno;
        return Task.FromResult(alumno);
    }

    public Task<Alumno?> GetAlumnoAsync(int id)
    {
        _alumnos.TryGetValue(id, out var alumno);
        return Task.FromResult(alumno);
    }

    public Task<IEnumerable<Alumno>> GetAlumnosAsync()
    {
        return Task.FromResult(_alumnos.Values.AsEnumerable());
    }

    public Task<bool> UpdateAlumnoAsync(int id, AlumnoCreateDto dto)
    {
        if (!_alumnos.TryGetValue(id, out var alumno))
            return Task.FromResult(false);

        alumno.Codigo = dto.Codigo;
        alumno.Nombre = dto.Nombre;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAlumnoAsync(int id)
    {
        var removed = _alumnos.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    public Task<Clase> CreateClaseAsync(ClaseCreateDto dto)
    {
        var id = Interlocked.Increment(ref _claseSeq);
        var clase = new Clase
        {
            Id = id,
            Asignatura = dto.Asignatura,
            InicioUtc = DateTime.UtcNow
        };
        _clases[id] = clase;
        return Task.FromResult(clase);
    }

    public Task<Clase?> GetClaseAsync(int id)
    {
        _clases.TryGetValue(id, out var clase);
        return Task.FromResult(clase);
    }

    public Task<IEnumerable<Clase>> GetClasesAsync()
    {
        return Task.FromResult(_clases.Values.AsEnumerable());
    }

    public Task<bool> UpdateClaseAsync(int id, ClaseCreateDto dto)
    {
        if (!_clases.TryGetValue(id, out var clase))
            return Task.FromResult(false);

        clase.Asignatura = dto.Asignatura;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteClaseAsync(int id)
    {
        var removed = _clases.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    public Task<bool> CerrarClaseAsync(int id)
    {
        if (!_clases.TryGetValue(id, out var clase) || clase.FinUtc.HasValue)
            return Task.FromResult(false);
        
        clase.FinUtc = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    public Task<bool> ReabrirClaseAsync(int id)
    {
        if (!_clases.TryGetValue(id, out var clase) || !clase.FinUtc.HasValue)
            return Task.FromResult(false);
        
        clase.FinUtc = null;
        return Task.FromResult(true);
    }

    public Task<Clase> DuplicarClaseAsync(int id)
    {
        if (!_clases.TryGetValue(id, out var claseOriginal))
            throw new ArgumentException("Clase no encontrada");

        var nuevoId = Interlocked.Increment(ref _claseSeq);
        var claseNueva = new Clase
        {
            Id = nuevoId,
            Asignatura = claseOriginal.Asignatura + " (Copia)",
            InicioUtc = DateTime.UtcNow,
            FinUtc = null
        };

        _clases[claseNueva.Id] = claseNueva;
        return Task.FromResult(claseNueva);
    }

    public Task<bool> RegistrarAsistenciaAsync(int alumnoId, int claseId, string metodo)
    {
        var asistencia = new Asistencia
        {
            Id = _asistencias.Count + 1,
            AlumnoId = alumnoId,
            ClaseId = claseId,
            MarcadaUtc = DateTime.UtcNow,
            Metodo = metodo
        };
        _asistencias.Add(asistencia);
        return Task.FromResult(true);
    }

    public Task<IEnumerable<dynamic>> GetAsistenciasPorClaseAsync(int claseId)
    {
        var result = _asistencias
            .Where(a => a.ClaseId == claseId)
            .OrderBy(a => a.MarcadaUtc)
            .Select(a =>
            {
                _alumnos.TryGetValue(a.AlumnoId, out var alu);
                _clases.TryGetValue(a.ClaseId, out var cls);
                return new
                {
                    a.Id,
                    a.ClaseId,
                    Asignatura = cls?.Asignatura,
                    a.AlumnoId,
                    Codigo = alu?.Codigo,
                    Nombre = alu?.Nombre,
                    a.Metodo,
                    a.MarcadaUtc
                };
            });
        return Task.FromResult(result.Cast<dynamic>());
    }

    public Task<IEnumerable<dynamic>> GetAsistenciasAsync()
    {
        var result = _asistencias
            .OrderBy(a => a.ClaseId).ThenBy(a => a.MarcadaUtc)
            .Select(a =>
            {
                _alumnos.TryGetValue(a.AlumnoId, out var alu);
                _clases.TryGetValue(a.ClaseId, out var cls);
                return new
                {
                    a.Id,
                    a.ClaseId,
                    Asignatura = cls?.Asignatura,
                    a.AlumnoId,
                    Codigo = alu?.Codigo,
                    Nombre = alu?.Nombre,
                    a.Metodo,
                    a.MarcadaUtc
                };
            });
        return Task.FromResult(result.Cast<dynamic>());
    }

    public Task<bool> ExisteAsistenciaAsync(int alumnoId, int claseId)
    {
        var existe = _asistencias.Any(a => a.AlumnoId == alumnoId && a.ClaseId == claseId);
        return Task.FromResult(existe);
    }

    public Task<string> GenerarTokenClaseAsync(int claseId)
    {
        var nonce = Guid.NewGuid().ToString("N");
        var token = new QrClaseToken
        {
            ClaseId = claseId,
            Nonce = nonce,
            ExpiraUtc = DateTime.UtcNow.AddSeconds(300) // 5 minutos
        };
        _tokens[nonce] = token;
        
        _logger.LogInformation("🎯 Token generado - ClaseId: {ClaseId}, Nonce: {Nonce}, Expira: {ExpiraUtc}", 
            claseId, nonce, token.ExpiraUtc);
        
        return Task.FromResult(nonce);
    }

    public Task<bool> ValidarTokenAsync(string nonce, int claseId)
    {
        _logger.LogInformation("🔍 Validando token - Nonce: {Nonce}, ClaseId esperada: {ClaseId}", nonce, claseId);
        
        if (!_tokens.TryGetValue(nonce, out var token))
        {
            _logger.LogWarning("❌ Token no encontrado - Nonce: {Nonce}", nonce);
            return Task.FromResult(false);
        }
        
        var now = DateTime.UtcNow;
        var isValidClass = token.ClaseId == claseId;
        var isNotExpired = now <= token.ExpiraUtc;
        var isValid = isValidClass && isNotExpired;
        
        _logger.LogInformation("📋 Token info - ClaseId: {TokenClase} vs {ClaseEsperada}, Expira: {Expira}, Ahora: {Ahora}, Válido: {Valido}", 
            token.ClaseId, claseId, token.ExpiraUtc, now, isValid);
            
        if (!isValidClass)
        {
            _logger.LogWarning("❌ Token de clase incorrecta - Token ClaseId: {TokenClase}, Esperado: {ClaseEsperada}", 
                token.ClaseId, claseId);
        }
        
        if (!isNotExpired)
        {
            _logger.LogWarning("❌ Token expirado - Expiraba: {ExpiraUtc}, Ahora: {Ahora}", token.ExpiraUtc, now);
        }
        
        return Task.FromResult(isValid);
    }

    public Task ConsumeTokenAsync(string nonce)
    {
        var removed = _tokens.TryRemove(nonce, out var token);
        _logger.LogInformation("🗑️ Token consumido - Nonce: {Nonce}, Removido: {Removido}", nonce, removed);
        return Task.CompletedTask;
    }

    // Cursos (stub implementations for interface compliance)
    public Task<Curso> CreateCursoAsync(CursoCreateDto dto)
    {
        throw new NotImplementedException("Use EfDataService for Curso operations");
    }

    public Task<Curso?> GetCursoAsync(int id)
    {
        throw new NotImplementedException("Use EfDataService for Curso operations");
    }

    public Task<IEnumerable<Curso>> GetCursosAsync()
    {
        throw new NotImplementedException("Use EfDataService for Curso operations");
    }

    public Task<bool> UpdateCursoAsync(int id, CursoUpdateDto dto)
    {
        throw new NotImplementedException("Use EfDataService for Curso operations");
    }

    public Task<bool> DeleteCursoAsync(int id)
    {
        throw new NotImplementedException("Use EfDataService for Curso operations");
    }

    // Ramos (stub implementations)
    public Task<Ramo> CreateRamoAsync(RamoCreateDto dto)
    {
        throw new NotImplementedException("Use EfDataService for Ramo operations");
    }

    public Task<Ramo?> GetRamoAsync(int id)
    {
        throw new NotImplementedException("Use EfDataService for Ramo operations");
    }

    public Task<IEnumerable<Ramo>> GetRamosAsync()
    {
        throw new NotImplementedException("Use EfDataService for Ramo operations");
    }

    public Task<IEnumerable<Ramo>> GetRamosByCursoAsync(int cursoId)
    {
        throw new NotImplementedException("Use EfDataService for Ramo operations");
    }

    public Task<bool> UpdateRamoAsync(int id, RamoUpdateDto dto)
    {
        throw new NotImplementedException("Use EfDataService for Ramo operations");
    }

    public Task<bool> DeleteRamoAsync(int id)
    {
        throw new NotImplementedException("Use EfDataService for Ramo operations");
    }

    // Alumnos-Cursos (stub implementations)
    public Task<AlumnoCurso> InscribirAlumnoEnCursoAsync(AlumnoCursoCreateDto dto)
    {
        throw new NotImplementedException("Use EfDataService for AlumnoCurso operations");
    }

    public Task<IEnumerable<AlumnoCursoDto>> GetAlumnosCursoAsync(int cursoId)
    {
        throw new NotImplementedException("Use EfDataService for AlumnoCurso operations");
    }

    public Task<IEnumerable<AlumnoCursoDto>> GetCursosAlumnoAsync(int alumnoId)
    {
        throw new NotImplementedException("Use EfDataService for AlumnoCurso operations");
    }

    public Task<bool> DesinscribirAlumnoDelCursoAsync(int alumnoId, int cursoId)
    {
        throw new NotImplementedException("Use EfDataService for AlumnoCurso operations");
    }

    public Task<bool> AlumnoPerteneceCursoAsync(int alumnoId, int cursoId)
    {
        throw new NotImplementedException("Use EfDataService for AlumnoCurso operations");
    }

    public Task<bool> ValidarLimiteInscripcionAsync(int alumnoId)
    {
        throw new NotImplementedException("Use EfDataService for AlumnoCurso operations");
    }
}