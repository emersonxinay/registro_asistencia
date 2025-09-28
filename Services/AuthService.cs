using Microsoft.EntityFrameworkCore;
using registroAsistencia.Data;
using registroAsistencia.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace registroAsistencia.Services;

public interface IAuthService
{
    Task<Usuario?> LoginAsync(string email, string password);
    Task<Usuario> RegisterAsync(string nombre, string email, string codigoDocente, string password, string? departamento = null, bool esAdministrador = false);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> CodigoDocenteExistsAsync(string codigoDocente);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    ClaimsPrincipal CreateClaimsPrincipal(Usuario usuario);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> LoginAsync(string email, string password)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo);
            
        if (usuario == null || !VerifyPassword(password, usuario.PasswordHash))
        {
            return null;
        }

        // Actualizar último acceso
        usuario.UltimoAccesoUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return usuario;
    }

    public async Task<Usuario> RegisterAsync(string nombre, string email, string codigoDocente, string password, string? departamento = null, bool esAdministrador = false)
    {
        if (await EmailExistsAsync(email))
        {
            throw new InvalidOperationException("El email ya está registrado");
        }

        if (await CodigoDocenteExistsAsync(codigoDocente))
        {
            throw new InvalidOperationException("El código de docente ya está registrado");
        }

        var usuario = new Usuario
        {
            Nombre = nombre,
            Email = email,
            CodigoDocente = codigoDocente,
            PasswordHash = HashPassword(password),
            Departamento = departamento,
            EsAdministrador = esAdministrador,
            Activo = true,
            CreadoUtc = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return usuario;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Usuarios.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> CodigoDocenteExistsAsync(string codigoDocente)
    {
        return await _context.Usuarios.AnyAsync(u => u.CodigoDocente == codigoDocente);
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "QuantumAttend_Salt_2025"));
        return Convert.ToBase64String(hashedBytes);
    }

    public bool VerifyPassword(string password, string hash)
    {
        var hashedPassword = HashPassword(password);
        return hashedPassword == hash;
    }

    public ClaimsPrincipal CreateClaimsPrincipal(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim("CodigoDocente", usuario.CodigoDocente),
            new Claim("Departamento", usuario.Departamento ?? ""),
            new Claim(ClaimTypes.Role, usuario.EsAdministrador ? "Administrador" : "Docente")
        };

        var identity = new ClaimsIdentity(claims, "QuantumAttend");
        return new ClaimsPrincipal(identity);
    }
}