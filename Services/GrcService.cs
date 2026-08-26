using Microsoft.EntityFrameworkCore;
using RiesgosElor.Data;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

public class GrcService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public GrcService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    private AppDbContext Db() => _factory.CreateDbContext();

    public static readonly List<string> EstadosEvento = new()
    {
        "Reportado", "En analisis", "En tratamiento", "Cerrado"
    };

    public static readonly List<string> EstadosCumplimiento = new()
    {
        "Pendiente", "En proceso", "Cumplida", "Vencida", "No aplica"
    };

    public static readonly List<string> EstadosAuditoria = new()
    {
        "Planificada", "En ejecucion", "Informe emitido", "Cerrada", "Cancelada"
    };

    public static readonly List<string> EstadosHallazgo = new()
    {
        "Abierto", "En remediacion", "Implementado", "Cerrado"
    };

    public static readonly List<string> EstadosDocumento = new()
    {
        "Borrador", "Vigente", "En revision", "Obsoleto"
    };

    public static readonly List<string> NivelesCriticidad = new()
    {
        "Baja", "Media", "Alta", "Critica"
    };

    public async Task<GrcResumen> GetResumenAsync(int usuarioId, string rol)
    {
        using var db = Db();
        var hoy = DateTime.Today;
        var (codigosProceso, riesgoIdsAutorizados) = await GetAmbitoAutorizadoAsync(db, usuarioId, rol);
        var riesgoQuery = db.Riesgos.AsNoTracking();

        if (!TieneAccesoTotal(rol))
        {
            riesgoQuery = riesgoQuery.Where(r => riesgoIdsAutorizados.Contains(r.Id));
        }

        var riesgos = await riesgoQuery.ToListAsync();
        var riesgoIds = riesgos.Select(r => r.Id).ToList();

        var eventos = FiltrarEventos(db.EventosRiesgo.AsNoTracking(), rol, codigosProceso, riesgoIds);
        var obligaciones = FiltrarObligaciones(db.ObligacionesNormativas.AsNoTracking(), rol, codigosProceso, riesgoIds);
        var auditorias = FiltrarAuditorias(db.AuditoriasGrc.AsNoTracking(), rol, codigosProceso);
        var hallazgos = FiltrarHallazgos(db.HallazgosAuditoria.AsNoTracking(), rol, codigosProceso);
        var documentos = FiltrarDocumentos(db.DocumentosGrc.AsNoTracking(), rol, codigosProceso);
        var activos = FiltrarActivos(db.ActivosInformacion.AsNoTracking(), rol, codigosProceso, riesgoIds);
        var evaluaciones = FiltrarEvaluaciones(db.EvaluacionesControl.AsNoTracking(), rol, riesgoIds);

        return new GrcResumen
        {
            TotalRiesgos = riesgos.Count,
            RiesgosExtremos = riesgos.Count(r => r.NivelResidual == "Extremo"),
            RiesgosAltos = riesgos.Count(r => r.NivelResidual == "Alto"),
            EventosAbiertos = await eventos.CountAsync(e => e.Estado != "Cerrado"),
            PerdidaEstimada = await eventos.SumAsync(e => e.MontoPerdidaEstimado ?? 0),
            ObligacionesPendientes = await obligaciones.CountAsync(o => o.Estado == "Pendiente" || o.Estado == "En proceso"),
            ObligacionesVencidas = await obligaciones.CountAsync(o =>
                o.FechaVencimiento.HasValue && o.FechaVencimiento.Value < hoy && o.Estado != "Cumplida"),
            AuditoriasActivas = await auditorias.CountAsync(a => a.Estado == "Planificada" || a.Estado == "En ejecucion" || a.Estado == "Informe emitido"),
            HallazgosAbiertos = await hallazgos
                .Where(h => h.Estado == "Abierto" || h.Estado == "En remediacion")
                .CountAsync(),
            DocumentosPorRevisar = await documentos.CountAsync(d =>
                d.FechaRevision.HasValue && d.FechaRevision.Value <= hoy.AddDays(30) && d.Estado != "Obsoleto"),
            ActivosCriticos = await activos.CountAsync(a => a.Criticidad == "Alta" || a.Criticidad == "Critica"),
            EvaluacionesPendientes = await evaluaciones.CountAsync(e => e.Resultado == "Pendiente"),
            PlanesAccionAbiertos = await FiltrarPlanes(db.PlanesAccion.AsNoTracking(), rol, riesgoIds)
                .CountAsync(p => p.EstadoPlan != "Cerrado" && p.EstadoPlan != "Completado")
        };
    }

    public async Task<List<Riesgo>> GetRiesgosParaSelectorAsync(int usuarioId, string rol)
    {
        using var db = Db();
        var query = db.Riesgos.AsNoTracking();

        if (!TieneAccesoTotal(rol))
        {
            var matrizIds = await GetMatrizIdsAutorizadasAsync(db, usuarioId, rol);
            query = query.Where(r => r.MatrizGrupoId.HasValue && matrizIds.Contains(r.MatrizGrupoId.Value));
        }

        return await query
            .OrderBy(r => r.CodigoProceso)
            .ThenBy(r => r.CodigoRiesgo)
            .ToListAsync();
    }

    public async Task<List<RiesgoControl>> GetControlesParaSelectorAsync(int usuarioId, string rol)
    {
        using var db = Db();
        var query = db.RiesgosControl
            .Include(c => c.Riesgo)
            .AsNoTracking();

        if (!TieneAccesoTotal(rol))
        {
            var matrizIds = await GetMatrizIdsAutorizadasAsync(db, usuarioId, rol);
            query = query.Where(c => c.Riesgo != null &&
                c.Riesgo.MatrizGrupoId.HasValue &&
                matrizIds.Contains(c.Riesgo.MatrizGrupoId.Value));
        }

        return await query
            .OrderBy(c => c.Riesgo!.CodigoProceso)
            .ThenBy(c => c.CodigoControl)
            .ToListAsync();
    }

    public async Task<List<PlanAccion>> GetPlanesAccionAsync(int usuarioId, string rol)
    {
        using var db = Db();
        var (_, riesgoIds) = await GetAmbitoAutorizadoAsync(db, usuarioId, rol);

        return await FiltrarPlanes(db.PlanesAccion.Include(p => p.Riesgo).AsNoTracking(), rol, riesgoIds)
            .OrderBy(p => p.FinPlan ?? DateTime.MaxValue)
            .ThenBy(p => p.CodigoPlan)
            .ToListAsync();
    }

    public async Task GuardarPlanAccionAsync(PlanAccion plan)
    {
        using var db = Db();
        if (plan.Id == 0 && string.IsNullOrWhiteSpace(plan.CodigoPlan))
        {
            plan.CodigoPlan = await GenerarCodigoAsync(db.PlanesAccion, "PLA");
        }

        if (plan.Id == 0) db.PlanesAccion.Add(plan);
        else db.PlanesAccion.Update(plan);
        await db.SaveChangesAsync();
    }

    public async Task EliminarPlanAccionAsync(int id)
    {
        using var db = Db();
        var item = await db.PlanesAccion.FindAsync(id);
        if (item == null) return;
        db.PlanesAccion.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task<List<EventoRiesgo>> GetEventosAsync(int usuarioId, string rol)
    {
        using var db = Db();
        var (codigosProceso, riesgoIds) = await GetAmbitoAutorizadoAsync(db, usuarioId, rol);

        return await FiltrarEventos(db.EventosRiesgo.Include(e => e.Riesgo).AsNoTracking(), rol, codigosProceso, riesgoIds)
            .OrderByDescending(e => e.FechaEvento)
            .ThenByDescending(e => e.Id)
            .ToListAsync();
    }

    public async Task GuardarEventoAsync(EventoRiesgo evento)
    {
        using var db = Db();
        if (evento.RiesgoId.HasValue && evento.CodigoProceso == "")
        {
            evento.CodigoProceso = await db.Riesgos
                .Where(r => r.Id == evento.RiesgoId.Value)
                .Select(r => r.CodigoProceso)
                .FirstOrDefaultAsync() ?? "";
        }

        if (evento.Id == 0)
        {
            evento.Codigo = await GenerarCodigoAsync(db.EventosRiesgo, "EVT");
            evento.FechaRegistro = DateTime.Now;
            db.EventosRiesgo.Add(evento);
        }
        else
        {
            db.EventosRiesgo.Update(evento);
        }
        await db.SaveChangesAsync();
    }

    public async Task EliminarEventoAsync(int id)
    {
        using var db = Db();
        var item = await db.EventosRiesgo.FindAsync(id);
        if (item == null) return;
        db.EventosRiesgo.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task<List<ObligacionNormativa>> GetObligacionesAsync(int usuarioId, string rol)
    {
        using var db = Db();
        var (codigosProceso, riesgoIds) = await GetAmbitoAutorizadoAsync(db, usuarioId, rol);

        return await FiltrarObligaciones(db.ObligacionesNormativas.Include(o => o.Riesgo).AsNoTracking(), rol, codigosProceso, riesgoIds)
            .OrderBy(o => o.FechaVencimiento ?? DateTime.MaxValue)
            .ThenBy(o => o.Codigo)
            .ToListAsync();
    }

    public async Task GuardarObligacionAsync(ObligacionNormativa obligacion)
    {
        using var db = Db();
        if (obligacion.RiesgoId.HasValue && obligacion.CodigoProceso == "")
        {
            obligacion.CodigoProceso = await db.Riesgos
                .Where(r => r.Id == obligacion.RiesgoId.Value)
                .Select(r => r.CodigoProceso)
                .FirstOrDefaultAsync() ?? "";
        }

        if (obligacion.Id == 0)
        {
            obligacion.Codigo = await GenerarCodigoAsync(db.ObligacionesNormativas, "OBL");
            obligacion.FechaRegistro = DateTime.Now;
            db.ObligacionesNormativas.Add(obligacion);
        }
        else
        {
            db.ObligacionesNormativas.Update(obligacion);
        }
        await db.SaveChangesAsync();
    }

    public async Task EliminarObligacionAsync(int id)
    {
        using var db = Db();
        var item = await db.ObligacionesNormativas.FindAsync(id);
        if (item == null) return;
        db.ObligacionesNormativas.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task<List<AuditoriaGrc>> GetAuditoriasAsync(int usuarioId, string rol)
    {
        using var db = Db();
        var (codigosProceso, _) = await GetAmbitoAutorizadoAsync(db, usuarioId, rol);

        return await FiltrarAuditorias(db.AuditoriasGrc.Include(a => a.Hallazgos).AsNoTracking(), rol, codigosProceso)
            .OrderByDescending(a => a.FechaInicio ?? DateTime.MinValue)
            .ThenBy(a => a.Codigo)
            .ToListAsync();
    }

    public async Task GuardarAuditoriaAsync(AuditoriaGrc auditoria)
    {
        using var db = Db();
        if (auditoria.Id == 0)
        {
            auditoria.Codigo = await GenerarCodigoAsync(db.AuditoriasGrc, "AUD");
            db.AuditoriasGrc.Add(auditoria);
        }
        else
        {
            db.AuditoriasGrc.Update(auditoria);
        }
        await db.SaveChangesAsync();
    }

    public async Task EliminarAuditoriaAsync(int id)
    {
        using var db = Db();
        var item = await db.AuditoriasGrc.FindAsync(id);
        if (item == null) return;
        db.AuditoriasGrc.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task GuardarHallazgoAsync(HallazgoAuditoria hallazgo)
    {
        using var db = Db();
        if (hallazgo.Id == 0)
        {
            hallazgo.Codigo = await GenerarCodigoAsync(db.HallazgosAuditoria, "HAL");
            db.HallazgosAuditoria.Add(hallazgo);
        }
        else
        {
            db.HallazgosAuditoria.Update(hallazgo);
        }
        await db.SaveChangesAsync();
    }

    public async Task EliminarHallazgoAsync(int id)
    {
        using var db = Db();
        var item = await db.HallazgosAuditoria.FindAsync(id);
        if (item == null) return;
        db.HallazgosAuditoria.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task<List<DocumentoGrc>> GetDocumentosAsync(int usuarioId, string rol)
    {
        using var db = Db();
        var (codigosProceso, _) = await GetAmbitoAutorizadoAsync(db, usuarioId, rol);

        return await FiltrarDocumentos(db.DocumentosGrc.AsNoTracking(), rol, codigosProceso)
            .OrderBy(d => d.FechaRevision ?? DateTime.MaxValue)
            .ThenBy(d => d.Codigo)
            .ToListAsync();
    }

    public async Task GuardarDocumentoAsync(DocumentoGrc documento)
    {
        using var db = Db();
        if (documento.Id == 0)
        {
            documento.Codigo = await GenerarCodigoAsync(db.DocumentosGrc, "DOC");
            documento.FechaRegistro = DateTime.Now;
            db.DocumentosGrc.Add(documento);
        }
        else
        {
            db.DocumentosGrc.Update(documento);
        }
        await db.SaveChangesAsync();
    }

    public async Task EliminarDocumentoAsync(int id)
    {
        using var db = Db();
        var item = await db.DocumentosGrc.FindAsync(id);
        if (item == null) return;
        db.DocumentosGrc.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task<List<ActivoInformacion>> GetActivosAsync(int usuarioId, string rol)
    {
        using var db = Db();
        var (codigosProceso, riesgoIds) = await GetAmbitoAutorizadoAsync(db, usuarioId, rol);

        return await FiltrarActivos(db.ActivosInformacion.Include(a => a.Riesgo).AsNoTracking(), rol, codigosProceso, riesgoIds)
            .OrderByDescending(a => a.Criticidad)
            .ThenBy(a => a.Codigo)
            .ToListAsync();
    }

    public async Task GuardarActivoAsync(ActivoInformacion activo)
    {
        using var db = Db();
        if (activo.RiesgoId.HasValue && activo.CodigoProceso == "")
        {
            activo.CodigoProceso = await db.Riesgos
                .Where(r => r.Id == activo.RiesgoId.Value)
                .Select(r => r.CodigoProceso)
                .FirstOrDefaultAsync() ?? "";
        }

        if (activo.Id == 0)
        {
            activo.Codigo = await GenerarCodigoAsync(db.ActivosInformacion, "ACT");
            activo.FechaRegistro = DateTime.Now;
            db.ActivosInformacion.Add(activo);
        }
        else
        {
            db.ActivosInformacion.Update(activo);
        }
        await db.SaveChangesAsync();
    }

    public async Task EliminarActivoAsync(int id)
    {
        using var db = Db();
        var item = await db.ActivosInformacion.FindAsync(id);
        if (item == null) return;
        db.ActivosInformacion.Remove(item);
        await db.SaveChangesAsync();
    }

    public async Task<List<EvaluacionControl>> GetEvaluacionesControlAsync(int usuarioId, string rol)
    {
        using var db = Db();
        var (_, riesgoIds) = await GetAmbitoAutorizadoAsync(db, usuarioId, rol);

        return await FiltrarEvaluaciones(db.EvaluacionesControl
                .Include(e => e.RiesgoControl)
                .ThenInclude(c => c!.Riesgo)
                .AsNoTracking(), rol, riesgoIds)
            .OrderByDescending(e => e.FechaEvaluacion)
            .ThenByDescending(e => e.Id)
            .ToListAsync();
    }

    public async Task GuardarEvaluacionControlAsync(EvaluacionControl evaluacion)
    {
        using var db = Db();
        if (evaluacion.Id == 0) db.EvaluacionesControl.Add(evaluacion);
        else db.EvaluacionesControl.Update(evaluacion);
        await db.SaveChangesAsync();
    }

    public async Task EliminarEvaluacionControlAsync(int id)
    {
        using var db = Db();
        var item = await db.EvaluacionesControl.FindAsync(id);
        if (item == null) return;
        db.EvaluacionesControl.Remove(item);
        await db.SaveChangesAsync();
    }

    private static bool TieneAccesoTotal(string rol) => rol == "SuperAdmin" || rol == "Admin";

    private async Task<List<int>> GetMatrizIdsAutorizadasAsync(AppDbContext db, int usuarioId, string rol)
    {
        if (TieneAccesoTotal(rol))
            return await db.MatrizGrupos.Select(m => m.Id).ToListAsync();

        return await db.UsuarioMatrices
            .Where(um => um.UsuarioId == usuarioId)
            .Select(um => um.MatrizGrupoId)
            .ToListAsync();
    }

    private async Task<(List<string> CodigosProceso, List<int> RiesgoIds)> GetAmbitoAutorizadoAsync(AppDbContext db, int usuarioId, string rol)
    {
        if (TieneAccesoTotal(rol))
        {
            var codigos = await db.MatrizGrupos.Select(m => m.CodigoProceso).ToListAsync();
            var riesgoIds = await db.Riesgos.Select(r => r.Id).ToListAsync();
            return (codigos, riesgoIds);
        }

        var matrizIds = await GetMatrizIdsAutorizadasAsync(db, usuarioId, rol);
        var codigosProceso = await db.MatrizGrupos
            .Where(m => matrizIds.Contains(m.Id))
            .Select(m => m.CodigoProceso)
            .Distinct()
            .ToListAsync();

        var riesgos = await db.Riesgos
            .Where(r => r.MatrizGrupoId.HasValue && matrizIds.Contains(r.MatrizGrupoId.Value))
            .Select(r => r.Id)
            .ToListAsync();

        return (codigosProceso, riesgos);
    }

    private static IQueryable<EventoRiesgo> FiltrarEventos(
        IQueryable<EventoRiesgo> query,
        string rol,
        List<string> codigosProceso,
        List<int> riesgoIds)
    {
        if (TieneAccesoTotal(rol)) return query;
        return query.Where(e =>
            codigosProceso.Contains(e.CodigoProceso) ||
            (e.RiesgoId.HasValue && riesgoIds.Contains(e.RiesgoId.Value)));
    }

    private static IQueryable<ObligacionNormativa> FiltrarObligaciones(
        IQueryable<ObligacionNormativa> query,
        string rol,
        List<string> codigosProceso,
        List<int> riesgoIds)
    {
        if (TieneAccesoTotal(rol)) return query;
        return query.Where(o =>
            codigosProceso.Contains(o.CodigoProceso) ||
            (o.RiesgoId.HasValue && riesgoIds.Contains(o.RiesgoId.Value)));
    }

    private static IQueryable<AuditoriaGrc> FiltrarAuditorias(
        IQueryable<AuditoriaGrc> query,
        string rol,
        List<string> codigosProceso)
    {
        if (TieneAccesoTotal(rol)) return query;
        return query.Where(a => codigosProceso.Contains(a.CodigoProceso));
    }

    private static IQueryable<DocumentoGrc> FiltrarDocumentos(
        IQueryable<DocumentoGrc> query,
        string rol,
        List<string> codigosProceso)
    {
        if (TieneAccesoTotal(rol)) return query;
        return query.Where(d => codigosProceso.Contains(d.CodigoProceso));
    }

    private static IQueryable<HallazgoAuditoria> FiltrarHallazgos(
        IQueryable<HallazgoAuditoria> query,
        string rol,
        List<string> codigosProceso)
    {
        if (TieneAccesoTotal(rol)) return query;
        return query.Where(h => h.AuditoriaGrc != null && codigosProceso.Contains(h.AuditoriaGrc.CodigoProceso));
    }

    private static IQueryable<ActivoInformacion> FiltrarActivos(
        IQueryable<ActivoInformacion> query,
        string rol,
        List<string> codigosProceso,
        List<int> riesgoIds)
    {
        if (TieneAccesoTotal(rol)) return query;
        return query.Where(a =>
            codigosProceso.Contains(a.CodigoProceso) ||
            (a.RiesgoId.HasValue && riesgoIds.Contains(a.RiesgoId.Value)));
    }

    private static IQueryable<EvaluacionControl> FiltrarEvaluaciones(
        IQueryable<EvaluacionControl> query,
        string rol,
        List<int> riesgoIds)
    {
        if (TieneAccesoTotal(rol)) return query;
        return query.Where(e => e.RiesgoControl != null && riesgoIds.Contains(e.RiesgoControl.RiesgoId));
    }

    private static IQueryable<PlanAccion> FiltrarPlanes(
        IQueryable<PlanAccion> query,
        string rol,
        List<int> riesgoIds)
    {
        if (TieneAccesoTotal(rol)) return query;
        return query.Where(p => riesgoIds.Contains(p.RiesgoId));
    }

    private static async Task<string> GenerarCodigoAsync<T>(DbSet<T> set, string prefijo) where T : class
    {
        var siguiente = await set.CountAsync() + 1;
        return $"{prefijo}-{DateTime.Now:yyyyMMdd}-{siguiente:D3}";
    }
}

public class GrcResumen
{
    public int TotalRiesgos { get; set; }
    public int RiesgosExtremos { get; set; }
    public int RiesgosAltos { get; set; }
    public int EventosAbiertos { get; set; }
    public decimal PerdidaEstimada { get; set; }
    public int ObligacionesPendientes { get; set; }
    public int ObligacionesVencidas { get; set; }
    public int AuditoriasActivas { get; set; }
    public int HallazgosAbiertos { get; set; }
    public int DocumentosPorRevisar { get; set; }
    public int ActivosCriticos { get; set; }
    public int EvaluacionesPendientes { get; set; }
    public int PlanesAccionAbiertos { get; set; }
}
