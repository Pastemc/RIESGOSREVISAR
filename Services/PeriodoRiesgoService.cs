using Microsoft.EntityFrameworkCore;
using RiesgosElor.Data;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

public class PeriodoRiesgoService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public PeriodoRiesgoService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    private AppDbContext Db() => _factory.CreateDbContext();

    // ── CONSULTAS ─────────────────────────────────────────────────────────

    public async Task<List<PeriodoRiesgo>> GetTodosAsync()
    {
        using var db = Db();
        return await db.PeriodosRiesgo
            .Include(p => p.Bitacora.OrderByDescending(b => b.FechaCambio))
            .OrderByDescending(p => p.FechaInicio)
            .ToListAsync();
    }

    public async Task<PeriodoRiesgo?> GetActivoAsync()
    {
        using var db = Db();
        return await db.PeriodosRiesgo
            .Include(p => p.Bitacora.OrderByDescending(b => b.FechaCambio))
            .FirstOrDefaultAsync(p => p.Estado == "Activo");
    }

    public async Task<PeriodoRiesgo?> GetByIdAsync(int id)
    {
        using var db = Db();
        return await db.PeriodosRiesgo
            .Include(p => p.Bitacora.OrderByDescending(b => b.FechaCambio))
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<BitacoraPeriodoRiesgo>> GetBitacoraAsync(int periodoId)
    {
        using var db = Db();
        return await db.BitacoraPeriodoRiesgo
            .Where(b => b.PeriodoId == periodoId)
            .OrderByDescending(b => b.FechaCambio)
            .ToListAsync();
    }

    public async Task<List<BitacoraPeriodoRiesgo>> GetBitacoraGlobalAsync()
    {
        using var db = Db();
        return await db.BitacoraPeriodoRiesgo
            .Include(b => b.Periodo)
            .OrderByDescending(b => b.FechaCambio)
            .ToListAsync();
    }

    // ── CREAR ─────────────────────────────────────────────────────────────

    public async Task<PeriodoRiesgo> CrearAsync(PeriodoRiesgo periodo, string usuario)
    {
        using var db = Db();

        // Cerrar cualquier periodo activo primero (seguridad)
        var activo = await db.PeriodosRiesgo.FirstOrDefaultAsync(p => p.Estado == "Activo");
        if (activo != null)
            throw new InvalidOperationException("Ya existe un periodo activo. Ciérralo antes de crear uno nuevo.");

        periodo.Estado       = "Activo";
        periodo.CreadoPor    = usuario;
        periodo.FechaCreacion= DateTime.Now;

        db.PeriodosRiesgo.Add(periodo);
        await db.SaveChangesAsync();

        // Bitácora — Creado
        db.BitacoraPeriodoRiesgo.Add(new BitacoraPeriodoRiesgo
        {
            PeriodoId     = periodo.Id,
            Accion        = "Creado",
            UsuarioNombre = usuario,
            Motivo        = $"Periodo '{periodo.NombrePeriodo}' aperturado. Frecuencia: {periodo.Frecuencia}.",
            FechaCambio   = DateTime.Now
        });
        await db.SaveChangesAsync();

        return periodo;
    }

    // ── EDITAR ────────────────────────────────────────────────────────────

    public async Task EditarAsync(PeriodoRiesgo nuevo, string usuario, string motivo)
    {
        using var db = Db();
        var actual = await db.PeriodosRiesgo.FindAsync(nuevo.Id)
            ?? throw new KeyNotFoundException("Periodo no encontrado.");

        // Registrar cambios campo a campo
        var cambios = new List<(string campo, string antes, string despues)>();

        if (actual.NombrePeriodo != nuevo.NombrePeriodo)
            cambios.Add(("NombrePeriodo", actual.NombrePeriodo, nuevo.NombrePeriodo));
        if (actual.Tipo != nuevo.Tipo)
            cambios.Add(("Tipo", actual.Tipo, nuevo.Tipo));
        if (actual.Frecuencia != nuevo.Frecuencia)
            cambios.Add(("Frecuencia", actual.Frecuencia, nuevo.Frecuencia));
        if (actual.FechaInicio != nuevo.FechaInicio)
            cambios.Add(("FechaInicio", actual.FechaInicio.ToString("dd/MM/yyyy"), nuevo.FechaInicio.ToString("dd/MM/yyyy")));
        if (actual.FechaCierre != nuevo.FechaCierre)
            cambios.Add(("FechaCierre", actual.FechaCierre.ToString("dd/MM/yyyy"), nuevo.FechaCierre.ToString("dd/MM/yyyy")));
        if (actual.Descripcion != nuevo.Descripcion)
            cambios.Add(("Descripcion", actual.Descripcion, nuevo.Descripcion));

        // Aplicar cambios
        actual.NombrePeriodo = nuevo.NombrePeriodo;
        actual.Tipo          = nuevo.Tipo;
        actual.Frecuencia    = nuevo.Frecuencia;
        actual.FechaInicio   = nuevo.FechaInicio;
        actual.FechaCierre   = nuevo.FechaCierre;
        actual.Descripcion   = nuevo.Descripcion;

        await db.SaveChangesAsync();

        // Bitácora — una entrada por campo modificado
        foreach (var (campo, antes, despues) in cambios)
        {
            db.BitacoraPeriodoRiesgo.Add(new BitacoraPeriodoRiesgo
            {
                PeriodoId       = actual.Id,
                Accion          = "Editado",
                CampoModificado = campo,
                ValorAnterior   = antes,
                ValorNuevo      = despues,
                UsuarioNombre   = usuario,
                Motivo          = motivo,
                FechaCambio     = DateTime.Now
            });
        }

        if (!cambios.Any())
        {
            db.BitacoraPeriodoRiesgo.Add(new BitacoraPeriodoRiesgo
            {
                PeriodoId     = actual.Id,
                Accion        = "Editado",
                UsuarioNombre = usuario,
                Motivo        = "Sin cambios detectados. " + motivo,
                FechaCambio   = DateTime.Now
            });
        }

        await db.SaveChangesAsync();
    }

    // ── CERRAR ────────────────────────────────────────────────────────────

    public async Task CerrarAsync(int id, string usuario, string motivo)
    {
        using var db = Db();
        var p = await db.PeriodosRiesgo.FindAsync(id)
            ?? throw new KeyNotFoundException("Periodo no encontrado.");

        p.Estado          = "Cerrado";
        p.CerradoPor      = usuario;
        p.FechaCierreReal = DateTime.Now;

        db.BitacoraPeriodoRiesgo.Add(new BitacoraPeriodoRiesgo
        {
            PeriodoId     = p.Id,
            Accion        = "Cerrado",
            ValorAnterior = "Activo",
            ValorNuevo    = "Cerrado",
            UsuarioNombre = usuario,
            Motivo        = string.IsNullOrWhiteSpace(motivo) ? "Cierre de periodo." : motivo,
            FechaCambio   = DateTime.Now
        });

        await db.SaveChangesAsync();
    }

    // ── ELIMINAR (lógico — queda en historial) ────────────────────────────

    public async Task EliminarAsync(int id, string usuario, string motivo)
    {
        using var db = Db();
        var p = await db.PeriodosRiesgo.FindAsync(id)
            ?? throw new KeyNotFoundException("Periodo no encontrado.");

        if (p.Estado == "Activo")
            throw new InvalidOperationException("No se puede eliminar un periodo activo. Ciérralo primero.");

        // Registrar en bitácora ANTES de eliminar
        db.BitacoraPeriodoRiesgo.Add(new BitacoraPeriodoRiesgo
        {
            PeriodoId     = p.Id,
            Accion        = "Eliminado",
            ValorAnterior = p.NombrePeriodo,
            ValorNuevo    = "ELIMINADO",
            UsuarioNombre = usuario,
            Motivo        = string.IsNullOrWhiteSpace(motivo) ? "Eliminación manual." : motivo,
            FechaCambio   = DateTime.Now
        });
        await db.SaveChangesAsync();

        // Eliminar periodo (la bitácora se mantiene por CASCADE DELETE desactivado en este caso)
        // Para mantener historial completo, marcamos como "Eliminado" en lugar de borrar
        p.Estado     = "Eliminado";
        p.CerradoPor = usuario;
        await db.SaveChangesAsync();
    }
}
