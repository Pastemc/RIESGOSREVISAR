// ============================================================
// Archivo: Services/SnapshotService.cs
// ============================================================
using Microsoft.EntityFrameworkCore;
using RiesgosElor.Data;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

/// <summary>
/// Gestiona la creación, lectura y cierre de snapshots por periodo.
/// REGLA FUNDAMENTAL: al abrir un periodo nuevo, los datos se copian
/// SIEMPRE desde el snapshot del periodo anterior cerrado (no desde
/// la tabla Riesgos base, que puede haber sido editada después del cierre).
/// Si no existe ningún periodo cerrado previo (primer periodo), se copia
/// desde la tabla Riesgos base.
/// </summary>
public class SnapshotService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SnapshotService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 1. PERIODOS
    // ══════════════════════════════════════════════════════════════════════

    public async Task<List<PeriodoRiesgo>> GetPeriodosOrdenadosAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.PeriodosRiesgo
            .Where(p => p.Estado != "Eliminado")
            .OrderByDescending(p => p.FechaInicio)
            .ToListAsync();
    }

    public async Task<PeriodoRiesgo?> GetPeriodoActivoAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.PeriodosRiesgo
            .FirstOrDefaultAsync(p => p.Estado == "Activo");
    }

    // ══════════════════════════════════════════════════════════════════════
    // 2. INICIALIZAR SNAPSHOT AL ABRIR PERIODO
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Crea los SnapshotMatriz + SnapshotRiesgo para el nuevo periodo.
    /// Firma de 2 parámetros — compatible con PeriodoRiesgoService.
    /// Fuente: snapshot del último periodo CERRADO, o Riesgos base si es el primero.
    /// </summary>
    public async Task InicializarSnapshotPeriodoAsync(int periodoId, string usuarioActual)
    {
        using var db = _dbFactory.CreateDbContext();

        // Verificar que no existan ya snapshots para este periodo
        var yaExiste = await db.SnapshotMatriz.AnyAsync(sm => sm.PeriodoId == periodoId);
        if (yaExiste) return;

        // Verificar que el periodo exista
        var periodoActual = await db.PeriodosRiesgo.FindAsync(periodoId);
        if (periodoActual == null) return;

        // Buscar el último periodo CERRADO anterior
        var ultimoPeriodoCerrado = await db.PeriodosRiesgo
            .Where(p => p.Estado == "Cerrado" && p.Id != periodoId)
            .OrderByDescending(p => p.FechaCierre)
            .FirstOrDefaultAsync();

        var matrices = await db.MatrizGrupos
            .OrderBy(m => m.Numero)
            .ToListAsync();

        foreach (var matriz in matrices)
        {
            var snapshotMatriz = new SnapshotMatriz
            {
                PeriodoId     = periodoId,
                MatrizGrupoId = matriz.Id,
                Estado        = "Borrador",
                CreadoEn      = DateTime.UtcNow
            };
            db.SnapshotMatriz.Add(snapshotMatriz);
            await db.SaveChangesAsync();

            if (ultimoPeriodoCerrado != null)
            {
                // ── CASO 1: copiar desde snapshot del periodo cerrado anterior ──
                var snapshotAnterior = await db.SnapshotMatriz
                    .Include(sm => sm.Riesgos)
                        .ThenInclude(sr => sr.Controles)
                    .Include(sm => sm.Riesgos)
                        .ThenInclude(sr => sr.Planes)
                    .Include(sm => sm.Riesgos)
                        .ThenInclude(sr => sr.Indicadores)
                    .FirstOrDefaultAsync(sm =>
                        sm.PeriodoId     == ultimoPeriodoCerrado.Id &&
                        sm.MatrizGrupoId == matriz.Id);

                if (snapshotAnterior == null || !snapshotAnterior.Riesgos.Any())
                    continue;

                foreach (var rAnt in snapshotAnterior.Riesgos.OrderBy(r => r.CodigoRiesgo))
                {
                    var nuevo = new SnapshotRiesgo
                    {
                        SnapshotMatrizId      = snapshotMatriz.Id,
                        RiesgoOrigenId        = rAnt.RiesgoOrigenId,
                        CodigoProceso         = rAnt.CodigoProceso,
                        NombreProceso         = rAnt.NombreProceso,
                        GerenciaResponsable   = rAnt.GerenciaResponsable,
                        Subproceso            = rAnt.Subproceso,
                        CodigoRiesgo          = rAnt.CodigoRiesgo,
                        DescripcionRiesgo     = rAnt.DescripcionRiesgo,
                        OrigenRiesgo          = rAnt.OrigenRiesgo,
                        FrecuenciaRiesgo      = rAnt.FrecuenciaRiesgo,
                        TipoRiesgo            = rAnt.TipoRiesgo,
                        ProbabilidadInherente = rAnt.ProbabilidadInherente,
                        ImpactoInherente      = rAnt.ImpactoInherente,
                        ProbabilidadResidual  = rAnt.ProbabilidadResidual,
                        ImpactoResidual       = rAnt.ImpactoResidual,
                        EstrategiaResidual    = rAnt.EstrategiaResidual,
                        CreadoPor             = usuarioActual
                    };
                    db.SnapshotRiesgo.Add(nuevo);
                    await db.SaveChangesAsync();

                    foreach (var ctrl in rAnt.Controles)
                        db.SnapshotControl.Add(new SnapshotControl
                        {
                            SnapshotRiesgoId      = nuevo.Id,
                            CodigoControl         = ctrl.CodigoControl,
                            DescripcionControl    = ctrl.DescripcionControl,
                            AreaResponsable       = ctrl.AreaResponsable,
                            ResponsablesControl   = ctrl.ResponsablesControl,
                            FrecuenciaControl     = ctrl.FrecuenciaControl,
                            OportunidadControl    = ctrl.OportunidadControl,
                            AutomatizacionControl = ctrl.AutomatizacionControl,
                            EvidenciaControl      = ctrl.EvidenciaControl
                        });

                    foreach (var plan in rAnt.Planes)
                        db.SnapshotPlanAccion.Add(new SnapshotPlanAccion
                        {
                            SnapshotRiesgoId    = nuevo.Id,
                            CodigoPlan          = plan.CodigoPlan,
                            DescripcionPlan     = plan.DescripcionPlan,
                            AreaResponsable     = plan.AreaResponsable,
                            ResponsablePlan     = plan.ResponsablePlan,
                            InicioPlan          = plan.InicioPlan,
                            FinPlan             = plan.FinPlan,
                            EstadoPlan          = plan.EstadoPlan,
                            EstrategiaRespuesta = plan.EstrategiaRespuesta
                        });

                    foreach (var ind in rAnt.Indicadores)
                        db.SnapshotIndicador.Add(new SnapshotIndicador
                        {
                            SnapshotRiesgoId = nuevo.Id,
                            CodigoKRI        = ind.CodigoKRI,
                            DefinicionKRI    = ind.DefinicionKRI,
                            Frecuencia       = ind.Frecuencia,
                            MetaKRI          = ind.MetaKRI,
                            KRIActual        = ind.KRIActual,
                            ResponsableKRI   = ind.ResponsableKRI
                        });

                    await db.SaveChangesAsync();
                }
            }
            else
            {
                // ── CASO 2: primer periodo del sistema, copiar desde Riesgos base ──
                var riesgosBase = await db.Riesgos
                    .Include(r => r.Controles)
                    .Include(r => r.PlanesAccion)
                    .Include(r => r.Indicadores)
                    .Where(r => r.MatrizGrupoId == matriz.Id)
                    .OrderBy(r => r.CodigoRiesgo)
                    .ToListAsync();

                foreach (var r in riesgosBase)
                {
                    var nuevo = new SnapshotRiesgo
                    {
                        SnapshotMatrizId      = snapshotMatriz.Id,
                        RiesgoOrigenId        = r.Id,
                        CodigoProceso         = r.CodigoProceso,
                        NombreProceso         = r.NombreProceso,
                        GerenciaResponsable   = r.GerenciaResponsable ?? "",
                        Subproceso            = r.Subproceso ?? "",
                        CodigoRiesgo          = r.CodigoRiesgo,
                        DescripcionRiesgo     = r.DescripcionRiesgo ?? "",
                        OrigenRiesgo          = r.OrigenRiesgo ?? "",
                        FrecuenciaRiesgo      = r.FrecuenciaRiesgo ?? "",
                        TipoRiesgo            = r.TipoRiesgo ?? "",
                        ProbabilidadInherente = r.ProbabilidadInherente,
                        ImpactoInherente      = r.ImpactoInherente,
                        ProbabilidadResidual  = r.ProbabilidadResidual,
                        ImpactoResidual       = r.ImpactoResidual,
                        EstrategiaResidual    = r.EstrategiaResidual ?? "",
                        CreadoPor             = usuarioActual
                    };
                    db.SnapshotRiesgo.Add(nuevo);
                    await db.SaveChangesAsync();

                    foreach (var ctrl in r.Controles)
                        db.SnapshotControl.Add(new SnapshotControl
                        {
                            SnapshotRiesgoId      = nuevo.Id,
                            CodigoControl         = ctrl.CodigoControl,
                            DescripcionControl    = ctrl.DescripcionControl ?? "",
                            AreaResponsable       = ctrl.AreaResponsable ?? "",
                            ResponsablesControl   = ctrl.ResponsablesControl ?? "",
                            FrecuenciaControl     = ctrl.FrecuenciaControl ?? "",
                            OportunidadControl    = ctrl.OportunidadControl ?? "",
                            AutomatizacionControl = ctrl.AutomatizacionControl ?? "",
                            EvidenciaControl      = ctrl.EvidenciaControl ?? ""
                        });

                    foreach (var plan in r.PlanesAccion)
                        db.SnapshotPlanAccion.Add(new SnapshotPlanAccion
                        {
                            SnapshotRiesgoId    = nuevo.Id,
                            CodigoPlan          = plan.CodigoPlan,
                            DescripcionPlan     = plan.DescripcionPlan ?? "",
                            AreaResponsable     = plan.AreaResponsable ?? "",
                            ResponsablePlan     = plan.ResponsablePlan ?? "",
                            InicioPlan          = plan.InicioPlan,
                            FinPlan             = plan.FinPlan,
                            EstadoPlan          = plan.EstadoPlan ?? "",
                            EstrategiaRespuesta = plan.EstrategiaRespuesta ?? ""
                        });

                    foreach (var ind in r.Indicadores)
                        db.SnapshotIndicador.Add(new SnapshotIndicador
                        {
                            SnapshotRiesgoId = nuevo.Id,
                            CodigoKRI        = ind.CodigoKRI,
                            DefinicionKRI    = ind.DefinicionKRI ?? "",
                            Frecuencia       = ind.Frecuencia ?? "",
                            MetaKRI          = ind.MetaKRI ?? "",
                            KRIActual        = ind.KRIActual ?? "",
                            ResponsableKRI   = ind.ResponsableKRI ?? ""
                        });

                    await db.SaveChangesAsync();
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // 3. OBTENER SNAPSHOT DE UNA MATRIZ EN UN PERIODO
    // ══════════════════════════════════════════════════════════════════════

    public async Task<SnapshotMatriz?> GetSnapshotMatrizAsync(int periodoId, int matrizGrupoId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.SnapshotMatriz
            .Include(sm => sm.Riesgos)
                .ThenInclude(sr => sr.Controles)
            .Include(sm => sm.Riesgos)
                .ThenInclude(sr => sr.Planes)
            .Include(sm => sm.Riesgos)
                .ThenInclude(sr => sr.Indicadores)
            .FirstOrDefaultAsync(sm =>
                sm.PeriodoId     == periodoId &&
                sm.MatrizGrupoId == matrizGrupoId);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 4. GUARDAR / ACTUALIZAR UN SNAPSHOT RIESGO COMPLETO (5 parámetros)
    // ══════════════════════════════════════════════════════════════════════

    public async Task GuardarSnapshotRiesgoAsync(
        SnapshotRiesgo           riesgo,
        List<SnapshotControl>    controles,
        List<SnapshotPlanAccion> planes,
        List<SnapshotIndicador>  indicadores,
        string                   usuario)
    {
        using var db = _dbFactory.CreateDbContext();

        riesgo.ModificadoPor = usuario;
        riesgo.ModificadoEn  = DateTime.UtcNow;

        if (riesgo.Id == 0)
        {
            db.SnapshotRiesgo.Add(riesgo);
            await db.SaveChangesAsync();
        }
        else
        {
            db.SnapshotRiesgo.Update(riesgo);
            await db.SaveChangesAsync();

            // Reemplazar hijos
            var vC = await db.SnapshotControl
                .Where(c => c.SnapshotRiesgoId == riesgo.Id).ToListAsync();
            db.SnapshotControl.RemoveRange(vC);

            var vP = await db.SnapshotPlanAccion
                .Where(p => p.SnapshotRiesgoId == riesgo.Id).ToListAsync();
            db.SnapshotPlanAccion.RemoveRange(vP);

            var vI = await db.SnapshotIndicador
                .Where(i => i.SnapshotRiesgoId == riesgo.Id).ToListAsync();
            db.SnapshotIndicador.RemoveRange(vI);

            await db.SaveChangesAsync();
        }

        foreach (var c in controles)
        { c.Id = 0; c.SnapshotRiesgoId = riesgo.Id; db.SnapshotControl.Add(c); }
        foreach (var p in planes)
        { p.Id = 0; p.SnapshotRiesgoId = riesgo.Id; db.SnapshotPlanAccion.Add(p); }
        foreach (var i in indicadores)
        { i.Id = 0; i.SnapshotRiesgoId = riesgo.Id; db.SnapshotIndicador.Add(i); }

        await db.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════════════════════════════
    // 5. NUEVO RIESGO EN EL SNAPSHOT (riesgo nuevo dentro del periodo activo)
    // ══════════════════════════════════════════════════════════════════════

    public async Task<SnapshotRiesgo> NuevoSnapshotRiesgoAsync(
        int periodoId, int matrizGrupoId, MatrizGrupo matriz, string usuario)
    {
        using var db = _dbFactory.CreateDbContext();

        var sm = await db.SnapshotMatriz
            .FirstOrDefaultAsync(x =>
                x.PeriodoId     == periodoId &&
                x.MatrizGrupoId == matrizGrupoId);

        if (sm == null)
        {
            sm = new SnapshotMatriz
            {
                PeriodoId     = periodoId,
                MatrizGrupoId = matrizGrupoId,
                Estado        = "Borrador",
                CreadoEn      = DateTime.UtcNow
            };
            db.SnapshotMatriz.Add(sm);
            await db.SaveChangesAsync();
        }

        var conteo = await db.SnapshotRiesgo
            .CountAsync(sr => sr.SnapshotMatrizId == sm.Id);

        var nuevo = new SnapshotRiesgo
        {
            SnapshotMatrizId  = sm.Id,
            RiesgoOrigenId    = null,
            CodigoProceso     = matriz.CodigoProceso,
            NombreProceso     = matriz.NombreProceso,
            CodigoRiesgo      = $"{matriz.CodigoProceso}.R{(conteo + 1):D2}",
            CreadoPor         = usuario,
            ModificadoEn      = DateTime.UtcNow
        };
        db.SnapshotRiesgo.Add(nuevo);
        await db.SaveChangesAsync();
        return nuevo;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 6. ELIMINAR SNAPSHOT RIESGO
    // ══════════════════════════════════════════════════════════════════════

    public async Task EliminarSnapshotRiesgoAsync(int snapshotRiesgoId)
    {
        using var db = _dbFactory.CreateDbContext();
        var sr = await db.SnapshotRiesgo.FindAsync(snapshotRiesgoId);
        if (sr != null)
        {
            db.SnapshotRiesgo.Remove(sr);
            await db.SaveChangesAsync();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // 7. CONTAR RIESGOS EN SNAPSHOT (para el mapa de procesos)
    // ══════════════════════════════════════════════════════════════════════

    public async Task<Dictionary<int, int>> GetConteoSnapshotAsync(int periodoId)
    {
        using var db = _dbFactory.CreateDbContext();
        var conteos = await db.SnapshotMatriz
            .Where(sm => sm.PeriodoId == periodoId)
            .Select(sm => new { sm.MatrizGrupoId, Count = sm.Riesgos.Count })
            .ToListAsync();

        return conteos.ToDictionary(x => x.MatrizGrupoId, x => x.Count);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 8. CONTAR RIESGOS EN TABLA BASE (para comparar en mapa de procesos)
    // ══════════════════════════════════════════════════════════════════════

    public async Task<Dictionary<int, int>> GetConteoBaseAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        var conteos = await db.Riesgos
            .Where(r => r.MatrizGrupoId != null)
            .GroupBy(r => r.MatrizGrupoId!.Value)
            .Select(g => new { MatrizGrupoId = g.Key, Count = g.Count() })
            .ToListAsync();

        return conteos.ToDictionary(x => x.MatrizGrupoId, x => x.Count);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 9. CERRAR SNAPSHOTS AL CERRAR EL PERIODO
    // ══════════════════════════════════════════════════════════════════════

    public async Task CerrarSnapshotsPeriodoAsync(int periodoId)
    {
        using var db = _dbFactory.CreateDbContext();
        var snapshots = await db.SnapshotMatriz
            .Where(sm => sm.PeriodoId == periodoId)
            .ToListAsync();

        foreach (var sm in snapshots)
        {
            sm.Estado    = "Cerrado";
            sm.CerradoEn = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    // ══════════════════════════════════════════════════════════════════════
    // 10. TODOS LOS RIESGOS DE UN PERIODO (para VistaDetallada / Resumen)
    // ══════════════════════════════════════════════════════════════════════

    public async Task<List<SnapshotRiesgo>> GetSnapshotRiesgosPorPeriodoAsync(int periodoId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.SnapshotMatriz
            .Where(sm => sm.PeriodoId == periodoId)
            .SelectMany(sm => sm.Riesgos)
            .Include(sr => sr.Controles)
            .Include(sr => sr.Planes)
            .Include(sr => sr.Indicadores)
            .OrderBy(sr => sr.CodigoProceso)
            .ThenBy(sr => sr.CodigoRiesgo)
            .ToListAsync();
    }
}
