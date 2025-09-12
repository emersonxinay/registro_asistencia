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
            
            // Relación opcional con Ramo
            entity.HasOne(e => e.Ramo)
                .WithMany(r => r.Clases)
                .HasForeignKey(e => e.RamoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Asistencia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AlumnoId).IsRequired();
            entity.Property(e => e.ClaseId).IsRequired();
            entity.Property(e => e.MarcadaUtc).IsRequired();
            entity.Property(e => e.Metodo).IsRequired().HasMaxLength(50);
            
            entity.HasIndex(e => new { e.AlumnoId, e.ClaseId }).IsUnique();
            
            entity.HasOne<Alumno>()
                .WithMany()
                .HasForeignKey(e => e.AlumnoId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne<Clase>()
                .WithMany()
                .HasForeignKey(e => e.ClaseId)
                .OnDelete(DeleteBehavior.Cascade);
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

        base.OnModelCreating(modelBuilder);
    }
}