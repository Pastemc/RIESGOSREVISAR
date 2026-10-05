// ============================================================
// Archivo: Services/PeriodoRiesgoService.cs
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

    // ── Consultas ─────────────────────────────────────────────────────────
    public async Task<List<PeriodoRiesgo>> GetTodosAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.PeriodosRiesgo
            .Include(p => p.Bitacora)
            .OrderByDescending(p => p.AnioRef)
            .ThenByDescending(p => p.NumeroPeriodo)
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

    // ── Calcular próximo NumeroPeriodo y Version ───────────────────────────
    // Usado solo para previsualizar en el modal de apertura
    public async Task<(int numero, int version)> CalcularNumeroVersionAsync(
        int anio, string nombreBase, bool tuvoModificaciones)
    {
        using var db = _dbFactory.CreateDbContext();

        // Último periodo de Apertura cerrado en ese año
        var ultimoCerrado = await db.PeriodosRiesgo
            .Where(p => p.AnioRef == anio
                     && p.TipoPeriodo == "Apertura"
                     && p.Estado == "Cerrado")
            .OrderByDescending(p => p.NumeroPeriodo)
            .ThenByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        // Si no hay ningún cerrado, buscar entre activos
        if (ultimoCerrado == null)
        {
            var ultimoActivo = await db.PeriodosRiesgo
                .Where(p => p.AnioRef == anio
                         && p.TipoPeriodo == "Apertura"
                         && p.Estado != "Eliminado")
                .OrderByDescending(p => p.NumeroPeriodo)
                .FirstOrDefaultAsync();

            int num = (ultimoActivo?.NumeroPeriodo ?? 0) + 1;
            int ver = 1;
            return (num, ver);
        }

        int numero = ultimoCerrado.NumeroPeriodo + 1;
        // La versión sube si el último periodo cerrado tuvo modificaciones
        int version = ultimoCerrado.TuvoModificaciones
            ? ultimoCerrado.Version + 1
            : ultimoCerrado.Version;

        return (numero, version);
    }

    // ── Obtener ultimo periodo de Apertura cerrado (para Validacion) ─────
    public async Task<PeriodoRiesgo?> GetUltimoAperturaCerradoAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.PeriodosRiesgo
            .Where(p => p.TipoPeriodo == "Apertura" && p.Estado == "Cerrado")
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();
    }

    // ── Crear periodo ─────────────────────────────────────────────────────
    // REGLAS:
    //   Apertura   → crea snapshot propio, calcula NumeroPeriodo y Version
    //   Validacion → NO crea snapshot, hereda P-Num y Version del ultimo Apertura cerrado
    //   Evidencia  → NO crea snapshot (implementacion futura)
    //   Solo puede haber UN periodo activo en total (cualquier tipo)
    public async Task CrearAsync(PeriodoRiesgo periodo, string usuario)
    {
        using var db = _dbFactory.CreateDbContext();

        // Regla: solo un periodo activo en todo el sistema
        var hayActivo = await db.PeriodosRiesgo
            .AnyAsync(p => p.Estado == "Activo");
        if (hayActivo)
            throw new InvalidOperationException(
                "Ya existe un periodo activo. Ciérralo antes de abrir uno nuevo.");

        periodo.Estado = "Activo";
        periodo.CreadoPor = usuario;
        periodo.AnioRef = periodo.FechaInicio.Year;
        periodo.TuvoModificaciones = false;
        periodo.NumeroPeriodo = 0;
        periodo.Version = 1;
        periodo.PeriodoAperturaRefId = null;

        // ── VALIDACION: hereda datos del ultimo Apertura cerrado ──────────
        bool esValidacion = periodo.TipoPeriodo == "Validación" || periodo.TipoPeriodo == "Validacion";
        if (esValidacion)
        {
            var ultimoApertura = await db.PeriodosRiesgo
                .Where(p => p.TipoPeriodo == "Apertura" && p.Estado == "Cerrado")
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            if (ultimoApertura == null)
                throw new InvalidOperationException(
                    "No existe un periodo de Apertura cerrado. " +
                    "Debes completar y cerrar un periodo de Apertura antes de abrir uno de Validación.");

            // El periodo de Validacion muestra el mismo P-Num y Version del Apertura
            periodo.NumeroPeriodo = ultimoApertura.NumeroPeriodo;
            periodo.Version = ultimoApertura.Version;
            periodo.PeriodoAperturaRefId = ultimoApertura.Id;
        }

        db.PeriodosRiesgo.Add(periodo);
        await db.SaveChangesAsync();

        // Bitacora
        db.BitacoraPeriodoRiesgo.Add(new BitacoraPeriodoRiesgo
        {
            PeriodoId = periodo.Id,
            Accion = "Creado",
            CampoModificado = "Estado",
            ValorAnterior = "",
            ValorNuevo = "Activo",
            UsuarioNombre = usuario,
            Motivo = $"Apertura del periodo {periodo.NombrePeriodo} — {periodo.TipoPeriodo}",
            FechaCambio = DateTime.Now
        });
        await db.SaveChangesAsync();

        // Solo Apertura inicializa snapshot y calcula NumeroPeriodo/Version
        // Validacion y Evidencia leen el snapshot del Apertura referenciado (PeriodoAperturaRefId)
        if (periodo.TipoPeriodo == "Apertura")
        {
            await _snapSvc.InicializarSnapshotPeriodoAsync(periodo.Id, usuario);

            // Actualizar bitacora con la version real (calculada por InicializarSnapshot)
            var periodoActualizado = await db.PeriodosRiesgo.FindAsync(periodo.Id);
            if (periodoActualizado != null)
            {
                var ultimaBitacora = await db.BitacoraPeriodoRiesgo
                    .Where(b => b.PeriodoId == periodo.Id && b.Accion == "Creado")
                    .OrderByDescending(b => b.FechaCambio)
                    .FirstOrDefaultAsync();
                if (ultimaBitacora != null)
                {
                    ultimaBitacora.Motivo =
                        $"Apertura del periodo {periodoActualizado.NombrePeriodo} " +
                        $"— {periodoActualizado.TipoPeriodo} " +
                        $"— P-{periodoActualizado.NumeroPeriodo} V{periodoActualizado.Version}";
                    await db.SaveChangesAsync();
                }
            }
        }
    }

    // ── Editar periodo ────────────────────────────────────────────────────
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

    // ── Cerrar periodo ────────────────────────────────────────────────────
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
            Motivo = $"{motivo} | TuvoModificaciones={p.TuvoModificaciones} | V{p.Version}",
            FechaCambio = DateTime.Now
        });

        await db.SaveChangesAsync();

        // Congelar snapshots solo si es periodo de Apertura
        if (p.TipoPeriodo == "Apertura")
            await _snapSvc.CerrarSnapshotsPeriodoAsync(periodoId);
    }

    // ── Eliminar (soft delete) ─────────────────────────────────────────────
    public async Task EliminarAsync(int periodoId, string usuario, string motivo)
    {
        using var db = _dbFactory.CreateDbContext();
        var p = await db.PeriodosRiesgo.FindAsync(periodoId)
                ?? throw new Exception("Periodo no encontrado.");

        var estadoAnterior = p.Estado;
        p.Estado = "Eliminado";

        db.BitacoraPeriodoRiesgo.Add(new BitacoraPeriodoRiesgo
        {
            PeriodoId = p.Id,
            Accion = "Eliminado",
            CampoModificado = "Estado",
            ValorAnterior = estadoAnterior,
            ValorNuevo = "Eliminado",
            UsuarioNombre = usuario,
            Motivo = motivo,
            FechaCambio = DateTime.Now
        });

        await db.SaveChangesAsync();
    }
}