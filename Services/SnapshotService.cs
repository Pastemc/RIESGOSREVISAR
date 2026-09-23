// ============================================================
// Archivo: Services/SnapshotService.cs  — VERSIÓN FINAL
//
// COMPORTAMIENTO CORRECTO:
//   Al abrir Periodo N:
//     → Copia TODOS los datos del periodo N-1 (prob, impacto,
//       controles completos, planes, KRI incluidos)
//     → El usuario edita lo que cambió en este periodo
//     → Los datos del periodo anterior quedan INTOCABLES en BD
//   Al editar en periodo activo:
//     → UPDATE solo sobre los SnapshotRiesgo de ESE periodo
//   Al cerrar periodo:
//     → SnapshotMatriz.Estado = "Cerrado" → guardia impide escritura
// ============================================================
using Microsoft.EntityFrameworkCore;
using RiesgosElor.Data;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

public class SnapshotService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SnapshotService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ─────────────────────────────────────────────────────────────
    // 1. INICIALIZAR SNAPSHOT AL ABRIR PERIODO
    //    Copia TODO del periodo anterior (incluyendo evaluaciones,
    //    controles completos, planes y KRI) como punto de partida.
    //    El usuario solo edita lo que cambió.
    // ─────────────────────────────────────────────────────────────
    public async Task InicializarSnapshotPeriodoAsync(int periodoId, string usuarioActual)
    {
        using var db = _dbFactory.CreateDbContext();

        var periodo = await db.PeriodosRiesgo.FindAsync(periodoId);
        if (periodo == null || periodo.Estado != "Activo") return;

        // Último periodo cerrado (fuente de datos para copiar)
        var periodoAnterior = await db.PeriodosRiesgo
            .Where(p => p.Estado == "Cerrado" && p.Id != periodoId)
            .OrderByDescending(p => p.FechaCierre)
            .FirstOrDefaultAsync();

        var matrices = await db.MatrizGrupos.ToListAsync();

        foreach (var matriz in matrices)
        {
            // Nunca duplicar
            var existe = await db.SnapshotMatriz
                .AnyAsync(sm => sm.PeriodoId == periodoId && sm.MatrizGrupoId == matriz.Id);
            if (existe) continue;

            // Crear SnapshotMatriz del nuevo periodo
            var nuevoSM = new SnapshotMatriz
            {
                PeriodoId = periodoId,
                MatrizGrupoId = matriz.Id,
                Estado = "Borrador",
                CreadoEn = DateTime.UtcNow
            };
            db.SnapshotMatriz.Add(nuevoSM);
            await db.SaveChangesAsync();

            // ── Fuente: snapshot del periodo anterior ─────────────
            if (periodoAnterior != null)
            {
                var snapAnt = await db.SnapshotMatriz
                    .Include(sm => sm.Riesgos)
                        .ThenInclude(sr => sr.Controles)
                    .Include(sm => sm.Riesgos)
                        .ThenInclude(sr => sr.Planes)
                    .Include(sm => sm.Riesgos)
                        .ThenInclude(sr => sr.Indicadores)
                    .FirstOrDefaultAsync(sm => sm.PeriodoId == periodoAnterior.Id
                                            && sm.MatrizGrupoId == matriz.Id);

                if (snapAnt != null && snapAnt.Riesgos.Any())
                {
                    foreach (var rAnt in snapAnt.Riesgos)
                    {
                        // Copiar riesgo CON TODOS sus datos (no en blanco)
                        var srNuevo = new SnapshotRiesgo
                        {
                            SnapshotMatrizId = nuevoSM.Id,
                            RiesgoOrigenId = rAnt.RiesgoOrigenId,
                            CodigoProceso = rAnt.CodigoProceso,
                            NombreProceso = rAnt.NombreProceso,
                            GerenciaResponsable = rAnt.GerenciaResponsable,
                            Subproceso = rAnt.Subproceso,
                            CodigoRiesgo = rAnt.CodigoRiesgo,
                            DescripcionRiesgo = rAnt.DescripcionRiesgo,
                            OrigenRiesgo = rAnt.OrigenRiesgo,
                            FrecuenciaRiesgo = rAnt.FrecuenciaRiesgo,
                            TipoRiesgo = rAnt.TipoRiesgo,
                            // ── Evaluación: se copia del anterior (editable en el nuevo periodo)
                            ProbabilidadInherente = rAnt.ProbabilidadInherente,
                            ImpactoInherente = rAnt.ImpactoInherente,
                            ProbabilidadResidual = rAnt.ProbabilidadResidual,
                            ImpactoResidual = rAnt.ImpactoResidual,
                            EstrategiaResidual = rAnt.EstrategiaResidual,
                            CreadoPor = usuarioActual,
                            ModificadoEn = DateTime.UtcNow
                        };
                        db.SnapshotRiesgo.Add(srNuevo);
                        await db.SaveChangesAsync();

                        // Copiar controles COMPLETOS (con todos los campos)
                        foreach (var cAnt in rAnt.Controles)
                        {
                            db.SnapshotControl.Add(new SnapshotControl
                            {
                                SnapshotRiesgoId = srNuevo.Id,
                                CodigoControl = cAnt.CodigoControl,
                                DescripcionControl = cAnt.DescripcionControl,
                                AreaResponsable = cAnt.AreaResponsable,
                                ResponsablesControl = cAnt.ResponsablesControl,
                                FrecuenciaControl = cAnt.FrecuenciaControl,
                                OportunidadControl = cAnt.OportunidadControl,
                                AutomatizacionControl = cAnt.AutomatizacionControl,
                                EvidenciaControl = cAnt.EvidenciaControl
                            });
                        }

                        // Copiar planes COMPLETOS
                        foreach (var pAnt in rAnt.Planes)
                        {
                            db.SnapshotPlanAccion.Add(new SnapshotPlanAccion
                            {
                                SnapshotRiesgoId = srNuevo.Id,
                                CodigoPlan = pAnt.CodigoPlan,
                                EstrategiaRespuesta = pAnt.EstrategiaRespuesta,
                                DescripcionPlan = pAnt.DescripcionPlan,
                                AreaResponsable = pAnt.AreaResponsable,
                                ResponsablePlan = pAnt.ResponsablePlan,
                                InicioPlan = pAnt.InicioPlan,
                                FinPlan = pAnt.FinPlan,
                                EstadoPlan = pAnt.EstadoPlan
                            });
                        }

                        // Copiar indicadores KRI COMPLETOS
                        foreach (var iAnt in rAnt.Indicadores)
                        {
                            db.SnapshotIndicador.Add(new SnapshotIndicador
                            {
                                SnapshotRiesgoId = srNuevo.Id,
                                CodigoKRI = iAnt.CodigoKRI,
                                DefinicionKRI = iAnt.DefinicionKRI,
                                Frecuencia = iAnt.Frecuencia,
                                MetaKRI = iAnt.MetaKRI,
                                KRIActual = iAnt.KRIActual,
                                ResponsableKRI = iAnt.ResponsableKRI
                            });
                        }

                        await db.SaveChangesAsync();
                    }
                    continue; // matriz procesada desde snapshot anterior
                }
            }

            // ── Sin snapshot anterior: copiar desde tabla base Riesgos ──
            var riesgosBase = await db.Riesgos
                .Include(r => r.Controles)
                .Include(r => r.PlanesAccion)
                .Include(r => r.Indicadores)
                .Where(r => r.MatrizGrupoId == matriz.Id)
                .ToListAsync();

            foreach (var r in riesgosBase)
            {
                var sr = new SnapshotRiesgo
                {
                    SnapshotMatrizId = nuevoSM.Id,
                    RiesgoOrigenId = r.Id,
                    CodigoProceso = r.CodigoProceso,
                    NombreProceso = r.NombreProceso,
                    GerenciaResponsable = r.GerenciaResponsable ?? "",
                    Subproceso = r.Subproceso ?? "",
                    CodigoRiesgo = r.CodigoRiesgo,
                    DescripcionRiesgo = r.DescripcionRiesgo ?? "",
                    OrigenRiesgo = r.OrigenRiesgo ?? "",
                    FrecuenciaRiesgo = r.FrecuenciaRiesgo ?? "",
                    TipoRiesgo = r.TipoRiesgo ?? "",
                    ProbabilidadInherente = r.ProbabilidadInherente,
                    ImpactoInherente = r.ImpactoInherente,
                    ProbabilidadResidual = r.ProbabilidadResidual,
                    ImpactoResidual = r.ImpactoResidual,
                    EstrategiaResidual = r.EstrategiaResidual ?? "",
                    CreadoPor = usuarioActual,
                    ModificadoEn = DateTime.UtcNow
                };
                db.SnapshotRiesgo.Add(sr);
                await db.SaveChangesAsync();

                foreach (var c in r.Controles)
                {
                    db.SnapshotControl.Add(new SnapshotControl
                    {
                        SnapshotRiesgoId = sr.Id,
                        CodigoControl = c.CodigoControl,
                        DescripcionControl = c.DescripcionControl ?? "",
                        AreaResponsable = c.AreaResponsable ?? "",
                        ResponsablesControl = c.ResponsablesControl ?? "",
                        FrecuenciaControl = c.FrecuenciaControl ?? "",
                        OportunidadControl = c.OportunidadControl ?? "",
                        AutomatizacionControl = c.AutomatizacionControl ?? "",
                        EvidenciaControl = c.EvidenciaControl ?? ""
                    });
                }

                foreach (var p in r.PlanesAccion)
                {
                    db.SnapshotPlanAccion.Add(new SnapshotPlanAccion
                    {
                        SnapshotRiesgoId = sr.Id,
                        CodigoPlan = p.CodigoPlan ?? "",
                        EstrategiaRespuesta = p.EstrategiaRespuesta ?? "",
                        DescripcionPlan = p.DescripcionPlan ?? "",
                        AreaResponsable = p.AreaResponsable ?? "",
                        ResponsablePlan = p.ResponsablePlan ?? "",
                        InicioPlan = p.InicioPlan,
                        FinPlan = p.FinPlan,
                        EstadoPlan = p.EstadoPlan ?? ""
                    });
                }

                foreach (var i in r.Indicadores)
                {
                    db.SnapshotIndicador.Add(new SnapshotIndicador
                    {
                        SnapshotRiesgoId = sr.Id,
                        CodigoKRI = i.CodigoKRI ?? "",
                        DefinicionKRI = i.DefinicionKRI ?? "",
                        Frecuencia = i.Frecuencia ?? "",
                        MetaKRI = i.MetaKRI ?? "",
                        KRIActual = i.KRIActual ?? "",
                        ResponsableKRI = i.ResponsableKRI ?? ""
                    });
                }

                await db.SaveChangesAsync();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 2. LISTA DE PERIODOS
    // ─────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────
    // 3. OBTENER SNAPSHOT DE UNA MATRIZ EN UN PERIODO
    // ─────────────────────────────────────────────────────────────
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
            .FirstOrDefaultAsync(sm => sm.PeriodoId == periodoId
                                    && sm.MatrizGrupoId == matrizGrupoId);
    }

    // ─────────────────────────────────────────────────────────────
    // 4. GUARDAR SNAPSHOT RIESGO (solo en periodo ACTIVO)
    //    GUARDIA: rechaza escritura si el periodo está Cerrado
    // ─────────────────────────────────────────────────────────────
    public async Task GuardarSnapshotRiesgoAsync(
        SnapshotRiesgo riesgo,
        List<SnapshotControl> controles,
        List<SnapshotPlanAccion> planes,
        List<SnapshotIndicador> indicadores,
        string usuario)
    {
        using var db = _dbFactory.CreateDbContext();

        var sm = await db.SnapshotMatriz
            .FirstOrDefaultAsync(x => x.Id == riesgo.SnapshotMatrizId);

        if (sm == null)
            throw new InvalidOperationException("El snapshot de matriz no existe.");

        if (sm.Estado == "Cerrado")
            throw new InvalidOperationException(
                "Este periodo está cerrado. Los datos son de solo lectura.");

        riesgo.ModificadoPor = usuario;
        riesgo.ModificadoEn = DateTime.UtcNow;

        if (riesgo.Id == 0)
        {
            db.SnapshotRiesgo.Add(riesgo);
            await db.SaveChangesAsync();
        }
        else
        {
            db.SnapshotRiesgo.Update(riesgo);
            await db.SaveChangesAsync();

            var vCtrl = await db.SnapshotControl
                .Where(c => c.SnapshotRiesgoId == riesgo.Id).ToListAsync();
            db.SnapshotControl.RemoveRange(vCtrl);

            var vPlan = await db.SnapshotPlanAccion
                .Where(p => p.SnapshotRiesgoId == riesgo.Id).ToListAsync();
            db.SnapshotPlanAccion.RemoveRange(vPlan);

            var vInd = await db.SnapshotIndicador
                .Where(i => i.SnapshotRiesgoId == riesgo.Id).ToListAsync();
            db.SnapshotIndicador.RemoveRange(vInd);

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

    // ─────────────────────────────────────────────────────────────
    // 5. ELIMINAR SNAPSHOT RIESGO (solo en periodo ACTIVO)
    // ─────────────────────────────────────────────────────────────
    public async Task EliminarSnapshotRiesgoAsync(int snapshotRiesgoId)
    {
        using var db = _dbFactory.CreateDbContext();

        var sr = await db.SnapshotRiesgo
            .Include(r => r.SnapshotMatriz)
            .FirstOrDefaultAsync(r => r.Id == snapshotRiesgoId);

        if (sr == null) return;

        if (sr.SnapshotMatriz?.Estado == "Cerrado")
            throw new InvalidOperationException(
                "No se puede eliminar un riesgo de un periodo cerrado.");

        db.SnapshotRiesgo.Remove(sr);
        await db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────
    // 6. CERRAR SNAPSHOTS AL CERRAR PERIODO
    // ─────────────────────────────────────────────────────────────
    public async Task CerrarSnapshotsPeriodoAsync(int periodoId)
    {
        using var db = _dbFactory.CreateDbContext();

        var snapshots = await db.SnapshotMatriz
            .Where(sm => sm.PeriodoId == periodoId)
            .ToListAsync();

        foreach (var sm in snapshots)
        {
            sm.Estado = "Cerrado";
            sm.CerradoEn = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────
    // 7. CONTEO PARA EL MAPA DE PROCESOS
    // ─────────────────────────────────────────────────────────────
    public async Task<Dictionary<int, int>> GetConteoSnapshotAsync(int periodoId)
    {
        using var db = _dbFactory.CreateDbContext();

        var conteos = await db.SnapshotMatriz
            .Where(sm => sm.PeriodoId == periodoId)
            .Select(sm => new { sm.MatrizGrupoId, Count = sm.Riesgos.Count })
            .ToListAsync();

        return conteos.ToDictionary(x => x.MatrizGrupoId, x => x.Count);
    }

    // ─────────────────────────────────────────────────────────────
    // 8. CONTEO DESDE TABLA BASE (cuando no hay periodo seleccionado)
    //    Usa la tabla Riesgos directamente para el mapa inicial
    // ─────────────────────────────────────────────────────────────
    public async Task<Dictionary<int, int>> GetConteoBaseAsync()
    {
        using var db = _dbFactory.CreateDbContext();

        var conteos = await db.Riesgos
            .Where(r => r.MatrizGrupoId.HasValue)
            .GroupBy(r => r.MatrizGrupoId!.Value)
            .Select(g => new { MatrizId = g.Key, Count = g.Count() })
            .ToListAsync();

        return conteos.ToDictionary(x => x.MatrizId, x => x.Count);
    }

    // ─────────────────────────────────────────────────────────────
    // 9. AGREGAR NUEVO RIESGO AL PERIODO ACTIVO
    // ─────────────────────────────────────────────────────────────
    public async Task<SnapshotRiesgo> NuevoSnapshotRiesgoAsync(
        int periodoId, int matrizGrupoId, MatrizGrupo matriz, string usuario)
    {
        using var db = _dbFactory.CreateDbContext();

        var sm = await db.SnapshotMatriz
            .FirstOrDefaultAsync(x => x.PeriodoId == periodoId
                                   && x.MatrizGrupoId == matrizGrupoId);

        if (sm == null)
        {
            sm = new SnapshotMatriz
            {
                PeriodoId = periodoId,
                MatrizGrupoId = matrizGrupoId,
                Estado = "Borrador",
                CreadoEn = DateTime.UtcNow
            };
            db.SnapshotMatriz.Add(sm);
            await db.SaveChangesAsync();
        }

        if (sm.Estado == "Cerrado")
            throw new InvalidOperationException("No se puede agregar riesgos a un periodo cerrado.");

        var conteo = await db.SnapshotRiesgo
            .CountAsync(sr => sr.SnapshotMatrizId == sm.Id);

        var nuevo = new SnapshotRiesgo
        {
            SnapshotMatrizId = sm.Id,
            RiesgoOrigenId = null,
            CodigoProceso = matriz.CodigoProceso,
            NombreProceso = matriz.NombreProceso,
            CodigoRiesgo = $"{matriz.CodigoProceso}.R{(conteo + 1):D2}",
            ProbabilidadInherente = 1,
            ImpactoInherente = 1,
            ProbabilidadResidual = 1,
            ImpactoResidual = 1,
            CreadoPor = usuario,
            ModificadoEn = DateTime.UtcNow
        };

        db.SnapshotRiesgo.Add(nuevo);
        await db.SaveChangesAsync();
        return nuevo;
    }
}