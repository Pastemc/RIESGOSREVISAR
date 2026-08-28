using Microsoft.EntityFrameworkCore;
using RiesgosElor.Data;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

public class UsuarioService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly EmailService _emailSvc;

    public UsuarioService(IDbContextFactory<AppDbContext> dbFactory, EmailService emailSvc)
    {
        _dbFactory = dbFactory;
        _emailSvc = emailSvc;
    }

    public async Task<List<Usuario>> GetTodosAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Usuarios.OrderBy(u => u.FechaCreacion).ToListAsync();
    }

    public async Task<List<Usuario>> GetPersonalAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Usuarios
            .Where(u => u.Rol != "SuperAdmin" && u.Activo)
            .OrderBy(u => u.Nombre)
            .ToListAsync();
    }

    public async Task<List<CambioPassword>> GetCambiosAsync(int usuarioId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.CambiosPassword
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();
    }

    public async Task<List<UsuarioMatriz>> GetTodasAsignacionesAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.UsuarioMatrices.ToListAsync();
    }

    public async Task<List<MatrizGrupo>> GetMatricesProcesosActivasAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.MatrizGrupos.OrderBy(m => m.Nombre).ToListAsync();
    }

    public async Task<List<BitacoraUsuario>> GetBitacorasAsync(int usuarioId)
    {
        using var db = _dbFactory.CreateDbContext();
        await AuthService.EnsureUsuarioColumnsExistAsync(db);
        return await db.BitacorasUsuario
            .Where(b => b.UsuarioId == usuarioId)
            .OrderByDescending(b => b.FechaCambio)
            .ToListAsync();
    }

    public async Task<bool> CrearAsync(string nombre, string correo, string password, string rol)
    {
        using var db = _dbFactory.CreateDbContext();
        if (await db.Usuarios.AnyAsync(u => u.Correo == correo)) return false;
        db.Usuarios.Add(new Usuario
        {
            Nombre = nombre,
            Correo = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Rol = rol,
            Activo = false
        });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActualizarUsuarioPerfilYRolAsync(
        int id,
        string nombre,
        string correo,
        string username,
        string rol,
        string cargo,
        string gerencia,
        string departamento,
        bool activo,
        string modificadoPor,
        string motivo = "")
    {
        using var db = _dbFactory.CreateDbContext();
        await AuthService.EnsureUsuarioColumnsExistAsync(db);

        var u = await db.Usuarios.FindAsync(id);
        if (u == null) return false;

        nombre = nombre?.Trim() ?? "";
        correo = correo?.Trim() ?? "";
        username = username?.Trim() ?? "";
        rol = rol?.Trim() ?? "Personal";
        cargo = cargo?.Trim() ?? "";
        gerencia = gerencia?.Trim() ?? "";
        departamento = departamento?.Trim() ?? "";
        modificadoPor = string.IsNullOrWhiteSpace(modificadoPor) ? "SuperAdmin" : modificadoPor.Trim();
        motivo = motivo?.Trim() ?? "";

        var bitacoras = new List<BitacoraUsuario>();

        void RegistrarCambio(string campo, string valViejo, string valNuevo)
        {
            if (valViejo != valNuevo)
            {
                bitacoras.Add(new BitacoraUsuario
                {
                    UsuarioId = id,
                    ModificadoPor = modificadoPor,
                    Campo = campo,
                    ValorAnterior = valViejo,
                    ValorNuevo = valNuevo,
                    Motivo = motivo,
                    FechaCambio = DateTime.Now
                });
            }
        }

        RegistrarCambio("Nombre", u.Nombre ?? "", nombre);
        RegistrarCambio("Correo", u.Correo ?? "", correo);
        RegistrarCambio("Username", u.Username ?? "", username);
        RegistrarCambio("Rol", u.Rol ?? "Personal", rol);
        RegistrarCambio("Cargo", u.Cargo ?? "", cargo);
        RegistrarCambio("Gerencia", u.Gerencia ?? "", gerencia);
        RegistrarCambio("Departamento", u.Departamento ?? "", departamento);
        RegistrarCambio("Estado", u.Activo ? "Activo" : "Inactivo",
                                        activo ? "Activo" : "Inactivo");

        u.Nombre = nombre;
        u.Correo = correo;
        u.Username = username;
        u.Rol = rol;
        u.Cargo = cargo;
        u.Gerencia = gerencia;
        u.Departamento = departamento;
        u.Activo = activo;

        if (bitacoras.Any())
            db.BitacorasUsuario.AddRange(bitacoras);

        await db.SaveChangesAsync();

        await AuthService.SincronizarOrganizacionGirAsync(
            db, u.Nombre, u.Correo, u.Cargo, u.Gerencia, u.Departamento);

        return true;
    }

    public async Task<bool> AprobarUsuarioConRolYMatricesAsync(
        int usuarioId,
        string rol,
        List<int> idsMatrices,
        string modificadoPor)
    {
        using var db = _dbFactory.CreateDbContext();
        await AuthService.EnsureUsuarioColumnsExistAsync(db);

        var u = await db.Usuarios.FindAsync(usuarioId);
        if (u == null) return false;

        rol = string.IsNullOrWhiteSpace(rol) ? "Personal" : rol.Trim();
        modificadoPor = string.IsNullOrWhiteSpace(modificadoPor) ? "SuperAdmin" : modificadoPor.Trim();

        string rolAnterior = u.Rol;
        u.Rol = rol;
        u.Activo = true;

        if (idsMatrices != null)
        {
            // Quitar matrices actuales
            var actuales = await db.UsuarioMatrices
                .Where(um => um.UsuarioId == usuarioId).ToListAsync();
            db.UsuarioMatrices.RemoveRange(actuales);

            // Agregar nuevas
            foreach (var mId in idsMatrices)
            {
                db.UsuarioMatrices.Add(new UsuarioMatriz
                {
                    UsuarioId = usuarioId,
                    MatrizGrupoId = mId
                });
            }
        }

        db.BitacorasUsuario.Add(new BitacoraUsuario
        {
            UsuarioId = usuarioId,
            ModificadoPor = modificadoPor,
            Campo = "Aprobación y Asignación de Rol",
            ValorAnterior = $"Inactivo ({rolAnterior})",
            ValorNuevo = $"Activo ({rol}) - {idsMatrices?.Count ?? 0} Matrices/Procesos Asignados",
            Motivo = $"Solicitud de acceso aprobada por Administrador. Rol asignado: {rol}.",
            FechaCambio = DateTime.Now
        });

        await db.SaveChangesAsync();

        await AuthService.SincronizarOrganizacionGirAsync(
            db, u.Nombre, u.Correo, u.Cargo, u.Gerencia, u.Departamento);

        await _emailSvc.EnviarCorreoAprobacionAsync(u.Correo, u.Nombre);

        return true;
    }

    public async Task ToggleActivoAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        var u = await db.Usuarios.FindAsync(id);
        if (u != null && u.Rol != "SuperAdmin")
        {
            bool estadoAnterior = u.Activo;
            u.Activo = !u.Activo;

            db.BitacorasUsuario.Add(new BitacoraUsuario
            {
                UsuarioId = id,
                ModificadoPor = "Administrador",
                Campo = "Estado",
                ValorAnterior = estadoAnterior ? "Activo" : "Inactivo",
                ValorNuevo = u.Activo ? "Activo" : "Inactivo",
                Motivo = "Cambio de estado desde listado de usuarios",
                FechaCambio = DateTime.Now
            });

            await db.SaveChangesAsync();

            if (u.Activo)
                await _emailSvc.EnviarCorreoAprobacionAsync(u.Correo, u.Nombre);
        }
    }

    public async Task EliminarAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        var u = await db.Usuarios.FindAsync(id);
        if (u != null && u.Rol != "SuperAdmin")
        {
            db.Usuarios.Remove(u);
            await db.SaveChangesAsync();
        }
    }

    public async Task<bool> ActivarAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        var u = await db.Usuarios.FindAsync(id);
        if (u == null) return false;

        bool estadoAnterior = u.Activo;
        u.Activo = true;

        if (!estadoAnterior)
        {
            db.BitacorasUsuario.Add(new BitacoraUsuario
            {
                UsuarioId = id,
                ModificadoPor = "Administrador",
                Campo = "Estado",
                ValorAnterior = "Inactivo (Pendiente)",
                ValorNuevo = "Activo",
                Motivo = "Aprobación de solicitud de acceso por el Administrador",
                FechaCambio = DateTime.Now
            });
        }

        await db.SaveChangesAsync();
        await _emailSvc.EnviarCorreoAprobacionAsync(u.Correo, u.Nombre);
        return true;
    }

    // ===== ASIGNACIÓN DE MATRICES =====

    public async Task<List<UsuarioMatriz>> GetMatricesAsignadasAsync(int usuarioId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.UsuarioMatrices
            .Include(um => um.MatrizGrupo)
            .Where(um => um.UsuarioId == usuarioId)
            .ToListAsync();
    }

    public async Task<List<int>> GetIdsMatricesAsignadasAsync(int usuarioId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.UsuarioMatrices
            .Where(um => um.UsuarioId == usuarioId)
            .Select(um => um.MatrizGrupoId)
            .ToListAsync();
    }

    public async Task AsignarMatrizAsync(int usuarioId, int matrizId)
    {
        using var db = _dbFactory.CreateDbContext();
        var existe = await db.UsuarioMatrices.AnyAsync(
            um => um.UsuarioId == usuarioId && um.MatrizGrupoId == matrizId);
        if (!existe)
        {
            db.UsuarioMatrices.Add(new UsuarioMatriz
            {
                UsuarioId = usuarioId,
                MatrizGrupoId = matrizId
            });
            await db.SaveChangesAsync();
        }
    }

    public async Task QuitarMatrizAsync(int usuarioId, int matrizId)
    {
        using var db = _dbFactory.CreateDbContext();
        var um = await db.UsuarioMatrices.FirstOrDefaultAsync(
            x => x.UsuarioId == usuarioId && x.MatrizGrupoId == matrizId);
        if (um != null)
        {
            db.UsuarioMatrices.Remove(um);
            await db.SaveChangesAsync();
        }
    }

    public async Task QuitarTodasMatricesAsync(int usuarioId)
    {
        using var db = _dbFactory.CreateDbContext();
        var lista = await db.UsuarioMatrices
            .Where(um => um.UsuarioId == usuarioId).ToListAsync();
        db.UsuarioMatrices.RemoveRange(lista);
        await db.SaveChangesAsync();
    }
}