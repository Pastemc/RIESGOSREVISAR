using Microsoft.EntityFrameworkCore;
using RiesgosElor.Data;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

// ── DTO para el resumen (no es modelo EF, solo transporte) ──────────────────
public class RiesgoResumenItem
{
    public string CodigoProceso { get; set; } = "";
    public string NombreProceso { get; set; } = "";
    public string CodigoRiesgo { get; set; } = "";
    public string NivelResidual { get; set; } = "";
    public int CantIndicadores { get; set; }
}

public class RiesgoService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public RiesgoService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    private AppDbContext Db() => _factory.CreateDbContext();

    // ─── PROCESOS ───────────────────────────────────────────
    public static readonly Dictionary<string, string> Procesos = new()
    {
        {"E1.1","Administración del Sistema Integrado de Gestión"},
        {"E1.2","Sistemas de Gestión"},
        {"E2.1","Gobierno Corporativo"},
        {"E2.2","Sistema de Control Interno"},
        {"E2.3","Gestión de Riesgos"},
        {"E2.4","Gestión de Cumplimiento"},
        {"E3.1","Planeamiento Institucional"},
        {"E3.2","Planificación Eléctrica"},
        {"E3.3","Gestión Presupuestal"},
        {"E4.1","Tarifas"},
        {"E4.2","Estudios Regulatorios"},
        {"E5.1","Gestión de Comunicaciones"},
        {"E5.2","Responsabilidad Social"},
        {"O1.1","Compra de Energía"},
        {"O1.2","Facturación y Cobranza"},
        {"O1.3","Atención al Cliente"},
        {"O2.1","Gestión de la Operación"},
        {"O2.2","Gestión del Mantenimiento"},
        {"O3.1","Detección de las Deficiencias"},
        {"O3.2","Mejoras Técnicas"},
        {"O4.1","Gestión de Iniciativas"},
        {"O4.2","Desarrollo de Proyectos"},
        {"S1.1","Solución de Controversias"},
        {"S1.2","Gestión del Directorio"},
        {"S1.3","Soporte Legal"},
        {"S2.1","Gestión Contable"},
        {"S2.2","Gestión Financiera"},
        {"S2.3","Gestión de Activos Fijos"},
        {"S3.1","Gestión de Desarrollo y Mantenimiento de Software"},
        {"S3.2","Gestión de la Infraestructura Tecnológica"},
        {"S4.1","Gestión de las Contrataciones"},
        {"S4.2","Gestión de Almacenes"},
        {"S5.1","Gestión del Personal"},
        {"S5.2","Desarrollo del Personal"},
        {"S5.3","Gestión de la Compensación"},
        {"S5.4","Gestión del Bienestar"},
        {"S6.1","Gestión del Trámite Documentario"},
        {"S6.2","Gestión de Archivo"}
    };

    public static readonly List<string> Areas = new()
    {
        "--- GERENCIAS ---",
        "Gerencia General",
        "Gerencia de Planeamiento, Gestión y Regulación",
        "Gerencia Comercial",
        "Gerencia de Administración y Finanzas",
        "Gerencia de Proyectos",
        "Gerencia Regional San Martín",
        "Gerencia Regional Amazonas Cajamarca",
        "Gerencias Regionales",
        "--- OFICINAS ---",
        "Oficina de Calidad y Fiscalización",
        "Oficina de Imagen Institucional y Responsabilidad Social",
        "Oficina de Imagen Institucional",
        "Oficina de Asesoría Legal",
        "--- DIRECTORIO ---",
        "Directorio",
        "--- DEPARTAMENTOS ---",
        "Dpto. de Planeamiento y Regulación",
        "Dpto. de Atención al Cliente",
        "Dpto. de Operaciones Comerciales",
        "Dpto. Comercial de las Sedes (SM y AC)",
        "Dpto. de Finanzas",
        "Dpto. de TIC",
        "Dpto. de Administración de la Sede San Martín",
        "Dpto. de Administración de las Sedes",
        "Dpto. de Distribución de las Sedes",
        "Dpto. de Generación",
        "Dpto. de Generación y Transmisión de las Sedes (SM y AC)",
        "Dpto. de Generación y Transmisión de la Sede San Martín",
        "Dpto. de Control de Pérdidas",
        "Dpto. de Contabilidad",
        "Dpto. de Control Patrimonial y Seguros",
        "Dpto. de Logística",
        "Dpto. de Recursos Humanos",
        "--- OTROS ---",
        "Supervisión de Imagen Institucional",
        "Cada Área Usuaria"
    };

    // ─── CÓDIGO ──────────────────────────────────────────────
    public async Task<string> GenerarCodigoRiesgoAsync(string codigoProceso)
    {
        using var db = Db();
        var count = await db.Riesgos.CountAsync(r => r.CodigoProceso == codigoProceso);
        return $"{codigoProceso}.R{(count + 1):D2}";
    }

    public async Task<int> ContarControlesExistentesAsync(string codigoProceso, int riesgoIdActual)
    {
        using var db = Db();
        var riesgoIds = await db.Riesgos
            .Where(r => r.CodigoProceso == codigoProceso && r.Id != riesgoIdActual)
            .Select(r => r.Id)
            .ToListAsync();
        return await db.RiesgosControl
            .CountAsync(c => riesgoIds.Contains(c.RiesgoId));
    }

    // ─── RIESGOS ─────────────────────────────────────────────
    public async Task<List<Riesgo>> GetTodosAsync()
    {
        using var db = Db();
        return await db.Riesgos
            .Include(r => r.Controles.OrderBy(c => c.Id))
            .Include(r => r.PlanesAccion)
            .Include(r => r.Indicadores)
            .OrderBy(r => r.FechaCreacion)
            .ToListAsync();
    }

    public async Task<Riesgo?> GetByIdAsync(int id)
    {
        using var db = Db();
        return await db.Riesgos
            .Include(r => r.Controles)
            .Include(r => r.PlanesAccion)
            .Include(r => r.Indicadores)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<int> GuardarRiesgoAsync(Riesgo riesgo)
    {
        using var db = Db();
        if (riesgo.Id == 0)
            db.Riesgos.Add(riesgo);
        else
            db.Riesgos.Update(riesgo);
        await db.SaveChangesAsync();
        return riesgo.Id;
    }

    public async Task EliminarAsync(int id)
    {
        using var db = Db();
        var r = await db.Riesgos.FindAsync(id);
        if (r != null) { db.Riesgos.Remove(r); await db.SaveChangesAsync(); }
    }

    // ─── CONTROLES ───────────────────────────────────────────
    public async Task GuardarControlAsync(RiesgoControl ctrl)
    {
        using var db = Db();
        if (ctrl.Id == 0) db.RiesgosControl.Add(ctrl);
        else db.RiesgosControl.Update(ctrl);
        await db.SaveChangesAsync();
    }

    public async Task EliminarControlAsync(int id)
    {
        using var db = Db();
        var c = await db.RiesgosControl.FindAsync(id);
        if (c != null) { db.RiesgosControl.Remove(c); await db.SaveChangesAsync(); }
    }

    // ─── PLANES ──────────────────────────────────────────────
    public async Task GuardarPlanAsync(PlanAccion plan)
    {
        using var db = Db();
        if (plan.Id == 0) db.PlanesAccion.Add(plan);
        else db.PlanesAccion.Update(plan);
        await db.SaveChangesAsync();
    }

    public async Task EliminarPlanAsync(int id)
    {
        using var db = Db();
        var p = await db.PlanesAccion.FindAsync(id);
        if (p != null) { db.PlanesAccion.Remove(p); await db.SaveChangesAsync(); }
    }

    // ─── INDICADORES ─────────────────────────────────────────
    public async Task GuardarIndicadorAsync(Indicador ind)
    {
        using var db = Db();
        if (ind.Id == 0) db.Indicadores.Add(ind);
        else db.Indicadores.Update(ind);
        await db.SaveChangesAsync();
    }

    public async Task EliminarIndicadorAsync(int id)
    {
        using var db = Db();
        var i = await db.Indicadores.FindAsync(id);
        if (i != null) { db.Indicadores.Remove(i); await db.SaveChangesAsync(); }
    }

    // ─── MATRICES ────────────────────────────────────────────
    public async Task<List<MatrizGrupo>> GetMatricesAsync()
    {
        using var db = Db();
        return await db.MatrizGrupos.OrderBy(m => m.Numero).ToListAsync();
    }

    public async Task<MatrizGrupo?> GetMatrizByIdAsync(int id)
    {
        using var db = Db();
        return await db.MatrizGrupos.FindAsync(id);
    }

    public async Task GuardarMatrizGrupoAsync(MatrizGrupo m)
    {
        using var db = Db();
        db.MatrizGrupos.Update(m);
        await db.SaveChangesAsync();
    }

    public async Task<List<Riesgo>> GetRiesgosPorMatrizAsync(int matrizId)
    {
        using var db = Db();
        return await db.Riesgos
            .Include(r => r.Controles.OrderBy(c => c.Id))
            .Include(r => r.PlanesAccion)
            .Include(r => r.Indicadores)
            .Where(r => r.MatrizGrupoId == matrizId)
            .OrderBy(r => r.FechaCreacion)
            .ToListAsync();
    }

    public async Task<List<MatrizGrupo>> GetMatricesPorUsuarioAsync(int usuarioId, string rol)
    {
        using var db = Db();
        if (rol == "SuperAdmin" || rol == "Admin")
            return await db.MatrizGrupos.OrderBy(m => m.Numero).ToListAsync();

        var idsAsignadas = await db.UsuarioMatrices
            .Where(um => um.UsuarioId == usuarioId)
            .Select(um => um.MatrizGrupoId)
            .ToListAsync();

        return await db.MatrizGrupos
            .Where(m => idsAsignadas.Contains(m.Id))
            .OrderBy(m => m.Numero)
            .ToListAsync();
    }

    public async Task SeedMatricesAsync()
    {
        using var db = Db();
        var maxNumero = await db.MatrizGrupos.AnyAsync()
            ? await db.MatrizGrupos.MaxAsync(m => m.Numero)
            : 0;
        var numero = maxNumero + 1;

        foreach (var kv in Procesos)
        {
            var existe = await db.MatrizGrupos.AnyAsync(m => m.CodigoProceso == kv.Key);
            if (existe) continue;

            db.MatrizGrupos.Add(new MatrizGrupo
            {
                Numero = numero++,
                CodigoProceso = kv.Key,
                NombreProceso = kv.Value,
                Nombre = $"MRC - {kv.Key}",
                CodigoMatriz = $"MRC - {kv.Key}",
                FechaAprobacion = "",
                Fecha = "",
                Cerrada = false,
                FechaCreacion = DateTime.Now
            });
        }
        await db.SaveChangesAsync();
    }

    public static string GetColorNivel(string nivel) => nivel switch
    {
        "Bajo" => "#28a745",
        "Moderado" => "#ffc107",
        "Alto" => "#fd7e14",
        "Extremo" => "#dc3545",
        _ => "#6c757d"
    };

    public static async Task EnsureDatabaseTablesCreatedAsync(AppDbContext db)
    {
        try
        {
            string sqlScript = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BitacoraDepartamentoGerencias')
BEGIN
    CREATE TABLE BitacoraDepartamentoGerencias (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        DepartamentoId INT NOT NULL,
        NombreDepartamento NVARCHAR(MAX) NOT NULL,
        GerenciaAnteriorId INT NULL,
        GerenciaAnteriorNombre NVARCHAR(MAX) NOT NULL,
        GerenciaNuevaId INT NULL,
        GerenciaNuevaNombre NVARCHAR(MAX) NOT NULL,
        Periodo NVARCHAR(MAX) NOT NULL,
        FechaCambio DATETIME2 NOT NULL,
        UsuarioNombre NVARCHAR(MAX) NOT NULL,
        Motivo NVARCHAR(MAX) NOT NULL
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MaestroProcesos')
BEGIN
    CREATE TABLE MaestroProcesos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Codigo NVARCHAR(MAX) NOT NULL,
        Nombre NVARCHAR(MAX) NOT NULL,
        Macroproceso NVARCHAR(MAX) NOT NULL,
        Nivel NVARCHAR(MAX) NOT NULL,
        AreaResponsableDefault NVARCHAR(MAX) NOT NULL,
        PadreId INT NULL,
        AreaResponsableId INT NULL,
        ResponsableId INT NULL,
        Objetivo NVARCHAR(MAX) NOT NULL,
        Descripcion NVARCHAR(MAX) NOT NULL,
        Activo BIT NOT NULL,
        Orden INT NOT NULL
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MaestroAreas')
BEGIN
    CREATE TABLE MaestroAreas (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Categoria NVARCHAR(MAX) NOT NULL,
        Nombre NVARCHAR(MAX) NOT NULL,
        Codigo NVARCHAR(MAX) NOT NULL,
        PadreId INT NULL,
        Activo BIT NOT NULL,
        Orden INT NOT NULL
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MaestroTiposRiesgo')
BEGIN
    CREATE TABLE MaestroTiposRiesgo (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Categoria NVARCHAR(MAX) NOT NULL,
        Nombre NVARCHAR(MAX) NOT NULL,
        Descripcion NVARCHAR(MAX) NOT NULL,
        Activo BIT NOT NULL,
        Orden INT NOT NULL
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MaestroParametros')
BEGIN
    CREATE TABLE MaestroParametros (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Grupo NVARCHAR(MAX) NOT NULL,
        Valor NVARCHAR(MAX) NOT NULL,
        Descripcion NVARCHAR(MAX) NOT NULL,
        Activo BIT NOT NULL,
        Orden INT NOT NULL
    );
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MaestroResponsables')
BEGIN
    CREATE TABLE MaestroResponsables (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(MAX) NOT NULL,
        Cargo NVARCHAR(MAX) NOT NULL,
        AreaId INT NULL,
        Activo BIT NOT NULL,
        Orden INT NOT NULL
    );
END
";
            await db.Database.ExecuteSqlRawAsync(sqlScript);
        }
        catch
        {
            // Ignorar en caso de que ya existan o manejado por EF
        }
    }

    // ─── TABLAS MAESTRAS (GRC / PIRANI) ─────────────────────
    public async Task EnsureSeedMasterDataAsync()
    {
        using var db = Db();
        await EnsureDatabaseTablesCreatedAsync(db);

        if (!await db.MaestroProcesos.AnyAsync())
        {
            int orden = 1;
            foreach (var kv in Procesos)
            {
                db.MaestroProcesos.Add(new MaestroProceso
                {
                    Codigo = kv.Key,
                    Nombre = kv.Value,
                    Nivel = "Proceso",
                    Macroproceso = kv.Key.StartsWith("E") ? "ESTRATÉGICO"
                                 : kv.Key.StartsWith("O") ? "OPERATIVO"
                                 : "SOPORTE",
                    Activo = true,
                    Orden = orden++
                });
            }
            await db.SaveChangesAsync();
        }

        if (!await db.MaestroAreas.AnyAsync())
        {
            int orden = 1;
            string catActual = "DEPARTAMENTOS";
            foreach (var a in Areas)
            {
                if (a.StartsWith("---"))
                {
                    catActual = NormalizarCategoriaArea(a.Replace("---", "").Trim());
                    continue;
                }
                db.MaestroAreas.Add(new MaestroArea
                {
                    Categoria = catActual,
                    Nombre = a,
                    Activo = true,
                    Orden = orden++
                });
            }
            await db.SaveChangesAsync();
        }
        else
        {
            var areas = await db.MaestroAreas.ToListAsync();
            foreach (var area in areas)
            {
                area.Categoria = NormalizarCategoriaArea(area.Categoria);
                if (area.Categoria == "DIRECTORIO") area.PadreId = null;
            }
            await db.SaveChangesAsync();
        }

        if (!await db.MaestroResponsables.AnyAsync())
        {
            var areaGpr = await db.MaestroAreas
                .OrderBy(a => a.Orden)
                .FirstOrDefaultAsync(a => a.Nombre.Contains("Planeamiento"));

            db.MaestroResponsables.Add(new MaestroResponsable
            {
                Nombre = "Responsable de la Gestion Integral de Riesgos",
                Cargo = "GIR",
                AreaId = areaGpr?.Id,
                Activo = true,
                Orden = 1
            });
            await db.SaveChangesAsync();
        }

        if (!await db.MaestroTiposRiesgo.AnyAsync())
        {
            var tiposDef = new List<(string Cat, string Nom, string Desc)>
            {
                ("Operacional",   "Falla de Proceso Interno",                    "Riesgos por deficiencias en procesos operacionales"),
                ("Operacional",   "Falla Humana / Negligencia",                  "Riesgo derivado de error humano"),
                ("Tecnológico",   "Falla de Infraestructura TI / Ciberseguridad","Interrupciones en servidores, redes o software"),
                ("Tecnológico",   "Pérdida o Alteración de Datos",               "Pérdida de confidencialidad o integridad de datos"),
                ("Financiero",    "Fraude Interno / Malversación",               "Pérdida financiera por fraude"),
                ("Cumplimiento",  "Incumplimiento Regulador (OSINERGMIN / OEFA)","Sanciones por incumplimiento de normativas"),
                ("Estratégico",   "Deficiencia en Planeamiento Institucional",   "Desalineamiento de objetivos estratégicos"),
                ("Reputacional",  "Afectación a la Imagen Institucional",        "Daño reputacional ante la ciudadanía y reguladores")
            };
            int o = 1;
            foreach (var t in tiposDef)
            {
                db.MaestroTiposRiesgo.Add(new MaestroTipoRiesgo
                {
                    Categoria = t.Cat,
                    Nombre = t.Nom,
                    Descripcion = t.Desc,
                    Activo = true,
                    Orden = o++
                });
            }
            await db.SaveChangesAsync();
        }

        var paramsDef = new List<(string Grupo, string Valor, string Desc)>
        {
            ("OrigenRiesgo","Interno","Origen definido por el formato DATOS"),
            ("OrigenRiesgo","Externo","Origen definido por el formato DATOS"),
            ("OrigenRiesgo","Ambos","Origen definido por el formato DATOS"),
            ("FrecuenciaRiesgo","No Recurrente","Frecuencia definida por el formato DATOS"),
            ("FrecuenciaRiesgo","Recurrente","Frecuencia definida por el formato DATOS"),
            ("TipoRiesgo","Estratégico","Tipo definido por el formato DATOS"),
            ("TipoRiesgo","Operacional","Tipo definido por el formato DATOS"),
            ("TipoRiesgo","Tecnologías de la información","Tipo definido por el formato DATOS"),
            ("TipoRiesgo","Reporte","Tipo definido por el formato DATOS"),
            ("TipoRiesgo","Cumplimiento","Tipo definido por el formato DATOS"),
            ("TipoImpacto","Económico","Aplica a riesgos de nivel entidad FONAFE"),
            ("TipoImpacto","No económico","Aplica a riesgos de nivel entidad FONAFE"),
            ("TipoImpacto","Híbrido","Aplica a riesgos de nivel entidad FONAFE"),
            ("FrecuenciaControl","Diaria","Frecuencia definida por el formato DATOS"),
            ("FrecuenciaControl","Semanal","Frecuencia definida por el formato DATOS"),
            ("FrecuenciaControl","Mensual","Frecuencia definida por el formato DATOS"),
            ("FrecuenciaControl","Bimestral","Frecuencia definida por el formato DATOS"),
            ("FrecuenciaControl","Trimestral","Frecuencia definida por el formato DATOS"),
            ("FrecuenciaControl","Semestral","Frecuencia definida por el formato DATOS"),
            ("FrecuenciaControl","Anual","Frecuencia definida por el formato DATOS"),
            ("FrecuenciaControl","Cada vez que suceda","Frecuencia definida por el formato DATOS"),
            ("OportunidadControl","Correctivo","Oportunidad definida por el formato DATOS"),
            ("OportunidadControl","Preventivo","Oportunidad definida por el formato DATOS"),
            ("OportunidadControl","Detectivo","Oportunidad definida por el formato DATOS"),
            ("AutomatizacionControl","Manual","Automatizacion definida por el formato DATOS"),
            ("AutomatizacionControl","Semiautomático","Automatizacion definida por el formato DATOS"),
            ("AutomatizacionControl","Automático","Automatizacion definida por el formato DATOS"),
            ("EstadoPlan","Concluido","Estado definido por el formato DATOS"),
            ("EstadoPlan","En proceso","Estado definido por el formato DATOS"),
            ("EstadoPlan","No iniciado","Estado definido por el formato DATOS"),
            ("EstrategiaTratamiento","Evitar","Estrategia definida por el formato DATOS"),
            ("EstrategiaTratamiento","Reducir o mitigar","Estrategia definida por el formato DATOS"),
            ("EstrategiaTratamiento","Transferir","Estrategia definida por el formato DATOS"),
            ("EstrategiaTratamiento","Retener o Aceptar","Estrategia definida por el formato DATOS"),
            ("EstrategiaTratamiento","Eliminar","Estrategia definida por el formato DATOS"),
            ("VerificacionEficacia","Si","Respuesta definida por el formato"),
            ("VerificacionEficacia","Parcialmente","Respuesta definida por el formato"),
            ("VerificacionEficacia","No","Respuesta definida por el formato")
        };

        var ordenParametro = await db.MaestroParametros.AnyAsync()
            ? await db.MaestroParametros.MaxAsync(p => p.Orden) + 1
            : 1;

        foreach (var p in paramsDef)
        {
            var existe = await db.MaestroParametros
                .AnyAsync(x => x.Grupo == p.Grupo && x.Valor == p.Valor);
            if (existe) continue;
            db.MaestroParametros.Add(new MaestroParametro
            {
                Grupo = p.Grupo,
                Valor = p.Valor,
                Descripcion = p.Desc,
                Activo = true,
                Orden = ordenParametro++
            });
        }

        await db.SaveChangesAsync();
        await SincronizarMatricesDesdeMaestrosAsync(db);
    }

    private async Task SincronizarMatricesDesdeMaestrosAsync(AppDbContext db)
    {
        var procesos = await db.MaestroProcesos
            .Where(p => p.Activo && p.Nivel == "Proceso")
            .OrderBy(p => p.Orden).ThenBy(p => p.Codigo)
            .ToListAsync();

        var numero = await db.MatrizGrupos.AnyAsync()
            ? await db.MatrizGrupos.MaxAsync(m => m.Numero) + 1
            : 1;

        foreach (var proceso in procesos)
        {
            var existe = await db.MatrizGrupos.AnyAsync(m => m.CodigoProceso == proceso.Codigo);
            if (existe) continue;
            db.MatrizGrupos.Add(new MatrizGrupo
            {
                Numero = numero++,
                CodigoProceso = proceso.Codigo,
                NombreProceso = proceso.Nombre,
                Nombre = $"MRC - {proceso.Codigo}",
                CodigoMatriz = $"MRC - {proceso.Codigo}",
                MatrizNivel = proceso.Nivel,
                FechaAprobacion = "",
                Fecha = "",
                Cerrada = false,
                FechaCreacion = DateTime.Now
            });
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<MaestroProceso>> GetMaestroProcesosAsync(bool soloActivos = false)
    {
        await EnsureSeedMasterDataAsync();
        using var db = Db();
        var q = db.MaestroProcesos
            .Include(p => p.Padre)
            .Include(p => p.AreaResponsable)
            .Include(p => p.Responsable)
            .AsQueryable();
        if (soloActivos) q = q.Where(p => p.Activo);
        return await q.OrderBy(p => p.Orden).ThenBy(p => p.Codigo).ToListAsync();
    }

    public async Task SaveMaestroProcesoAsync(MaestroProceso item)
    {
        using var db = Db();
        if (item.Id == 0) db.MaestroProcesos.Add(item);
        else db.MaestroProcesos.Update(item);
        await db.SaveChangesAsync();
        await SincronizarMatricesDesdeMaestrosAsync(db);
    }

    public async Task DeleteMaestroProcesoAsync(int id)
    {
        using var db = Db();
        var item = await db.MaestroProcesos.FindAsync(id);
        if (item != null)
        {
            var tieneHijos = await db.MaestroProcesos.AnyAsync(p => p.PadreId == id);
            if (tieneHijos) item.Activo = false;
            else db.MaestroProcesos.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<MaestroArea>> GetMaestroAreasAsync(bool soloActivas = false)
    {
        await EnsureSeedMasterDataAsync();
        using var db = Db();
        var q = db.MaestroAreas.Include(a => a.Padre).AsQueryable();
        if (soloActivas) q = q.Where(a => a.Activo);
        return await q.OrderBy(a => a.Categoria).ThenBy(a => a.Orden).ThenBy(a => a.Nombre).ToListAsync();
    }

    public async Task<List<BitacoraDepartamentoGerencia>> GetBitacoraDepartamentoGerenciasAsync()
    {
        using var db = Db();
        await EnsureDatabaseTablesCreatedAsync(db);
        try
        {
            return await db.BitacoraDepartamentoGerencias
                .OrderByDescending(b => b.FechaCambio)
                .ToListAsync();
        }
        catch { return new List<BitacoraDepartamentoGerencia>(); }
    }

    public async Task<List<MaestroResponsable>> GetMaestroResponsablesAsync(bool soloActivos = false)
    {
        await EnsureSeedMasterDataAsync();
        using var db = Db();
        var q = db.MaestroResponsables.Include(r => r.Area).AsQueryable();
        if (soloActivos) q = q.Where(r => r.Activo);
        return await q.OrderBy(r => r.Orden).ThenBy(r => r.Nombre).ToListAsync();
    }

    public async Task SaveMaestroResponsableAsync(MaestroResponsable item)
    {
        using var db = Db();
        if (item.Id == 0) db.MaestroResponsables.Add(item);
        else db.MaestroResponsables.Update(item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteMaestroResponsableAsync(int id)
    {
        using var db = Db();
        var item = await db.MaestroResponsables.FindAsync(id);
        if (item != null) { db.MaestroResponsables.Remove(item); await db.SaveChangesAsync(); }
    }

    public async Task SaveMaestroAreaAsync(MaestroArea item, string usuarioNombre = "SuperAdmin", string motivo = "")
    {
        using var db = Db();
        item.Categoria = NormalizarCategoriaArea(item.Categoria);
        if (item.Categoria == "DIRECTORIO") item.PadreId = null;

        if (item.Id == 0)
        {
            db.MaestroAreas.Add(item);
        }
        else
        {
            var existente = await db.MaestroAreas.Include(a => a.Padre).FirstOrDefaultAsync(a => a.Id == item.Id);
            if (existente != null && existente.PadreId != item.PadreId)
            {
                var gerenciaAnteriorNombre = existente.Padre?.Nombre ?? "Sin Gerencia Asignada";
                string gerenciaNuevaNombre = "Sin Gerencia Asignada";
                if (item.PadreId.HasValue)
                {
                    var nuevaG = await db.MaestroAreas.FindAsync(item.PadreId.Value);
                    if (nuevaG != null) gerenciaNuevaNombre = nuevaG.Nombre;
                }
                db.BitacoraDepartamentoGerencias.Add(new BitacoraDepartamentoGerencia
                {
                    DepartamentoId = item.Id,
                    NombreDepartamento = item.Nombre,
                    GerenciaAnteriorId = existente.PadreId,
                    GerenciaAnteriorNombre = gerenciaAnteriorNombre,
                    GerenciaNuevaId = item.PadreId,
                    GerenciaNuevaNombre = gerenciaNuevaNombre,
                    Periodo = DateTime.Now.Year.ToString(),
                    FechaCambio = DateTime.Now,
                    UsuarioNombre = string.IsNullOrWhiteSpace(usuarioNombre) ? "SuperAdmin" : usuarioNombre,
                    Motivo = string.IsNullOrWhiteSpace(motivo) ? "Reestructuración Organizacional" : motivo
                });
            }
            db.MaestroAreas.Update(item);
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteMaestroAreaAsync(int id)
    {
        using var db = Db();
        var item = await db.MaestroAreas.FindAsync(id);
        if (item != null)
        {
            var tieneDependencias =
                await db.MaestroAreas.AnyAsync(a => a.PadreId == id) ||
                await db.MaestroResponsables.AnyAsync(r => r.AreaId == id) ||
                await db.MaestroProcesos.AnyAsync(p => p.AreaResponsableId == id);
            if (tieneDependencias) item.Activo = false;
            else db.MaestroAreas.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<MaestroTipoRiesgo>> GetMaestroTiposRiesgoAsync(bool soloActivos = false)
    {
        await EnsureSeedMasterDataAsync();
        using var db = Db();
        var q = db.MaestroTiposRiesgo.AsQueryable();
        if (soloActivos) q = q.Where(t => t.Activo);
        return await q.OrderBy(t => t.Orden).ThenBy(t => t.Nombre).ToListAsync();
    }

    public async Task SaveMaestroTipoRiesgoAsync(MaestroTipoRiesgo item)
    {
        using var db = Db();
        if (item.Id == 0) db.MaestroTiposRiesgo.Add(item);
        else db.MaestroTiposRiesgo.Update(item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteMaestroTipoRiesgoAsync(int id)
    {
        using var db = Db();
        var item = await db.MaestroTiposRiesgo.FindAsync(id);
        if (item != null) { db.MaestroTiposRiesgo.Remove(item); await db.SaveChangesAsync(); }
    }

    public async Task<List<MaestroParametro>> GetMaestroParametrosAsync(string? grupo = null, bool soloActivos = false)
    {
        await EnsureSeedMasterDataAsync();
        using var db = Db();
        var q = db.MaestroParametros.AsQueryable();
        if (!string.IsNullOrEmpty(grupo)) q = q.Where(p => p.Grupo == grupo);
        if (soloActivos) q = q.Where(p => p.Activo);
        return await q.OrderBy(p => p.Orden).ThenBy(p => p.Valor).ToListAsync();
    }

    public async Task SaveMaestroParametroAsync(MaestroParametro item)
    {
        using var db = Db();
        if (item.Id == 0) db.MaestroParametros.Add(item);
        else db.MaestroParametros.Update(item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteMaestroParametroAsync(int id)
    {
        using var db = Db();
        var item = await db.MaestroParametros.FindAsync(id);
        if (item != null) { db.MaestroParametros.Remove(item); await db.SaveChangesAsync(); }
    }

    // ── Helper: divide celdas con múltiples valores separados por \n ────────
    // Filtra líneas vacías y marcas visuales como "(X)"
    private static List<string> SplitCeldaMulti(string celda)
    {
        if (string.IsNullOrWhiteSpace(celda)) return new List<string>();
        return celda
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l) &&
                        !l.Equals("(X)", StringComparison.OrdinalIgnoreCase) &&
                        !l.Equals("-", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string NormalizarCategoriaArea(string categoria)
    {
        var valor = (categoria ?? "").Trim().ToUpperInvariant();
        return valor switch
        {
            "DIRECTORIO" => "DIRECTORIO",
            "GERENCIA" or "GERENCIAS" => "GERENCIAS",
            _ => "DEPARTAMENTOS"
        };
    }

    // ═════════════════════════════════════════════════════════════════════
    // RESUMEN DE RIESGOS — CONSULTA DIRECTA SIN DUPLICADOS
    // ═════════════════════════════════════════════════════════════════════
    public async Task<List<RiesgoResumenItem>> GetResumenRiesgosAsync(int userId, string rol)
    {
        using var db = Db();

        IQueryable<Riesgo> query = db.Riesgos.Include(r => r.Indicadores);

        if (rol != "SuperAdmin" && rol != "Admin")
            query = query.Where(r => r.UsuarioId == userId);

        var riesgos = await query
            .Where(r =>
                r.CodigoProceso != null && r.CodigoProceso != "" &&
                r.CodigoRiesgo != null && r.CodigoRiesgo != "" &&
                r.ProbabilidadResidual >= 1 && r.ProbabilidadResidual <= 4 &&
                r.ImpactoResidual >= 1 && r.ImpactoResidual <= 4)
            .ToListAsync();

        var unicos = riesgos
            .GroupBy(r => r.CodigoRiesgo.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(r => r.Id).First())
            .ToList();

        return unicos.Select(r =>
        {
            // ✅ FIX: Riesgo.GetNivel(int, int) ahora existe como sobrecarga
            string niv = Riesgo.GetNivel(r.ProbabilidadResidual, r.ImpactoResidual);

            return new RiesgoResumenItem
            {
                CodigoProceso = r.CodigoProceso.Trim(),
                NombreProceso = r.NombreProceso ?? "",
                CodigoRiesgo = r.CodigoRiesgo.Trim(),
                NivelResidual = niv,
                CantIndicadores = r.Indicadores?.Count ?? 0
            };
        }).ToList();
    }

    // ═════════════════════════════════════════════════════════════════════
    // CARGA MASIVA
    // ═════════════════════════════════════════════════════════════════════
    public async Task ValidarDuplicadosAsync(List<FilaCargaMasiva> filas)
    {
        using var db = Db();
        var codigosBuscados = filas
            .Select(f => f.CodigoRiesgo)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();

        var existenciasBd = await db.Riesgos
            .Where(r => codigosBuscados.Contains(r.CodigoRiesgo))
            .Select(r => r.CodigoRiesgo)
            .ToListAsync();

        var setBd = new HashSet<string>(existenciasBd, StringComparer.OrdinalIgnoreCase);

        foreach (var f in filas)
        {
            if (!string.IsNullOrWhiteSpace(f.CodigoRiesgo) && setBd.Contains(f.CodigoRiesgo))
            {
                f.ExisteEnBaseDatos = true;
                if (!f.EsDuplicadoEnArchivo)
                    f.EstadoDuplicado = "Existe en BD";
            }
        }
    }

    public async Task<ResultadoCargaMasiva> ProcesarCargaMasivaAsync(
        List<FilaCargaMasiva> filas, bool actualizarExistentes, string usuarioNombre)
    {
        await ValidarDuplicadosAsync(filas);
        var res = new ResultadoCargaMasiva { TotalFilasLeidas = filas.Count, FilasProcesadas = filas };
        using var db = Db();
        using var trans = await db.Database.BeginTransactionAsync();

        try
        {
            foreach (var f in filas)
            {
                if (!f.EsValido ||
                    string.IsNullOrWhiteSpace(f.CodigoProceso) ||
                    string.IsNullOrWhiteSpace(f.CodigoRiesgo))
                {
                    res.TotalErrores++;
                    res.MensajesLog.Add($"Fila {f.FilaNumero}: Omitida — datos incompletos.");
                    continue;
                }

                if (!actualizarExistentes && f.ExisteEnBaseDatos)
                {
                    res.TotalRiesgosDuplicadosOmitidos++;
                    res.MensajesLog.Add($"Fila {f.FilaNumero}: {f.CodigoRiesgo} omitido (ya existe en BD).");
                    continue;
                }

                // 1. Buscar o crear MatrizGrupo
                var matriz = await db.MatrizGrupos
                    .FirstOrDefaultAsync(m => m.CodigoProceso == f.CodigoProceso);
                if (matriz == null)
                {
                    matriz = new MatrizGrupo
                    {
                        Codigo = "MTR-" + f.CodigoProceso,
                        CodigoProceso = f.CodigoProceso,
                        NombreProceso = string.IsNullOrWhiteSpace(f.NombreProceso)
                                        ? "Proceso " + f.CodigoProceso : f.NombreProceso,
                        Nombre = string.IsNullOrWhiteSpace(f.NombreProceso)
                                        ? "Matriz " + f.CodigoProceso : f.NombreProceso,
                        ElaboradoPor = string.IsNullOrWhiteSpace(usuarioNombre)
                                        ? "SuperAdmin" : usuarioNombre,
                        Version = "1.0",
                        Fecha = DateTime.Now.ToString("yyyy-MM-dd")
                    };
                    db.MatrizGrupos.Add(matriz);
                    await db.SaveChangesAsync();
                    res.MensajesLog.Add($"Matriz creada para proceso {f.CodigoProceso}.");
                }

                // 2. Buscar o crear Riesgo
                var riesgo = await db.Riesgos
                    .Include(r => r.Controles)
                    .Include(r => r.PlanesAccion)
                    .FirstOrDefaultAsync(r =>
                        r.MatrizGrupoId == matriz.Id &&
                        r.CodigoRiesgo == f.CodigoRiesgo);

                if (riesgo != null)
                {
                    if (actualizarExistentes)
                    {
                        riesgo.DescripcionRiesgo = f.DescripcionRiesgo;
                        riesgo.GerenciaResponsable = string.IsNullOrWhiteSpace(f.GerenciaResponsable) ? riesgo.GerenciaResponsable : f.GerenciaResponsable;
                        riesgo.NombreProceso = string.IsNullOrWhiteSpace(f.NombreProceso) ? riesgo.NombreProceso : f.NombreProceso;
                        riesgo.Subproceso = f.Subproceso;
                        riesgo.OrigenRiesgo = string.IsNullOrWhiteSpace(f.OrigenRiesgo) ? riesgo.OrigenRiesgo : f.OrigenRiesgo;
                        riesgo.FrecuenciaRiesgo = string.IsNullOrWhiteSpace(f.FrecuenciaRiesgo) ? riesgo.FrecuenciaRiesgo : f.FrecuenciaRiesgo;
                        riesgo.TipoRiesgo = string.IsNullOrWhiteSpace(f.TipoRiesgo) ? riesgo.TipoRiesgo : f.TipoRiesgo;
                        riesgo.ProbabilidadInherente = f.ProbabilidadInherente;
                        riesgo.ImpactoInherente = f.ImpactoInherente;
                        riesgo.ProbabilidadResidual = f.ProbabilidadResidual;
                        riesgo.ImpactoResidual = f.ImpactoResidual;
                        riesgo.EstrategiaResidual = string.IsNullOrWhiteSpace(f.EstrategiaRespuesta) ? riesgo.EstrategiaResidual : f.EstrategiaRespuesta;
                        res.TotalRiesgosActualizados++;
                    }
                }
                else
                {
                    riesgo = new Riesgo
                    {
                        MatrizGrupoId = matriz.Id,
                        CodigoProceso = f.CodigoProceso,
                        NombreProceso = string.IsNullOrWhiteSpace(f.NombreProceso) ? matriz.NombreProceso : f.NombreProceso,
                        GerenciaResponsable = string.IsNullOrWhiteSpace(f.GerenciaResponsable) ? "Gerencia General" : f.GerenciaResponsable,
                        Subproceso = f.Subproceso,
                        CodigoRiesgo = f.CodigoRiesgo,
                        DescripcionRiesgo = f.DescripcionRiesgo,
                        OrigenRiesgo = string.IsNullOrWhiteSpace(f.OrigenRiesgo) ? "Interno" : f.OrigenRiesgo,
                        FrecuenciaRiesgo = string.IsNullOrWhiteSpace(f.FrecuenciaRiesgo) ? "Recurrente" : f.FrecuenciaRiesgo,
                        TipoRiesgo = string.IsNullOrWhiteSpace(f.TipoRiesgo) ? "Operacional" : f.TipoRiesgo,
                        ProbabilidadInherente = f.ProbabilidadInherente,
                        ImpactoInherente = f.ImpactoInherente,
                        ProbabilidadResidual = f.ProbabilidadResidual,
                        ImpactoResidual = f.ImpactoResidual,
                        EstrategiaResidual = string.IsNullOrWhiteSpace(f.EstrategiaRespuesta) ? "Retener" : f.EstrategiaRespuesta
                    };
                    db.Riesgos.Add(riesgo);
                    await db.SaveChangesAsync();
                    res.TotalRiesgosNuevos++;
                }

                // 3. Control
                if (!string.IsNullOrWhiteSpace(f.CodigoControl) ||
                    !string.IsNullOrWhiteSpace(f.DescripcionControl))
                {
                    var codC = string.IsNullOrWhiteSpace(f.CodigoControl)
                               ? f.CodigoRiesgo + "-C1" : f.CodigoControl;
                    var ctrl = riesgo.Controles.FirstOrDefault(c => c.CodigoControl == codC);
                    if (ctrl == null)
                    {
                        ctrl = new RiesgoControl
                        {
                            RiesgoId = riesgo.Id,
                            CodigoControl = codC,
                            DescripcionControl = string.IsNullOrWhiteSpace(f.DescripcionControl) ? "Control preventivo" : f.DescripcionControl,
                            AreaResponsable = f.AreaResponsableControl,
                            ResponsablesControl = f.ResponsableControl,
                            FrecuenciaControl = string.IsNullOrWhiteSpace(f.FrecuenciaControl) ? "Cada vez que suceda" : f.FrecuenciaControl,
                            OportunidadControl = string.IsNullOrWhiteSpace(f.OportunidadControl) ? "Preventivo" : f.OportunidadControl,
                            AutomatizacionControl = string.IsNullOrWhiteSpace(f.AutomatizacionControl) ? "Manual" : f.AutomatizacionControl,
                            EvidenciaControl = f.EvidenciaControl
                        };
                        db.RiesgosControl.Add(ctrl);
                        riesgo.Controles.Add(ctrl);
                        res.TotalControlesCreados++;
                    }
                    else if (actualizarExistentes)
                    {
                        ctrl.DescripcionControl = string.IsNullOrWhiteSpace(f.DescripcionControl) ? ctrl.DescripcionControl : f.DescripcionControl;
                        ctrl.AreaResponsable = string.IsNullOrWhiteSpace(f.AreaResponsableControl) ? ctrl.AreaResponsable : f.AreaResponsableControl;
                        ctrl.ResponsablesControl = string.IsNullOrWhiteSpace(f.ResponsableControl) ? ctrl.ResponsablesControl : f.ResponsableControl;
                        ctrl.EvidenciaControl = string.IsNullOrWhiteSpace(f.EvidenciaControl) ? ctrl.EvidenciaControl : f.EvidenciaControl;
                    }
                }

                // 4. Plan de Acción
                if (!string.IsNullOrWhiteSpace(f.CodigoPlanAccion) ||
                    !string.IsNullOrWhiteSpace(f.DescripcionPlanAccion))
                {
                    var codP = string.IsNullOrWhiteSpace(f.CodigoPlanAccion)
                               ? f.CodigoRiesgo + "-PA1" : f.CodigoPlanAccion;
                    var plan = riesgo.PlanesAccion.FirstOrDefault(p => p.CodigoPlan == codP);
                    if (plan == null)
                    {
                        plan = new PlanAccion
                        {
                            RiesgoId = riesgo.Id,
                            CodigoPlan = codP,
                            DescripcionPlan = string.IsNullOrWhiteSpace(f.DescripcionPlanAccion) ? "Plan de Mitigación" : f.DescripcionPlanAccion,
                            AreaResponsable = f.AreaResponsablePlan,
                            ResponsablePlan = f.ResponsablePlan,
                            InicioPlan = f.InicioPlanAccion ?? DateTime.Now,
                            EstadoPlan = string.IsNullOrWhiteSpace(f.EstadoPlanAccion) ? "No iniciado" : f.EstadoPlanAccion,
                            FinPlan = f.FinPlanAccion ?? DateTime.Now.AddMonths(3)
                        };
                        db.PlanesAccion.Add(plan);
                        riesgo.PlanesAccion.Add(plan);
                        res.TotalPlanesCreados++;
                    }
                    else if (actualizarExistentes)
                    {
                        plan.DescripcionPlan = string.IsNullOrWhiteSpace(f.DescripcionPlanAccion) ? plan.DescripcionPlan : f.DescripcionPlanAccion;
                        plan.AreaResponsable = string.IsNullOrWhiteSpace(f.AreaResponsablePlan) ? plan.AreaResponsable : f.AreaResponsablePlan;
                        plan.ResponsablePlan = string.IsNullOrWhiteSpace(f.ResponsablePlan) ? plan.ResponsablePlan : f.ResponsablePlan;
                        plan.EstadoPlan = string.IsNullOrWhiteSpace(f.EstadoPlanAccion) ? plan.EstadoPlan : f.EstadoPlanAccion;
                    }
                }

                // 5. KRI — múltiples indicadores por celda separados por \n
                if (!string.IsNullOrWhiteSpace(f.CodigoKRI))
                {
                    // Dividir celdas multi-valor: cada línea no vacía y no (X) es un KRI
                    var codigosKRI = SplitCeldaMulti(f.CodigoKRI);
                    var definicionesKRI = SplitCeldaMulti(f.DefinicionKRI);
                    var frecuenciasKRI = SplitCeldaMulti(f.FrecuenciaKRI);
                    var metasKRI = SplitCeldaMulti(f.MetaKRI);
                    var actualesKRI = SplitCeldaMulti(f.KRIActual);
                    var responsablesKRI = SplitCeldaMulti(f.ResponsableKRI);

                    for (int k = 0; k < codigosKRI.Count; k++)
                    {
                        var codKRI = codigosKRI[k];
                        if (string.IsNullOrWhiteSpace(codKRI)) continue;

                        // Verificar si ya existe este KRI para este riesgo
                        var kriExist = await db.Indicadores
                            .AnyAsync(i => i.RiesgoId == riesgo.Id && i.CodigoKRI == codKRI);
                        if (kriExist) continue;

                        db.Indicadores.Add(new Indicador
                        {
                            RiesgoId = riesgo.Id,
                            CodigoKRI = codKRI,
                            DefinicionKRI = k < definicionesKRI.Count ? definicionesKRI[k] : "",
                            Frecuencia = k < frecuenciasKRI.Count ? frecuenciasKRI[k] : "",
                            MetaKRI = k < metasKRI.Count ? metasKRI[k] : "",
                            KRIActual = k < actualesKRI.Count ? actualesKRI[k] : "",
                            ResponsableKRI = k < responsablesKRI.Count ? responsablesKRI[k] : ""
                        });
                    }
                }

                await db.SaveChangesAsync();
            }

            await trans.CommitAsync();
            res.MensajesLog.Add(
                $"Carga completada. Nuevos: {res.TotalRiesgosNuevos}, " +
                $"Actualizados: {res.TotalRiesgosActualizados}, " +
                $"Controles: {res.TotalControlesCreados}, " +
                $"Planes: {res.TotalPlanesCreados}.");
        }
        catch (Exception ex)
        {
            await trans.RollbackAsync();
            res.TotalErrores++;
            res.MensajesLog.Add($"ERROR EN CARGA MASIVA: {ex.Message}");
        }

        return res;
    }
}