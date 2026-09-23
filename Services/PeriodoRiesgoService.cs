// ============================================================
// Archivo: Services/PeriodoRiesgoService.cs  — VERSIÓN COMPLETA
// Integra la inicialización y cierre de snapshots
// ============================================================
using Microsoft.EntityFrameworkCore;
using RiesgosElor.Data;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

public class PeriodoRiesgoService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly SnapshotService _snapSvc;

    public PeriodoRiesgoService(
        IDbContextFactory<AppDbContext> dbFactory,
        SnapshotService snapSvc)
    {
        _dbFactory = dbFactory;
        _snapSvc = snapSvc;
    }

    // ── Consultas ─────────────────────────────────────────────────────
    public async Task<List<PeriodoRiesgo>> GetTodosAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.PeriodosRiesgo
            .Include(p => p.Bitacora)
            .OrderByDescending(p => p.FechaInicio)
            .ToListAsync();
    }

    public async Task<List<BitacoraPeriodoRiesgo>> GetBitacoraGlobalAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.BitacoraPeriodoRiesgo
            .Include(b => b.Periodo)
            .OrderByDescending(b => b.FechaCambio)
            .Take(200)
            .ToListAsync();
    }

    public async Task<List<BitacoraPeriodoRiesgo>> GetBitacoraAsync(int periodoId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.BitacoraPeriodoRiesgo
            .Where(b => b.PeriodoId == periodoId)
            .OrderByDescending(b => b.FechaCambio)
            .ToListAsync();
    }

    // ── Crear periodo ─────────────────────────────────────────────────
    public async Task CrearAsync(PeriodoRiesgo periodo, string usuario)
    {
        using var db = _dbFactory.CreateDbContext();

        // Solo puede haber un periodo activo
        if (await db.PeriodosRiesgo.AnyAsync(p => p.Estado == "Activo"))
            throw new InvalidOperationException("Ya existe un periodo activo. Ciérralo antes de abrir uno nuevo.");

        periodo.Estado = "Activo";
        periodo.CreadoPor = usuario;
        db.PeriodosRiesgo.Add(periodo);
        await db.SaveChangesAsync();

        // Registrar en bitácora
        db.BitacoraPeriodoRiesgo.Add(new BitacoraPeriodoRiesgo
        {
            PeriodoId = periodo.Id,
            Accion = "Creado",
            CampoModificado = "Estado",
            ValorAnterior = "",
            ValorNuevo = "Activo",
            UsuarioNombre = usuario,
            Motivo = $"Apertura del periodo {periodo.NombrePeriodo}",
            FechaCambio = DateTime.Now
        });
        await db.SaveChangesAsync();

        // ── CREAR SNAPSHOTS EN BLANCO ─────────────────────────────────
        await _snapSvc.InicializarSnapshotPeriodoAsync(periodo.Id, usuario);
    }

    // ── Editar periodo ────────────────────────────────────────────────
    public async Task EditarAsync(PeriodoRiesgo datos, string usuario, string motivo)
    {
        using var db = _dbFactory.CreateDbContext();
        var p = await db.PeriodosRiesgo.FindAsync(datos.Id)
                ?? throw new Exception("Periodo no encontrado.");

        var cambios = new List<(string campo, string antes, string despues)>();

        if (p.NombrePeriodo != datos.NombrePeriodo)
            cambios.Add(("NombrePeriodo", p.NombrePeriodo, datos.NombrePeriodo));
        if (p.FechaInicio != datos.FechaInicio)
            cambios.Add(("FechaInicio",
                p.FechaInicio.ToString("dd/MM/yyyy"),
                datos.FechaInicio.ToString("dd/MM/yyyy")));
        if (p.FechaCierre != datos.FechaCierre)
            cambios.Add(("FechaCierre",
                p.FechaCierre.ToString("dd/MM/yyyy"),
                datos.FechaCierre.ToString("dd/MM/yyyy")));
        if (p.Descripcion != datos.Descripcion)
            cambios.Add(("Descripcion", p.Descripcion ?? "", datos.Descripcion ?? ""));

        p.NombrePeriodo = datos.NombrePeriodo;
        p.Tipo = datos.Tipo;
        p.Frecuencia = datos.Frecuencia;
        p.FechaInicio = datos.FechaInicio;
        p.FechaCierre = datos.FechaCierre;
        p.Descripcion = datos.Descripcion;

        foreach (var (campo, antes, despues) in cambios)
        {
            db.BitacoraPeriodoRiesgo.Add(new BitacoraPeriodoRiesgo
            {
                PeriodoId = p.Id,
                Accion = "Editado",
                CampoModificado = campo,
                ValorAnterior = antes,
                ValorNuevo = despues,
                UsuarioNombre = usuario,
                Motivo = motivo,
                FechaCambio = DateTime.Now
            });
        }

        await db.SaveChangesAsync();
    }

    // ── Cerrar periodo ────────────────────────────────────────────────
    public async Task CerrarAsync(int periodoId, string usuario, string motivo)
    {
        using var db = _dbFactory.CreateDbContext();
        var p = await db.PeriodosRiesgo.FindAsync(periodoId)
                ?? throw new Exception("Periodo no encontrado.");

        p.Estado = "Cerrado";
        p.CerradoPor = usuario;
        p.FechaCierreReal = DateTime.Now;

        db.BitacoraPeriodoRiesgo.Add(new BitacoraPeriodoRiesgo
        {
            PeriodoId = p.Id,
            Accion = "Cerrado",
            CampoModificado = "Estado",
            ValorAnterior = "Activo",
            ValorNuevo = "Cerrado",
            UsuarioNombre = usuario,
            Motivo = motivo,
            FechaCambio = DateTime.Now
        });

        await db.SaveChangesAsync();

        // ── CONGELAR SNAPSHOTS ────────────────────────────────────────
        await _snapSvc.CerrarSnapshotsPeriodoAsync(periodoId);
    }

    // ── Eliminar (soft delete) ─────────────────────────────────────────
    public async Task EliminarAsync(int periodoId, string usuario, string motivo)
    {
        using var db = _dbFactory.CreateDbContext();
        var p = await db.PeriodosRiesgo.FindAsync(periodoId)
                ?? throw new Exception("Periodo no encontrado.");

        p.Estado = "Eliminado";

        db.BitacoraPeriodoRiesgo.Add(new BitacoraPeriodoRiesgo
        {
            PeriodoId = p.Id,
            Accion = "Eliminado",
            CampoModificado = "Estado",
            ValorAnterior = p.Estado,
            ValorNuevo = "Eliminado",
            UsuarioNombre = usuario,
            Motivo = motivo,
            FechaCambio = DateTime.Now
        });

        await db.SaveChangesAsync();
    }
}