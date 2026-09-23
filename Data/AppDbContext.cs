// ============================================================
// Archivo: Data/AppDbContext.cs  — VERSIÓN ACTUALIZADA
// Agrega los 5 DbSet de snapshot al contexto existente
// ============================================================
using Microsoft.EntityFrameworkCore;
using RiesgosElor.Models;

namespace RiesgosElor.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Tablas existentes (sin cambios) ─────────────────────────────────
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<CambioPassword> CambiosPassword { get; set; }
    public DbSet<Riesgo> Riesgos { get; set; }
    public DbSet<RiesgoControl> RiesgosControl { get; set; }
    public DbSet<PlanAccion> PlanesAccion { get; set; }
    public DbSet<Indicador> Indicadores { get; set; }
    public DbSet<MatrizEncabezado> MatrizEncabezados { get; set; }
    public DbSet<MatrizGrupo> MatrizGrupos { get; set; }
    public DbSet<UsuarioMatriz> UsuarioMatrices { get; set; }
    public DbSet<SabanaEncabezado> SabanaEncabezados { get; set; }
    public DbSet<EventoRiesgo> EventosRiesgo { get; set; }
    public DbSet<ObligacionNormativa> ObligacionesNormativas { get; set; }
    public DbSet<AuditoriaGrc> AuditoriasGrc { get; set; }
    public DbSet<HallazgoAuditoria> HallazgosAuditoria { get; set; }
    public DbSet<DocumentoGrc> DocumentosGrc { get; set; }
    public DbSet<ActivoInformacion> ActivosInformacion { get; set; }
    public DbSet<EvaluacionControl> EvaluacionesControl { get; set; }
    public DbSet<MaestroProceso> MaestroProcesos { get; set; }
    public DbSet<MaestroArea> MaestroAreas { get; set; }
    public DbSet<MaestroTipoRiesgo> MaestroTiposRiesgo { get; set; }
    public DbSet<MaestroParametro> MaestroParametros { get; set; }
    public DbSet<MaestroResponsable> MaestroResponsables { get; set; }
    public DbSet<BitacoraDepartamentoGerencia> BitacoraDepartamentoGerencias { get; set; }
    public DbSet<BitacoraUsuario> BitacorasUsuario { get; set; }
    public DbSet<PeriodoRiesgo> PeriodosRiesgo { get; set; }
    public DbSet<BitacoraPeriodoRiesgo> BitacoraPeriodoRiesgo { get; set; }

    // ── NUEVOS: Snapshot por periodo ────────────────────────────────────
    public DbSet<SnapshotMatriz> SnapshotMatriz { get; set; }
    public DbSet<SnapshotRiesgo> SnapshotRiesgo { get; set; }
    public DbSet<SnapshotControl> SnapshotControl { get; set; }
    public DbSet<SnapshotPlanAccion> SnapshotPlanAccion { get; set; }
    public DbSet<SnapshotIndicador> SnapshotIndicador { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Configuraciones existentes (sin cambios) ────────────────────
        modelBuilder.Entity<MaestroArea>()
            .HasOne(m => m.Padre)
            .WithMany(m => m.UnidadesHijas)
            .HasForeignKey(m => m.PadreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Correo).IsUnique();

        modelBuilder.Entity<Riesgo>()
            .Ignore(r => r.SeveridadInherente)
            .Ignore(r => r.NivelInherente)
            .Ignore(r => r.SeveridadResidual)
            .Ignore(r => r.NivelResidual)
            .Ignore(r => r.RequierePlanAccion);

        modelBuilder.Entity<Riesgo>()
            .HasMany(r => r.Controles)
            .WithOne(c => c.Riesgo)
            .HasForeignKey(c => c.RiesgoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Riesgo>()
            .HasMany(r => r.PlanesAccion)
            .WithOne(p => p.Riesgo)
            .HasForeignKey(p => p.RiesgoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Riesgo>()
            .HasMany(r => r.Indicadores)
            .WithOne(i => i.Riesgo)
            .HasForeignKey(i => i.RiesgoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MatrizGrupo>()
            .HasMany(m => m.Riesgos)
            .WithOne(r => r.MatrizGrupo)
            .HasForeignKey(r => r.MatrizGrupoId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UsuarioMatriz>()
            .HasOne(um => um.Usuario)
            .WithMany()
            .HasForeignKey(um => um.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UsuarioMatriz>()
            .HasOne(um => um.MatrizGrupo)
            .WithMany()
            .HasForeignKey(um => um.MatrizGrupoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EventoRiesgo>()
            .Property(e => e.MontoPerdidaEstimado)
            .HasPrecision(18, 2);

        modelBuilder.Entity<EventoRiesgo>()
            .HasOne(e => e.Riesgo)
            .WithMany()
            .HasForeignKey(e => e.RiesgoId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ObligacionNormativa>()
            .HasOne(o => o.Riesgo)
            .WithMany()
            .HasForeignKey(o => o.RiesgoId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditoriaGrc>()
            .HasMany(a => a.Hallazgos)
            .WithOne(h => h.AuditoriaGrc)
            .HasForeignKey(h => h.AuditoriaGrcId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HallazgoAuditoria>()
            .HasOne(h => h.PlanAccion)
            .WithMany()
            .HasForeignKey(h => h.PlanAccionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ActivoInformacion>()
            .HasOne(a => a.Riesgo)
            .WithMany()
            .HasForeignKey(a => a.RiesgoId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EvaluacionControl>()
            .HasOne(e => e.RiesgoControl)
            .WithMany()
            .HasForeignKey(e => e.RiesgoControlId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PeriodoRiesgo>(e =>
        {
            e.ToTable("PeriodosRiesgo");
            e.HasMany(p => p.Bitacora)
             .WithOne(b => b.Periodo)
             .HasForeignKey(b => b.PeriodoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BitacoraPeriodoRiesgo>(e =>
        {
            e.ToTable("BitacoraPeriodoRiesgo");
        });

        // ── NUEVAS: Configuraciones Snapshot ────────────────────────────

        // SnapshotMatriz: restricción única (PeriodoId, MatrizGrupoId)
        modelBuilder.Entity<SnapshotMatriz>(e =>
        {
            e.ToTable("SnapshotMatriz");
            e.HasIndex(sm => new { sm.PeriodoId, sm.MatrizGrupoId }).IsUnique();

            e.HasOne(sm => sm.Periodo)
             .WithMany()
             .HasForeignKey(sm => sm.PeriodoId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(sm => sm.MatrizGrupo)
             .WithMany()
             .HasForeignKey(sm => sm.MatrizGrupoId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(sm => sm.Riesgos)
             .WithOne(sr => sr.SnapshotMatriz)
             .HasForeignKey(sr => sr.SnapshotMatrizId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // SnapshotRiesgo
        modelBuilder.Entity<SnapshotRiesgo>(e =>
        {
            e.ToTable("SnapshotRiesgo");

            e.HasOne(sr => sr.RiesgoOrigen)
             .WithMany()
             .HasForeignKey(sr => sr.RiesgoOrigenId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasMany(sr => sr.Controles)
             .WithOne(sc => sc.SnapshotRiesgo)
             .HasForeignKey(sc => sc.SnapshotRiesgoId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(sr => sr.Planes)
             .WithOne(sp => sp.SnapshotRiesgo)
             .HasForeignKey(sp => sp.SnapshotRiesgoId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(sr => sr.Indicadores)
             .WithOne(si => si.SnapshotRiesgo)
             .HasForeignKey(si => si.SnapshotRiesgoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SnapshotControl>(e => e.ToTable("SnapshotControl"));
        modelBuilder.Entity<SnapshotPlanAccion>(e => e.ToTable("SnapshotPlanAccion"));
        modelBuilder.Entity<SnapshotIndicador>(e => e.ToTable("SnapshotIndicador"));
    }
}