using CiudadEducativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CiudadEducativa.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Colegio> Colegios => Set<Colegio>();
    public DbSet<Grado> Grados => Set<Grado>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<AnioAcademico> AniosAcademicos => Set<AnioAcademico>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<Docente> Docentes => Set<Docente>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
    public DbSet<DocenteColegio> DocenteColegios => Set<DocenteColegio>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Colegio>(e =>
        {
            e.ToTable("Colegios");
            e.Property(x => x.Nombre).HasMaxLength(150);
            e.Property(x => x.Sector).HasMaxLength(20);
        });

        modelBuilder.Entity<Grado>(e =>
        {
            e.ToTable("Grados");
            e.Property(x => x.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Grupo>(e =>
        {
            e.ToTable("Grupos");
            e.Property(x => x.Nombre).HasMaxLength(10);
            e.HasOne(x => x.Grado).WithMany(x => x.Grupos).HasForeignKey(x => x.GradoId);
            e.HasOne(x => x.DocenteDirector).WithMany(x => x.GruposDirigidos).HasForeignKey(x => x.DocenteDirectorId);
        });

        modelBuilder.Entity<AnioAcademico>(e =>
        {
            e.ToTable("AniosAcademicos");
            e.HasIndex(x => x.Anio).IsUnique();
        });

        modelBuilder.Entity<Estudiante>(e =>
        {
            e.ToTable("Estudiantes");
            e.Property(x => x.Nombre).HasMaxLength(150);
            e.Property(x => x.NumeroMatricula).HasMaxLength(30);
            e.HasIndex(x => x.NumeroMatricula).IsUnique();
        });

        modelBuilder.Entity<Docente>(e =>
        {
            e.ToTable("Docentes");
            e.Property(x => x.Nombre).HasMaxLength(150);
            e.Property(x => x.PeriodoContrato).HasMaxLength(50);
        });

        modelBuilder.Entity<Matricula>(e =>
        {
            e.ToTable("Matriculas");
            e.HasOne(x => x.Estudiante).WithMany(x => x.Matriculas).HasForeignKey(x => x.EstudianteId);
            e.HasOne(x => x.Colegio).WithMany(x => x.Matriculas).HasForeignKey(x => x.ColegioId);
            e.HasOne(x => x.Grado).WithMany(x => x.Matriculas).HasForeignKey(x => x.GradoId);
            e.HasOne(x => x.Grupo).WithMany(x => x.Matriculas).HasForeignKey(x => x.GrupoId);
            e.HasOne(x => x.AnioAcademico).WithMany(x => x.Matriculas).HasForeignKey(x => x.AnioAcademicoId);
        });

        modelBuilder.Entity<DocenteColegio>(e =>
        {
            e.ToTable("DocenteColegios");
            e.HasIndex(x => new { x.DocenteId, x.ColegioId }).IsUnique();
            e.HasOne(x => x.Docente).WithMany(x => x.DocenteColegios).HasForeignKey(x => x.DocenteId);
            e.HasOne(x => x.Colegio).WithMany(x => x.DocenteColegios).HasForeignKey(x => x.ColegioId);
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.PasswordHash).HasMaxLength(255);
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.Property(x => x.Rol).HasMaxLength(20);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Colegio).WithMany().HasForeignKey(x => x.ColegioId);
        });
    }
}
