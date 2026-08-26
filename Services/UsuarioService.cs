using Microsoft.EntityFrameworkCore;
using RiesgosElor.Data;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

public class UsuarioService
{
    private readonly AppDbContext _db;
    private readonly EmailService _emailSvc;

    public UsuarioService(AppDbContext db, EmailService emailSvc)
    {
        _db = db;
        _emailSvc = emailSvc;
    }

    public Task<List<Usuario>> GetTodosAsync() =>
        _db.Usuarios.OrderBy(u => u.FechaCreacion).ToListAsync();

    public Task<List<Usuario>> GetPersonalAsync() =>
        _db.Usuarios.Where(u => u.Rol != "SuperAdmin" && u.Activo).OrderBy(u => u.Nombre).ToListAsync();

    public Task<List<CambioPassword>> GetCambiosAsync(int usuarioId) =>
        _db.CambiosPassword
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();

    public async Task<List<UsuarioMatriz>> GetTodasAsignacionesAsync() =>
        await _db.UsuarioMatrices.ToListAsync();

    public async Task<List<MatrizGrupo>> GetMatricesProcesosActivasAsync() =>
        await _db.MatrizGrupos.OrderBy(m => m.Nombre).ToListAsync();

    public async Task<List<BitacoraUsuario>> GetBitacorasAsync(int usuarioId)
    {
        await AuthService.EnsureUsuarioColumnsExistAsync(_db);
        return await _db.BitacorasUsuario
            .Where(b => b.UsuarioId == usuarioId)
            .OrderByDescending(b => b.FechaCambio)
            .ToListAsync();
    }

    public async Task<bool> CrearAsync(string nombre, string correo, string password, string rol)
    {
        if (await _db.Usuarios.AnyAsync(u => u.Correo == correo)) return false;
        _db.Usuarios.Add(new Usuario
        {
            Nombre = nombre,
            Correo = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Rol = rol,
            Activo = false
        });
        await _db.SaveChangesAsync();
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
        await AuthService.EnsureUsuarioColumnsExistAsync(_db);
        var u = await _db.Usuarios.FindAsync(id);
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
        RegistrarCambio("Estado", u.Activo ? "Activo" : "Inactivo", activo ? "Activo" : "Inactivo");

        // Actualizar usuario
        u.Nombre = nombre;
        u.Correo = correo;
        u.Username = username;
        u.Rol = rol;
        u.Cargo = cargo;
        u.Gerencia = gerencia;
        u.Departamento = departamento;
        u.Activo = activo;

        if (bitacoras.Any())
        {
            _db.BitacorasUsuario.AddRange(bitacoras);
        }

        await _db.SaveChangesAsync();

        // Sincronizar en Organización GIR y Tablas Maestras
        await AuthService.SincronizarOrganizacionGirAsync(_db, u.Nombre, u.Correo, u.Cargo, u.Gerencia, u.Departamento);

        return true;
    }

    public async Task<bool> AprobarUsuarioConRolYMatricesAsync(
        int usuarioId,
        string rol,
        List<int> idsMatrices,
        string modificadoPor)
    {
        await AuthService.EnsureUsuarioColumnsExistAsync(_db);
        var u = await _db.Usuarios.FindAsync(usuarioId);
        if (u == null) return false;

        rol = string.IsNullOrWhiteSpace(rol) ? "Personal" : rol.Trim();
        modificadoPor = string.IsNullOrWhiteSpace(modificadoPor) ? "SuperAdmin" : modificadoPor.Trim();

        string rolAnterior = u.Rol;
        u.Rol = rol;
        u.Activo = true;

        // Asignar matrices seleccionadas si aplica
        if (idsMatrices != null)
        {
            await QuitarTodasMatricesAsync(usuarioId);
            foreach (var mId in idsMatrices)
            {
                await AsignarMatrizAsync(usuarioId, mId);
            }
        }

        // Registrar en Bitácora de Auditoría
        _db.BitacorasUsuario.Add(new BitacoraUsuario
        {
            UsuarioId = usuarioId,
            ModificadoPor = modificadoPor,
            Campo = "Aprobación y Asignación de Rol",
            ValorAnterior = $"Inactivo ({rolAnterior})",
            ValorNuevo = $"Activo ({rol}) - {idsMatrices?.Count ?? 0} Matrices/Procesos Asignados",
            Motivo = $"Solicitud de acceso aprobada por Administrador. Rol asignado: {rol}.",
            FechaCambio = DateTime.Now
        });

        await _db.SaveChangesAsync();

        // Sincronizar en Organización GIR y Tablas Maestras
        await AuthService.SincronizarOrganizacionGirAsync(_db, u.Nombre, u.Correo, u.Cargo, u.Gerencia, u.Departamento);

        // Enviar notificación por correo
        await _emailSvc.EnviarCorreoAprobacionAsync(u.Correo, u.Nombre);

        return true;
    }

    public async Task ToggleActivoAsync(int id)
    {
        var u = await _db.Usuarios.FindAsync(id);
        if (u != null && u.Rol != "SuperAdmin")
        {
            bool estadoAnterior = u.Activo;
            u.Activo = !u.Activo;

            _db.BitacorasUsuario.Add(new BitacoraUsuario
            {
                UsuarioId = id,
                ModificadoPor = "Administrador",
                Campo = "Estado",
                ValorAnterior = estadoAnterior ? "Activo" : "Inactivo",
                ValorNuevo = u.Activo ? "Activo" : "Inactivo",
                Motivo = "Cambio de estado desde listado de usuarios",
                FechaCambio = DateTime.Now
            });

            await _db.SaveChangesAsync();
            if (u.Activo)
            {
                await _emailSvc.EnviarCorreoAprobacionAsync(u.Correo, u.Nombre);
            }
        }
    }

    public async Task EliminarAsync(int id)
    {
        var u = await _db.Usuarios.FindAsync(id);
        if (u != null && u.Rol != "SuperAdmin")
        {
            _db.Usuarios.Remove(u);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> ActivarAsync(int id)
    {
        var u = await _db.Usuarios.FindAsync(id);
        if (u == null) return false;

        bool estadoAnterior = u.Activo;
        u.Activo = true;

        if (!estadoAnterior)
        {
            _db.BitacorasUsuario.Add(new BitacoraUsuario
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

        await _db.SaveChangesAsync();
        await _emailSvc.EnviarCorreoAprobacionAsync(u.Correo, u.Nombre);
        return true;
    }

    // ===== ASIGNACION DE MATRICES =====

    public async Task<List<UsuarioMatriz>> GetMatricesAsignadasAsync(int usuarioId) =>
        await _db.UsuarioMatrices
            .Include(um => um.MatrizGrupo)
            .Where(um => um.UsuarioId == usuarioId)
            .ToListAsync();

    public async Task<List<int>> GetIdsMatricesAsignadasAsync(int usuarioId) =>
        await _db.UsuarioMatrices
            .Where(um => um.UsuarioId == usuarioId)
            .Select(um => um.MatrizGrupoId)
            .ToListAsync();

    public async Task AsignarMatrizAsync(int usuarioId, int matrizId)
    {
        var existe = await _db.UsuarioMatrices.AnyAsync(
            um => um.UsuarioId == usuarioId && um.MatrizGrupoId == matrizId);
        if (!existe)
        {
            _db.UsuarioMatrices.Add(new UsuarioMatriz
            {
                UsuarioId = usuarioId,
                MatrizGrupoId = matrizId
            });
            await _db.SaveChangesAsync();
        }
    }

    public async Task QuitarMatrizAsync(int usuarioId, int matrizId)
    {
        var um = await _db.UsuarioMatrices.FirstOrDefaultAsync(
            x => x.UsuarioId == usuarioId && x.MatrizGrupoId == matrizId);
        if (um != null)
        {
            _db.UsuarioMatrices.Remove(um);
            await _db.SaveChangesAsync();
        }
    }

    public async Task QuitarTodasMatricesAsync(int usuarioId)
    {
        var lista = await _db.UsuarioMatrices
            .Where(um => um.UsuarioId == usuarioId).ToListAsync();
        _db.UsuarioMatrices.RemoveRange(lista);
        await _db.SaveChangesAsync();
    }
}