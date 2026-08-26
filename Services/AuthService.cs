using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RiesgosElor.Data;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

public class AuthResult
{
    public AuthResultType Type { get; set; }
    public Usuario? User { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Username { get; set; } = "";
    public string Cargo { get; set; } = "";
    public string Gerencia { get; set; } = "";
    public string Departamento { get; set; } = "";
}

public enum AuthResultType
{
    InvalidCredentials,
    FirstTimeNeedsConfirmation,
    PendingApproval,
    Success
}

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;

    public AuthService(AppDbContext db, HttpClient httpClient)
    {
        _db = db;
        _httpClient = httpClient;
    }

    public static async Task EnsureUsuarioColumnsExistAsync(AppDbContext db)
    {
        try
        {
            string sqlScript = @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'Username')
BEGIN ALTER TABLE Usuarios ADD Username NVARCHAR(MAX) NOT NULL DEFAULT ''; END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'Cargo')
BEGIN ALTER TABLE Usuarios ADD Cargo NVARCHAR(MAX) NOT NULL DEFAULT ''; END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'Gerencia')
BEGIN ALTER TABLE Usuarios ADD Gerencia NVARCHAR(MAX) NOT NULL DEFAULT ''; END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'Departamento')
BEGIN ALTER TABLE Usuarios ADD Departamento NVARCHAR(MAX) NOT NULL DEFAULT ''; END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'UltimaSincronizacionApi')
BEGIN ALTER TABLE Usuarios ADD UltimaSincronizacionApi DATETIME2 NULL; END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'FotoUrl')
BEGIN ALTER TABLE Usuarios ADD FotoUrl NVARCHAR(MAX) NOT NULL DEFAULT ''; END
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BitacorasUsuario')
BEGIN
    CREATE TABLE BitacorasUsuario (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UsuarioId INT NOT NULL,
        ModificadoPor NVARCHAR(150) NOT NULL DEFAULT '',
        Campo NVARCHAR(100) NOT NULL DEFAULT '',
        ValorAnterior NVARCHAR(MAX) NOT NULL DEFAULT '',
        ValorNuevo NVARCHAR(MAX) NOT NULL DEFAULT '',
        Motivo NVARCHAR(MAX) NOT NULL DEFAULT '',
        FechaCambio DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_BitacorasUsuario_Usuarios FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE CASCADE
    );
END";
            await db.Database.ExecuteSqlRawAsync(sqlScript);
        }
        catch { }
    }

    public async Task<AuthResult> ProcesarLoginAsync(string usuarioOrCorreo, string password)
    {
        var result = new AuthResult { Type = AuthResultType.InvalidCredentials };

        if (string.IsNullOrWhiteSpace(usuarioOrCorreo) || string.IsNullOrWhiteSpace(password))
            return result;

        await EnsureUsuarioColumnsExistAsync(_db);
        usuarioOrCorreo = usuarioOrCorreo.Trim();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "http://www1.elor.com.pe/WSToken/api/Login");
            request.Headers.Add("x-api-key", "1");
            var payload = new { Usuario = usuarioOrCorreo, Password = password };
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var response = await _httpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            Console.WriteLine($"[WSToken] StatusCode: {response.StatusCode}");
            Console.WriteLine($"[WSToken] JSON: {content}");

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // ── ESTRUCTURA REAL: { CodResp, Resp(JWT), Data: { Usuario, Email, Fullname, CodUsuario } }
                // Leer del objeto Data directamente
                string nombre = "";
                string correo = "";
                string usernameApi = "";

                if (root.TryGetProperty("Data", out var data))
                {
                    nombre = GetStr(data, "Fullname", "fullname", "NombreCompleto", "Nombre");
                    correo = GetStr(data, "Email", "email", "Correo", "Mail");
                    usernameApi = GetStr(data, "Usuario", "Username", "User", "CodUsuario");
                }

                // Fallback: buscar recursivo por si cambia la estructura
                if (string.IsNullOrWhiteSpace(nombre))
                    nombre = GetJsonProp(root, "Fullname", "fullname", "NombreCompleto", "Nombre", "FullName");
                if (string.IsNullOrWhiteSpace(correo))
                    correo = GetJsonProp(root, "Email", "email", "Correo", "Mail", "EmailAddress");
                if (string.IsNullOrWhiteSpace(usernameApi))
                    usernameApi = GetJsonProp(root, "Usuario", "Username", "User", "CodUsuario");

                // También leer el JWT y extraer datos adicionales si los hay
                if (root.TryGetProperty("Resp", out var jwtProp))
                {
                    var jwt = jwtProp.GetString() ?? "";
                    var jwtData = DecodeJwtPayload(jwt);
                    if (jwtData != null)
                    {
                        if (string.IsNullOrWhiteSpace(nombre))
                            nombre = GetStr(jwtData.Value, "fullname", "unique_name", "name");
                        if (string.IsNullOrWhiteSpace(correo))
                            correo = GetStr(jwtData.Value, "email", "correo");
                        if (string.IsNullOrWhiteSpace(usernameApi))
                            usernameApi = GetStr(jwtData.Value, "usuario", "unique_name", "codusuario");
                    }
                }

                if (string.IsNullOrWhiteSpace(nombre)) nombre = usuarioOrCorreo;
                if (string.IsNullOrWhiteSpace(correo))
                    correo = usuarioOrCorreo.Contains("@") ? usuarioOrCorreo : $"{usuarioOrCorreo}@elor.com.pe";
                if (string.IsNullOrWhiteSpace(usernameApi)) usernameApi = usuarioOrCorreo;

                // La API NO devuelve Cargo/Gerencia/Departamento — quedan vacíos por ahora
                Console.WriteLine($"[WSToken] → Nombre='{nombre}' Correo='{correo}' Username='{usernameApi}'");

                result.Nombre = nombre;
                result.Correo = correo;
                result.Username = usernameApi;

                // ── FIX PROBLEMA 2: buscar por Username O por Correo pero sin conflicto ──
                // Primero buscar por Username (más específico)
                var userLocal = await _db.Usuarios.FirstOrDefaultAsync(u =>
                    u.Username == usernameApi && u.Username != "");

                // Si no encontró por username, buscar por correo
                if (userLocal == null)
                    userLocal = await _db.Usuarios.FirstOrDefaultAsync(u =>
                        u.Correo == correo);

                // Si tampoco, buscar por el texto que ingresó
                if (userLocal == null)
                    userLocal = await _db.Usuarios.FirstOrDefaultAsync(u =>
                        u.Correo == usuarioOrCorreo || u.Username == usuarioOrCorreo);

                if (userLocal != null)
                {
                    // Actualizar nombre y username siempre
                    if (!string.IsNullOrWhiteSpace(nombre)) userLocal.Nombre = nombre;
                    if (!string.IsNullOrWhiteSpace(usernameApi)) userLocal.Username = usernameApi;

                    // ── FIX: solo actualizar correo si NO genera duplicado ──
                    if (!string.IsNullOrWhiteSpace(correo) && correo != userLocal.Correo)
                    {
                        bool correoEnUso = await _db.Usuarios.AnyAsync(u =>
                            u.Correo == correo && u.Id != userLocal.Id);
                        if (!correoEnUso)
                            userLocal.Correo = correo;
                    }

                    userLocal.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    userLocal.UltimaSincronizacionApi = DateTime.Now;
                    await _db.SaveChangesAsync();

                    if (!userLocal.Activo)
                    {
                        result.Type = AuthResultType.PendingApproval;
                        result.User = userLocal;
                        return result;
                    }

                    result.Type = AuthResultType.Success;
                    result.User = userLocal;
                    return result;
                }
                else
                {
                    // Primera vez — correos especiales crean admin directo
                    if (correo.Equals("superadmin@gmail.com", StringComparison.OrdinalIgnoreCase) ||
                        correo.Equals("admin@gmail.com", StringComparison.OrdinalIgnoreCase))
                    {
                        var adminSeed = new Usuario
                        {
                            Nombre = nombre,
                            Correo = correo,
                            Username = usernameApi,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                            Rol = correo.Contains("superadmin") ? "SuperAdmin" : "Admin",
                            Activo = true,
                            FechaCreacion = DateTime.Now
                        };
                        _db.Usuarios.Add(adminSeed);
                        await _db.SaveChangesAsync();
                        result.Type = AuthResultType.Success;
                        result.User = adminSeed;
                        return result;
                    }

                    result.Type = AuthResultType.FirstTimeNeedsConfirmation;
                    return result;
                }
            }
            else
            {
                Console.WriteLine($"[WSToken] Credenciales inválidas. Status: {response.StatusCode}");
                result.Type = AuthResultType.InvalidCredentials;
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WSToken] Error: {ex.Message}");
        }

        // FALLBACK BD LOCAL
        var fallbackUser = await _db.Usuarios.FirstOrDefaultAsync(u =>
            u.Correo == usuarioOrCorreo || u.Username == usuarioOrCorreo);

        if (fallbackUser != null && BCrypt.Net.BCrypt.Verify(password, fallbackUser.PasswordHash))
        {
            if (!fallbackUser.Activo)
            {
                result.Type = AuthResultType.PendingApproval;
                result.User = fallbackUser;
                return result;
            }
            result.Type = AuthResultType.Success;
            result.User = fallbackUser;
            return result;
        }

        result.Type = AuthResultType.InvalidCredentials;
        return result;
    }

    public async Task<bool> RegistrarSolicitudPrimerIngresoAsync(string usuarioOrCorreo, string password)
    {
        if (string.IsNullOrWhiteSpace(usuarioOrCorreo) || string.IsNullOrWhiteSpace(password))
            return false;

        await EnsureUsuarioColumnsExistAsync(_db);
        usuarioOrCorreo = usuarioOrCorreo.Trim();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "http://www1.elor.com.pe/WSToken/api/Login");
            request.Headers.Add("x-api-key", "1");
            var payload = new { Usuario = usuarioOrCorreo, Password = password };
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode) return false;

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            string nombre = "", correo = "", username = "";

            if (root.TryGetProperty("Data", out var data))
            {
                nombre = GetStr(data, "Fullname", "fullname", "NombreCompleto", "Nombre");
                correo = GetStr(data, "Email", "email", "Correo", "Mail");
                username = GetStr(data, "Usuario", "Username", "User", "CodUsuario");
            }

            if (string.IsNullOrWhiteSpace(nombre)) nombre = GetJsonProp(root, "Fullname", "fullname", "Nombre");
            if (string.IsNullOrWhiteSpace(correo)) correo = GetJsonProp(root, "Email", "email", "Correo");
            if (string.IsNullOrWhiteSpace(username)) username = GetJsonProp(root, "Usuario", "Username");

            // Leer JWT también
            if (root.TryGetProperty("Resp", out var jwtProp))
            {
                var jwtData = DecodeJwtPayload(jwtProp.GetString() ?? "");
                if (jwtData != null)
                {
                    if (string.IsNullOrWhiteSpace(nombre)) nombre = GetStr(jwtData.Value, "fullname", "unique_name");
                    if (string.IsNullOrWhiteSpace(correo)) correo = GetStr(jwtData.Value, "email");
                    if (string.IsNullOrWhiteSpace(username)) username = GetStr(jwtData.Value, "usuario", "unique_name");
                }
            }

            if (string.IsNullOrWhiteSpace(nombre)) nombre = usuarioOrCorreo;
            if (string.IsNullOrWhiteSpace(correo))
                correo = usuarioOrCorreo.Contains("@") ? usuarioOrCorreo : $"{usuarioOrCorreo}@elor.com.pe";
            if (string.IsNullOrWhiteSpace(username)) username = usuarioOrCorreo;

            // Buscar existente por username primero, luego por correo
            var usuarioExistente = await _db.Usuarios.FirstOrDefaultAsync(u => u.Username == username && u.Username != "");
            if (usuarioExistente == null)
                usuarioExistente = await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
            if (usuarioExistente == null)
                usuarioExistente = await _db.Usuarios.FirstOrDefaultAsync(u =>
                    u.Correo == usuarioOrCorreo || u.Username == usuarioOrCorreo);

            if (usuarioExistente != null)
            {
                usuarioExistente.Activo = false;
                usuarioExistente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                if (!string.IsNullOrWhiteSpace(nombre)) usuarioExistente.Nombre = nombre;
                if (!string.IsNullOrWhiteSpace(username)) usuarioExistente.Username = username;
                if (!string.IsNullOrWhiteSpace(correo) && correo != usuarioExistente.Correo)
                {
                    bool enUso = await _db.Usuarios.AnyAsync(u => u.Correo == correo && u.Id != usuarioExistente.Id);
                    if (!enUso) usuarioExistente.Correo = correo;
                }
                await _db.SaveChangesAsync();
                return true;
            }

            var nuevo = new Usuario
            {
                Nombre = nombre,
                Correo = correo,
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Rol = "Personal",
                Activo = false,
                FechaCreacion = DateTime.Now
            };
            _db.Usuarios.Add(nuevo);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RegistrarSolicitud] Error: {ex.Message}");
            return false;
        }
    }

    // ── FIX PROBLEMA 3: SincronizarOrganizacionGirAsync sin StringComparison ──
    public static async Task SincronizarOrganizacionGirAsync(AppDbContext db, string nombre,
        string correo, string cargo, string gerencia, string departamento)
    {
        if (db == null) return;

        // Si no hay cargo/gerencia/departamento, no hay nada que sincronizar
        if (string.IsNullOrWhiteSpace(gerencia) && string.IsNullOrWhiteSpace(departamento) &&
            string.IsNullOrWhiteSpace(cargo))
            return;

        try
        {
            MaestroArea? gerenciaArea = null;
            MaestroArea? deptoArea = null;

            if (!string.IsNullOrWhiteSpace(gerencia))
            {
                gerencia = gerencia.Trim();
                var gerenciaUpper = gerencia.ToUpper();

                // ── FIX: usar ToUpper() en vez de StringComparison.OrdinalIgnoreCase ──
                gerenciaArea = await db.MaestroAreas.FirstOrDefaultAsync(a =>
                    a.Nombre.ToUpper() == gerenciaUpper ||
                    (a.Categoria == "GERENCIAS" && a.Nombre.ToUpper().Contains(gerenciaUpper)));

                if (gerenciaArea == null)
                {
                    string codigo = GenerarCodigoArea(gerencia);
                    int maxOrden = await db.MaestroAreas.AnyAsync()
                        ? await db.MaestroAreas.MaxAsync(a => a.Orden) + 1 : 1;
                    gerenciaArea = new MaestroArea
                    {
                        Nombre = gerencia,
                        Categoria = "GERENCIAS",
                        Codigo = codigo,
                        Activo = true,
                        Orden = maxOrden
                    };
                    db.MaestroAreas.Add(gerenciaArea);
                    await db.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrWhiteSpace(departamento))
            {
                departamento = departamento.Trim();
                var deptoUpper = departamento.ToUpper();

                deptoArea = await db.MaestroAreas.FirstOrDefaultAsync(a =>
                    a.Nombre.ToUpper() == deptoUpper);

                if (deptoArea == null)
                {
                    string codigo = GenerarCodigoArea(departamento);
                    int maxOrden = await db.MaestroAreas.AnyAsync()
                        ? await db.MaestroAreas.MaxAsync(a => a.Orden) + 1 : 1;
                    deptoArea = new MaestroArea
                    {
                        Nombre = departamento,
                        Categoria = "DEPARTAMENTOS",
                        Codigo = codigo,
                        PadreId = gerenciaArea?.Id,
                        Activo = true,
                        Orden = maxOrden
                    };
                    db.MaestroAreas.Add(deptoArea);
                    await db.SaveChangesAsync();
                }
                else if (deptoArea.PadreId == null && gerenciaArea != null)
                {
                    deptoArea.PadreId = gerenciaArea.Id;
                    await db.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrWhiteSpace(nombre) || !string.IsNullOrWhiteSpace(correo))
            {
                int? areaAsignadaId = deptoArea?.Id ?? gerenciaArea?.Id;
                var correoUpper = correo?.ToUpper() ?? "";
                var nombreUpper = nombre?.ToUpper() ?? "";

                // ── FIX: ToUpper() en lugar de StringComparison ──
                var resp = await db.MaestroResponsables.FirstOrDefaultAsync(r =>
                    (!string.IsNullOrWhiteSpace(correo) && r.Correo.ToUpper() == correoUpper) ||
                    (!string.IsNullOrWhiteSpace(nombre) && r.Nombre.ToUpper() == nombreUpper));

                if (resp != null)
                {
                    if (!string.IsNullOrWhiteSpace(cargo)) resp.Cargo = cargo;
                    if (!string.IsNullOrWhiteSpace(correo)) resp.Correo = correo;
                    if (!string.IsNullOrWhiteSpace(nombre)) resp.Nombre = nombre;
                    if (areaAsignadaId.HasValue) resp.AreaId = areaAsignadaId.Value;
                    await db.SaveChangesAsync();
                }
                else
                {
                    int maxOrdenResp = await db.MaestroResponsables.AnyAsync()
                        ? await db.MaestroResponsables.MaxAsync(r => r.Orden) + 1 : 1;
                    var nuevoResp = new MaestroResponsable
                    {
                        Nombre = string.IsNullOrWhiteSpace(nombre) ? correo : nombre,
                        Correo = correo ?? "",
                        Cargo = string.IsNullOrWhiteSpace(cargo) ? "Personal GIR" : cargo,
                        AreaId = areaAsignadaId,
                        Activo = true,
                        Orden = maxOrdenResp
                    };
                    db.MaestroResponsables.Add(nuevoResp);
                    await db.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SincronizarOrganizacionGirAsync Error]: {ex.Message}");
        }
    }

    // ── Decodificar JWT para extraer claims ──
    private static JsonElement? DecodeJwtPayload(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1];
            // Agregar padding si falta
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            var json = Encoding.UTF8.GetString(bytes);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        }
        catch { return null; }
    }

    // Acceso directo a campo de JsonElement (sin recursión)
    private static string GetStr(JsonElement el, params string[] names)
    {
        foreach (var name in names)
        {
            if (el.TryGetProperty(name, out var val) && val.ValueKind == JsonValueKind.String)
            {
                var s = val.GetString()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
        }
        return "";
    }

    private static string GetJsonProp(JsonElement el, params string[] propNames)
    {
        try
        {
            if (el.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in el.EnumerateObject())
                {
                    if (propNames.Any(name => string.Equals(name, prop.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            var str = prop.Value.GetString()?.Trim() ?? "";
                            if (!string.IsNullOrWhiteSpace(str)) return str;
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.Number)
                            return prop.Value.ToString();
                    }
                }
                foreach (var prop in el.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var res = GetJsonProp(prop.Value, propNames);
                        if (!string.IsNullOrWhiteSpace(res)) return res;
                    }
                }
            }
            else if (el.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in el.EnumerateArray())
                {
                    var res = GetJsonProp(item, propNames);
                    if (!string.IsNullOrWhiteSpace(res)) return res;
                }
            }
        }
        catch { }
        return "";
    }

    private static string GenerarCodigoArea(string nombreArea)
    {
        if (string.IsNullOrWhiteSpace(nombreArea)) return "ORG";
        var palabras = nombreArea.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length > 2
                && !p.Equals("del", StringComparison.OrdinalIgnoreCase)
                && !p.Equals("las", StringComparison.OrdinalIgnoreCase)
                && !p.Equals("los", StringComparison.OrdinalIgnoreCase)
                && !p.Equals("para", StringComparison.OrdinalIgnoreCase))
            .Take(4);
        var acronimo = string.Concat(palabras.Select(p => char.ToUpper(p[0])));
        return string.IsNullOrWhiteSpace(acronimo)
            ? nombreArea.Substring(0, Math.Min(4, nombreArea.Length)).ToUpper()
            : acronimo;
    }

    public async Task<bool> CambiarPasswordAsync(int usuarioId, string passwordActual, string passwordNuevo)
    {
        var user = await _db.Usuarios.FindAsync(usuarioId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(passwordActual, user.PasswordHash))
            return false;

        var cambio = new CambioPassword
        {
            UsuarioId = usuarioId,
            PasswordHashAnterior = user.PasswordHash,
            PasswordHashNuevo = BCrypt.Net.BCrypt.HashPassword(passwordNuevo)
        };
        user.PasswordHash = cambio.PasswordHashNuevo;
        _db.CambiosPassword.Add(cambio);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task SeedAsync()
    {
        if (await _db.Usuarios.AnyAsync()) return;

        var usuarios = new[]
        {
            new Usuario { Nombre = "Super Admin", Correo = "superadmin@gmail.com", Username = "superadmin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"),
                Rol = "SuperAdmin", Activo = true },
            new Usuario { Nombre = "Admin", Correo = "admin@gmail.com", Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"),
                Rol = "Admin", Activo = true },
            new Usuario { Nombre = "Personal", Correo = "personal@gmail.com", Username = "personal",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"),
                Rol = "Personal", Activo = true },
        };

        _db.Usuarios.AddRange(usuarios);
        await _db.SaveChangesAsync();
    }
}