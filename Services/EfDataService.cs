using Microsoft.EntityFrameworkCore;
using registroAsistencia.Data;
using registroAsistencia.Models;

namespace registroAsistencia.Services;

public class EfDataService : IDataService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EfDataService> _logger;
    private readonly IQrService _qrService;

    public EfDataService(ApplicationDbContext context, ILogger<EfDataService> logger, IQrService qrService)
    {
        _context = context;
        _logger = logger;
        _qrService = qrService;
    }

    public async Task<Alumno> CreateAlumnoAsync(AlumnoCreateDto dto)
    {
        var alumno = new Alumno
        {
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            QrAlumnoBase64 = ""
        };

        _context.Alumnos.Add(alumno);
        await _context.SaveChangesAsync();
        
        // Generar QR después de guardar para tener el ID
        alumno.QrAlumnoBase64 = _qrService.GenerateBase64Qr($"alumno:{alumno.Id}");
        await _context.SaveChangesAsync();
        
        return alumno;
    }

    public async Task<Alumno?> GetAlumnoAsync(int id)
    {
        return await _context.Alumnos.FindAsync(id);
    }

    public async Task<IEnumerable<Alumno>> GetAlumnosAsync()
    {
        return await _context.Alumnos.ToListAsync();
    }

    public async Task<bool> UpdateAlumnoAsync(int id, AlumnoCreateDto dto)
    {
        var alumno = await _context.Alumnos.FindAsync(id);
        if (alumno == null)
            return false;

        alumno.Codigo = dto.Codigo;
        alumno.Nombre = dto.Nombre;
        // Regenerar QR con los nuevos datos
        alumno.QrAlumnoBase64 = _qrService.GenerateBase64Qr($"alumno:{alumno.Id}");

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAlumnoAsync(int id)
    {
        var alumno = await _context.Alumnos.FindAsync(id);
        if (alumno == null)
            return false;

        _context.Alumnos.Remove(alumno);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Clase> CreateClaseAsync(ClaseCreateDto dto)
    {
        var clase = new Clase
        {
            Asignatura = dto.Asignatura,
            InicioUtc = DateTime.UtcNow
        };

        _context.Clases.Add(clase);
        await _context.SaveChangesAsync();
        return clase;
    }

    public async Task<Clase?> GetClaseAsync(int id)
    {
        return await _context.Clases.FindAsync(id);
    }

    public async Task<IEnumerable<Clase>> GetClasesAsync()
    {
        return await _context.Clases.ToListAsync();
    }

    public async Task<bool> UpdateClaseAsync(int id, ClaseCreateDto dto)
    {
        var clase = await _context.Clases.FindAsync(id);
        if (clase == null)
            return false;

        clase.Asignatura = dto.Asignatura;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteClaseAsync(int id)
    {
        var clase = await _context.Clases.FindAsync(id);
        if (clase == null)
            return false;

        _context.Clases.Remove(clase);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CerrarClaseAsync(int id)
    {
        var clase = await _context.Clases.FindAsync(id);
        if (clase == null || clase.FinUtc.HasValue)
            return false;

        clase.FinUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RegistrarAsistenciaAsync(int alumnoId, int claseId, string metodo)
    {
        var existeAsistencia = await ExisteAsistenciaAsync(alumnoId, claseId);
        if (existeAsistencia)
            return false;

        var asistencia = new Asistencia
        {
            AlumnoId = alumnoId,
            ClaseId = claseId,
            MarcadaUtc = DateTime.UtcNow,
            Metodo = metodo
        };

        _context.Asistencias.Add(asistencia);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<dynamic>> GetAsistenciasPorClaseAsync(int claseId)
    {
        var result = await _context.Asistencias
            .Where(a => a.ClaseId == claseId)
            .OrderBy(a => a.MarcadaUtc)
            .Select(a => new
            {
                a.Id,
                a.ClaseId,
                Asignatura = _context.Clases.Where(c => c.Id == a.ClaseId).Select(c => c.Asignatura).FirstOrDefault(),
                a.AlumnoId,
                Codigo = _context.Alumnos.Where(al => al.Id == a.AlumnoId).Select(al => al.Codigo).FirstOrDefault(),
                Nombre = _context.Alumnos.Where(al => al.Id == a.AlumnoId).Select(al => al.Nombre).FirstOrDefault(),
                a.Metodo,
                a.MarcadaUtc
            })
            .ToListAsync();

        return result.Cast<dynamic>();
    }

    public async Task<IEnumerable<dynamic>> GetAsistenciasAsync()
    {
        var result = await _context.Asistencias
            .OrderBy(a => a.ClaseId).ThenBy(a => a.MarcadaUtc)
            .Select(a => new
            {
                a.Id,
                a.ClaseId,
                Asignatura = _context.Clases.Where(c => c.Id == a.ClaseId).Select(c => c.Asignatura).FirstOrDefault(),
                a.AlumnoId,
                Codigo = _context.Alumnos.Where(al => al.Id == a.AlumnoId).Select(al => al.Codigo).FirstOrDefault(),
                Nombre = _context.Alumnos.Where(al => al.Id == a.AlumnoId).Select(al => al.Nombre).FirstOrDefault(),
                a.Metodo,
                a.MarcadaUtc
            })
            .ToListAsync();

        return result.Cast<dynamic>();
    }

    public async Task<bool> ExisteAsistenciaAsync(int alumnoId, int claseId)
    {
        return await _context.Asistencias.AnyAsync(a => a.AlumnoId == alumnoId && a.ClaseId == claseId);
    }

    public async Task<string> GenerarTokenClaseAsync(int claseId)
    {
        var nonce = Guid.NewGuid().ToString("N");
        var token = new QrClaseToken
        {
            ClaseId = claseId,
            Nonce = nonce,
            ExpiraUtc = DateTime.UtcNow.AddSeconds(300)
        };

        _context.QrClaseTokens.Add(token);
        await _context.SaveChangesAsync();

        _logger.LogInformation("🎯 Token generado - ClaseId: {ClaseId}, Nonce: {Nonce}, Expira: {ExpiraUtc}",
            claseId, nonce, token.ExpiraUtc);

        return nonce;
    }

    public async Task<bool> ValidarTokenAsync(string nonce, int claseId)
    {
        _logger.LogInformation("🔍 Validando token - Nonce: {Nonce}, ClaseId esperada: {ClaseId}", nonce, claseId);

        var token = await _context.QrClaseTokens.FindAsync(nonce);
        if (token == null)
        {
            _logger.LogWarning("❌ Token no encontrado - Nonce: {Nonce}", nonce);
            return false;
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

        return isValid;
    }

    public async Task ConsumeTokenAsync(string nonce)
    {
        var token = await _context.QrClaseTokens.FindAsync(nonce);
        if (token != null)
        {
            _context.QrClaseTokens.Remove(token);
            await _context.SaveChangesAsync();
            _logger.LogInformation("🗑️ Token consumido - Nonce: {Nonce}", nonce);
        }
    }
}