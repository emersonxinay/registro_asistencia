using Microsoft.EntityFrameworkCore;
using registroAsistencia.Models;

namespace registroAsistencia.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    public DbSet<Alumno> Alumnos { get; set; }
    public DbSet<Clase> Clases { get; set; }
    public DbSet<Asistencia> Asistencias { get; set; }
    public DbSet<QrClaseToken> QrClaseTokens { get; set; }
    
    // Nuevas entidades
    public DbSet<Curso> Cursos { get; set; }
    public DbSet<Ramo> Ramos { get; set; }
    public DbSet<AlumnoCurso> AlumnoCursos { get; set; }
    
    // Usuarios/Docentes
    public DbSet<Usuario> Usuarios { get; set; }
    
    // Nuevas entidades para el sistema mejorado
    public DbSet<DocenteCurso> DocenteCursos { get; set; }
    public DbSet<DocenteRamo> DocenteRamos { get; set; }
    public DbSet<QrEstudiante> QrEstudiantes { get; set; }
    public DbSet<HorarioClase> HorarioClases { get; set; }
    public DbSet<ConfiguracionAsistencia> ConfiguracionesAsistencia { get; set; }
    public DbSet<LogAuditoria> LogsAuditoria { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alumno>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.QrAlumnoBase64).HasColumnType("text");
            entity.HasIndex(e => e.Codigo).IsUnique();
        });

        modelBuilder.Entity<Clase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Asignatura).HasMaxLength(200);
            entity.Property(e => e.InicioUtc).IsRequired();
            entity.Property(e => e.FinUtc);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.DocenteId).IsRequired();
            
            // Relación opcional con Ramo
            entity.HasOne(e => e.Ramo)
                .WithMany(r => r.Clases)
                .HasForeignKey(e => e.RamoId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Relación con Docente
            entity.HasOne(e => e.Docente)
                .WithMany(u => u.Clases)
                .HasForeignKey(e => e.DocenteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Asistencia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AlumnoId).IsRequired();
            entity.Property(e => e.ClaseId).IsRequired();
            entity.Property(e => e.MarcadaUtc).IsRequired();
            entity.Property(e => e.ClaseInicioUtc).IsRequired();
            entity.Property(e => e.MinutosRetraso).IsRequired();
            entity.Property(e => e.Estado).IsRequired();
            entity.Property(e => e.Metodo).IsRequired();
            entity.Property(e => e.EsRegistroManual).IsRequired();
            entity.Property(e => e.JustificacionManual).HasMaxLength(500);
            entity.Property(e => e.CreadoUtc).IsRequired();
            
            entity.HasIndex(e => new { e.AlumnoId, e.ClaseId }).IsUnique();
            
            entity.HasOne(e => e.Alumno)
                .WithMany()
                .HasForeignKey(e => e.AlumnoId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Clase)
                .WithMany(c => c.Asistencias)
                .HasForeignKey(e => e.ClaseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.DocenteQueRegistro)
                .WithMany()
                .HasForeignKey(e => e.DocenteQueRegistroId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QrClaseToken>(entity =>
        {
            entity.HasKey(e => e.Nonce);
            entity.Property(e => e.Nonce).HasMaxLength(32);
            entity.Property(e => e.ClaseId).IsRequired();
            entity.Property(e => e.ExpiraUtc).IsRequired();
            
            entity.HasOne<Clase>()
                .WithMany()
                .HasForeignKey(e => e.ClaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de Curso
        modelBuilder.Entity<Curso>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaCreacion).IsRequired();
            entity.Property(e => e.Activo).IsRequired();
            
            entity.HasIndex(e => e.Codigo).IsUnique();
        });

        // Configuración de Ramo
        modelBuilder.Entity<Ramo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaCreacion).IsRequired();
            entity.Property(e => e.Activo).IsRequired();
            
            // Relación con Curso
            entity.HasOne(e => e.Curso)
                .WithMany(c => c.Ramos)
                .HasForeignKey(e => e.CursoId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.CursoId, e.Codigo }).IsUnique();
        });

        // Configuración de AlumnoCurso
        modelBuilder.Entity<AlumnoCurso>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FechaInscripcion).IsRequired();
            entity.Property(e => e.Activo).IsRequired();
            
            // Relaciones
            entity.HasOne(e => e.Alumno)
                .WithMany(a => a.AlumnoCursos)
                .HasForeignKey(e => e.AlumnoId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Curso)
                .WithMany(c => c.AlumnoCursos)
                .HasForeignKey(e => e.CursoId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Un alumno no puede estar inscrito dos veces en el mismo curso
            entity.HasIndex(e => new { e.AlumnoId, e.CursoId }).IsUnique();
        });

        // Configuración de Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CodigoDocente).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Departamento).HasMaxLength(255);
            entity.Property(e => e.Activo).IsRequired();
            entity.Property(e => e.CreadoUtc).IsRequired();
            entity.Property(e => e.EsAdministrador).IsRequired();
            
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.CodigoDocente).IsUnique();
        });

        // Configuración de DocenteCurso
        modelBuilder.Entity<DocenteCurso>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EsPropietario).IsRequired();
            entity.Property(e => e.AsignadoUtc).IsRequired();
            entity.Property(e => e.Activo).IsRequired();
            
            entity.HasOne(e => e.Docente)
                .WithMany()
                .HasForeignKey(e => e.DocenteId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Curso)
                .WithMany()
                .HasForeignKey(e => e.CursoId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.DocenteId, e.CursoId }).IsUnique();
        });

        // Configuración de DocenteRamo
        modelBuilder.Entity<DocenteRamo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AsignadoUtc).IsRequired();
            entity.Property(e => e.Activo).IsRequired();
            
            entity.HasOne(e => e.Docente)
                .WithMany()
                .HasForeignKey(e => e.DocenteId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Ramo)
                .WithMany()
                .HasForeignKey(e => e.RamoId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => new { e.DocenteId, e.RamoId }).IsUnique();
        });

        // Configuración de QrEstudiante
        modelBuilder.Entity<QrEstudiante>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QrData).IsRequired().HasMaxLength(500);
            entity.Property(e => e.GeneradoUtc).IsRequired();
            entity.Property(e => e.Activo).IsRequired();
            
            entity.HasOne(e => e.Alumno)
                .WithMany()
                .HasForeignKey(e => e.AlumnoId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.QrData).IsUnique();
            entity.HasIndex(e => e.AlumnoId);
        });

        // Configuración de HorarioClase
        modelBuilder.Entity<HorarioClase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DiaSemana).IsRequired();
            entity.Property(e => e.HoraInicio).IsRequired();
            entity.Property(e => e.HoraFin).IsRequired();
            entity.Property(e => e.Aula).HasMaxLength(100);
            entity.Property(e => e.Activo).IsRequired();
            
            entity.HasOne(e => e.Ramo)
                .WithMany()
                .HasForeignKey(e => e.RamoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de ConfiguracionAsistencia
        modelBuilder.Entity<ConfiguracionAsistencia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LimitePresenteMinutos).IsRequired();
            entity.Property(e => e.PermiteRegistroManual).IsRequired();
            entity.Property(e => e.NotificarTardanzas).IsRequired();
            entity.Property(e => e.MarcarAusenteAutomatico).IsRequired();
            
            entity.HasOne(e => e.Clase)
                .WithOne()
                .HasForeignKey<ConfiguracionAsistencia>(e => e.ClaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de LogAuditoria
        modelBuilder.Entity<LogAuditoria>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Accion).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Justificacion).HasMaxLength(500);
            entity.Property(e => e.TimestampUtc).IsRequired();
            entity.Property(e => e.DatosAnteriores).HasColumnType("text");
            
            entity.HasOne(e => e.Asistencia)
                .WithMany()
                .HasForeignKey(e => e.AsistenciaId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}