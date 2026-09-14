using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RiesgosElor.Data;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

// ─── CLASES Y LOGICA INTERNA DE PERSONAL PARA AUTH ───────────────────────────
// Separadas en una clase helper file-scoped para evitar conflicto con
// RiesgosElor.Models.PersonalCargo que usa el buscador de Usuarios.razor
file static class PersonalCargoAuthHelper
{
    private const string ApiUrl =
        "https://www1.elor.com.pe/ApiPersonalCargo/api/Persona/ListarPersonaCargo";

    private static List<PersonaCargoInterno>? _cache;
    private static DateTime _cacheExpira = DateTime.MinValue;
    private static readonly object _lock = new();

    public static async Task<List<PersonaCargoInterno>> ObtenerAsync(HttpClient http)
    {
        lock (_lock)
        {
            if (_cache != null && DateTime.Now < _cacheExpira)
                return _cache;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var response = await http.GetAsync(ApiUrl, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cts.Token);
                var data = JsonSerializer.Deserialize<RespuestaInterna>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data?.Status == true && data.Value?.Any() == true)
                {
                    lock (_lock)
                    {
                        _cache = data.Value;
                        _cacheExpira = DateTime.Now.AddHours(1);
                    }
                    Console.WriteLine($"[ApiPersonalCargo] {_cache.Count} registros cargados.");
                    return _cache;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiPersonalCargo] Error: {ex.Message}");
        }

        lock (_lock) { return _cache ?? new List<PersonaCargoInterno>(); }
    }

    public static string BuscarCargo(List<PersonaCargoInterno> lista, string nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto) || !lista.Any()) return "";

        var buscar = Normalizar(nombreCompleto);
        var palabras = buscar.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // 1. Exacto
        foreach (var p in lista)
        {
            if (Normalizar(p.NombreCompleto) == buscar ||
                Normalizar(p.NombreCompletoAlt) == buscar)
            {
                Console.WriteLine($"[ApiPersonalCargo] Exacto: '{p.Cargo}'");
                return p.Cargo;
            }
        }

        // 2. Apellidos + nombre
        foreach (var p in lista)
        {
            var pat = Normalizar(p.ApellidoPaterno);
            var mat = Normalizar(p.ApellidoMaterno);
            var nom = Normalizar(p.Nombre);
            if (palabras.Contains(pat) && palabras.Contains(mat) &&
                palabras.Any(w => nom.Contains(w)))
            {
                Console.WriteLine($"[ApiPersonalCargo] Apellidos: '{p.Cargo}'");
                return p.Cargo;
            }
        }

        // 3. Parcial
        PersonaCargoInterno? mejor = null;
        int mejorHits = 0;
        foreach (var p in lista)
        {
            var nc = Normalizar(p.NombreCompleto);
            var nca = Normalizar(p.NombreCompletoAlt);
            int hits = palabras.Count(w => nc.Contains(w) || nca.Contains(w));
            if (hits >= Math.Min(3, palabras.Length) && hits > mejorHits)
            {
                mejorHits = hits; mejor = p;
            }
        }
        if (mejor != null)
        {
            Console.WriteLine($"[ApiPersonalCargo] Parcial: '{mejor.Cargo}'");
            return mejor.Cargo;
        }

        // 4. Apellido + primer nombre
        foreach (var p in lista)
        {
            var pat = Normalizar(p.ApellidoPaterno);
            var nom = Normalizar(p.Nombre);
            if (palabras.Contains(pat) && nom.Split(' ').Any(n => palabras.Contains(n)))
            {
                Console.WriteLine($"[ApiPersonalCargo] Apellido+nom: '{p.Cargo}'");
                return p.Cargo;
            }
        }

        Console.WriteLine($"[ApiPersonalCargo] Sin coincidencia: '{buscar}'");
        return "";
    }

    public static string Normalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "";
        var sb = new StringBuilder();
        foreach (char c in texto)
        {
            if (char.IsLetter(c) || char.IsDigit(c)) sb.Append(char.ToUpper(c));
            else if (char.IsWhiteSpace(c)) sb.Append(' ');
        }
        return Regex.Replace(sb.ToString().Trim(), @"\s+", " ");
    }
}

file class PersonaCargoInterno
{
    public string Nombre { get; set; } = "";
    public string ApellidoPaterno { get; set; } = "";
    public string ApellidoMaterno { get; set; } = "";
    public string CodigoCargo { get; set; } = "";
    public string Cargo { get; set; } = "";

    public string NombreCompleto =>
        $"{ApellidoPaterno} {ApellidoMaterno} {Nombre}".Trim().ToUpper();
    public string NombreCompletoAlt =>
        $"{Nombre} {ApellidoPaterno} {ApellidoMaterno}".Trim().ToUpper();
}

file class RespuestaInterna
{
    public bool Status { get; set; }
    public List<PersonaCargoInterno> Value { get; set; } = new();
    public string Msg { get; set; } = "";
}

// ─── AUTH RESULT ─────────────────────────────────────────────────────────────
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

// ─── AUTH SERVICE ─────────────────────────────────────────────────────────────
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
            await db.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id=OBJECT_ID('Usuarios') AND name='Username')
BEGIN ALTER TABLE Usuarios ADD Username NVARCHAR(MAX) NOT NULL DEFAULT ''; END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id=OBJECT_ID('Usuarios') AND name='Cargo')
BEGIN ALTER TABLE Usuarios ADD Cargo NVARCHAR(MAX) NOT NULL DEFAULT ''; END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id=OBJECT_ID('Usuarios') AND name='Gerencia')
BEGIN ALTER TABLE Usuarios ADD Gerencia NVARCHAR(MAX) NOT NULL DEFAULT ''; END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id=OBJECT_ID('Usuarios') AND name='Departamento')
BEGIN ALTER TABLE Usuarios ADD Departamento NVARCHAR(MAX) NOT NULL DEFAULT ''; END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id=OBJECT_ID('Usuarios') AND name='UltimaSincronizacionApi')
BEGIN ALTER TABLE Usuarios ADD UltimaSincronizacionApi DATETIME2 NULL; END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id=OBJECT_ID('Usuarios') AND name='FotoUrl')
BEGIN ALTER TABLE Usuarios ADD FotoUrl NVARCHAR(MAX) NOT NULL DEFAULT ''; END
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='BitacorasUsuario')
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
        CONSTRAINT FK_BitacorasUsuario_Usuarios
            FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE CASCADE
    );
END");
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
            using var request = new HttpRequestMessage(HttpMethod.Post,
                "http://www1.elor.com.pe/WSToken/api/Login");
            request.Headers.Add("x-api-key", "1");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { Usuario = usuarioOrCorreo, Password = password }),
                Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var response = await _httpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);
            Console.WriteLine($"[WSToken] {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var codResp = root.TryGetProperty("CodResp", out var cp)
                    ? cp.GetString() ?? "" : "";
                Console.WriteLine($"[WSToken] CodResp='{codResp}'");

                if (codResp != "2")
                    return await FallbackLocalAsync(usuarioOrCorreo, password, result);

                if (!root.TryGetProperty("Data", out var data) ||
                    data.ValueKind == JsonValueKind.Null)
                    return await FallbackLocalAsync(usuarioOrCorreo, password, result);

                var nombre = GetStr(data, "Fullname", "fullname", "NombreCompleto", "Nombre");
                var correo = GetStr(data, "Email", "email", "Correo", "Mail");
                var usernameApi = GetStr(data, "Usuario", "Username", "User", "CodUsuario");

                if (string.IsNullOrWhiteSpace(nombre))
                    nombre = GetJsonProp(root, "Fullname", "fullname", "NombreCompleto", "Nombre");
                if (string.IsNullOrWhiteSpace(correo))
                    correo = GetJsonProp(root, "Email", "email", "Correo", "Mail");
                if (string.IsNullOrWhiteSpace(usernameApi))
                    usernameApi = GetJsonProp(root, "Usuario", "Username", "User");

                if (root.TryGetProperty("Resp", out var jwtProp))
                {
                    var jwt = jwtProp.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(jwt) && !jwt.StartsWith("Error"))
                    {
                        var jd = DecodeJwtPayload(jwt);
                        if (jd != null)
                        {
                            if (string.IsNullOrWhiteSpace(nombre))
                                nombre = GetStr(jd.Value, "fullname", "unique_name", "name");
                            if (string.IsNullOrWhiteSpace(correo))
                                correo = GetStr(jd.Value, "email", "correo");
                            if (string.IsNullOrWhiteSpace(usernameApi))
                                usernameApi = GetStr(jd.Value, "usuario", "unique_name");
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(nombre)) nombre = usuarioOrCorreo;
                if (string.IsNullOrWhiteSpace(correo))
                    correo = usuarioOrCorreo.Contains("@") ? usuarioOrCorreo
                           : $"{usuarioOrCorreo}@elor.com.pe";
                if (string.IsNullOrWhiteSpace(usernameApi)) usernameApi = usuarioOrCorreo;

                var lista = await PersonalCargoAuthHelper.ObtenerAsync(_httpClient);
                var cargo = PersonalCargoAuthHelper.BuscarCargo(lista, nombre);

                Console.WriteLine($"[WSToken] Nombre='{nombre}' Cargo='{cargo}'");
                result.Nombre = nombre; result.Correo = correo;
                result.Username = usernameApi; result.Cargo = cargo;

                var userLocal =
                    await _db.Usuarios.FirstOrDefaultAsync(u =>
                        u.Username == usernameApi && u.Username != "") ??
                    await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo) ??
                    await _db.Usuarios.FirstOrDefaultAsync(u =>
                        u.Correo == usuarioOrCorreo || u.Username == usuarioOrCorreo);

                if (userLocal != null)
                {
                    if (!string.IsNullOrWhiteSpace(nombre)) userLocal.Nombre = nombre;
                    if (!string.IsNullOrWhiteSpace(usernameApi)) userLocal.Username = usernameApi;
                    if (!string.IsNullOrWhiteSpace(cargo)) userLocal.Cargo = cargo;
                    if (!string.IsNullOrWhiteSpace(correo) && correo != userLocal.Correo)
                    {
                        bool enUso = await _db.Usuarios.AnyAsync(u =>
                            u.Correo == correo && u.Id != userLocal.Id);
                        if (!enUso) userLocal.Correo = correo;
                    }
                    userLocal.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    userLocal.UltimaSincronizacionApi = DateTime.Now;
                    await _db.SaveChangesAsync();

                    result.Type = userLocal.Activo
                        ? AuthResultType.Success : AuthResultType.PendingApproval;
                    result.User = userLocal;
                    return result;
                }
                else
                {
                    if (correo.Equals("superadmin@gmail.com", StringComparison.OrdinalIgnoreCase) ||
                        correo.Equals("admin@gmail.com", StringComparison.OrdinalIgnoreCase))
                    {
                        var seed = new Usuario
                        {
                            Nombre = nombre,
                            Correo = correo,
                            Username = usernameApi,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                            Rol = correo.Contains("superadmin") ? "SuperAdmin" : "Admin",
                            Activo = true,
                            Cargo = cargo,
                            FechaCreacion = DateTime.Now
                        };
                        _db.Usuarios.Add(seed);
                        await _db.SaveChangesAsync();
                        result.Type = AuthResultType.Success;
                        result.User = seed;
                        return result;
                    }
                    result.Type = AuthResultType.FirstTimeNeedsConfirmation;
                    return result;
                }
            }
        }
        catch (Exception ex) { Console.WriteLine($"[WSToken] Error: {ex.Message}"); }

        return await FallbackLocalAsync(usuarioOrCorreo, password, result);
    }

    private async Task<AuthResult> FallbackLocalAsync(
        string usuario, string password, AuthResult result)
    {
        var u = await _db.Usuarios.FirstOrDefaultAsync(x =>
            x.Correo == usuario || x.Username == usuario);
        if (u != null && BCrypt.Net.BCrypt.Verify(password, u.PasswordHash))
        {
            result.Type = u.Activo ? AuthResultType.Success : AuthResultType.PendingApproval;
            result.User = u;
            return result;
        }
        result.Type = AuthResultType.InvalidCredentials;
        return result;
    }

    public async Task<bool> RegistrarSolicitudPrimerIngresoAsync(
        string usuarioOrCorreo, string password)
    {
        if (string.IsNullOrWhiteSpace(usuarioOrCorreo) ||
            string.IsNullOrWhiteSpace(password)) return false;

        await EnsureUsuarioColumnsExistAsync(_db);
        usuarioOrCorreo = usuarioOrCorreo.Trim();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                "http://www1.elor.com.pe/WSToken/api/Login");
            request.Headers.Add("x-api-key", "1");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { Usuario = usuarioOrCorreo, Password = password }),
                Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode) return false;

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var codResp = root.TryGetProperty("CodResp", out var cp)
                ? cp.GetString() ?? "" : "";
            if (codResp != "2") return false;

            if (!root.TryGetProperty("Data", out var data) ||
                data.ValueKind == JsonValueKind.Null) return false;

            var nombre = GetStr(data, "Fullname", "fullname", "NombreCompleto", "Nombre");
            var correo = GetStr(data, "Email", "email", "Correo", "Mail");
            var username = GetStr(data, "Usuario", "Username", "User", "CodUsuario");

            if (string.IsNullOrWhiteSpace(nombre))
                nombre = GetJsonProp(root, "Fullname", "fullname", "Nombre");
            if (string.IsNullOrWhiteSpace(correo))
                correo = GetJsonProp(root, "Email", "email", "Correo");
            if (string.IsNullOrWhiteSpace(username))
                username = GetJsonProp(root, "Usuario", "Username");

            if (root.TryGetProperty("Resp", out var jwtProp))
            {
                var jwt = jwtProp.GetString() ?? "";
                if (!string.IsNullOrWhiteSpace(jwt) && !jwt.StartsWith("Error"))
                {
                    var jd = DecodeJwtPayload(jwt);
                    if (jd != null)
                    {
                        if (string.IsNullOrWhiteSpace(nombre))
                            nombre = GetStr(jd.Value, "fullname", "unique_name");
                        if (string.IsNullOrWhiteSpace(correo))
                            correo = GetStr(jd.Value, "email");
                        if (string.IsNullOrWhiteSpace(username))
                            username = GetStr(jd.Value, "usuario", "unique_name");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(nombre)) nombre = usuarioOrCorreo;
            if (string.IsNullOrWhiteSpace(correo))
                correo = usuarioOrCorreo.Contains("@") ? usuarioOrCorreo
                       : $"{usuarioOrCorreo}@elor.com.pe";
            if (string.IsNullOrWhiteSpace(username)) username = usuarioOrCorreo;

            var lista = await PersonalCargoAuthHelper.ObtenerAsync(_httpClient);
            var cargo = PersonalCargoAuthHelper.BuscarCargo(lista, nombre);

            var existente =
                await _db.Usuarios.FirstOrDefaultAsync(u =>
                    u.Username == username && u.Username != "") ??
                await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo) ??
                await _db.Usuarios.FirstOrDefaultAsync(u =>
                    u.Correo == usuarioOrCorreo || u.Username == usuarioOrCorreo);

            if (existente != null)
            {
                existente.Activo = false;
                existente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                if (!string.IsNullOrWhiteSpace(nombre)) existente.Nombre = nombre;
                if (!string.IsNullOrWhiteSpace(username)) existente.Username = username;
                if (!string.IsNullOrWhiteSpace(cargo)) existente.Cargo = cargo;
                if (!string.IsNullOrWhiteSpace(correo) && correo != existente.Correo)
                {
                    bool enUso = await _db.Usuarios.AnyAsync(u =>
                        u.Correo == correo && u.Id != existente.Id);
                    if (!enUso) existente.Correo = correo;
                }
                await _db.SaveChangesAsync();
                return true;
            }

            _db.Usuarios.Add(new Usuario
            {
                Nombre = nombre,
                Correo = correo,
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Rol = "Personal",
                Activo = false,
                Cargo = cargo,
                FechaCreacion = DateTime.Now
            });
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RegistrarSolicitud] Error: {ex.Message}");
            return false;
        }
    }

    public static async Task SincronizarOrganizacionGirAsync(AppDbContext db,
        string nombre, string correo, string cargo,
        string gerencia, string departamento)
    {
        if (db == null) return;
        if (string.IsNullOrWhiteSpace(gerencia) &&
            string.IsNullOrWhiteSpace(departamento) &&
            string.IsNullOrWhiteSpace(cargo)) return;

        try
        {
            MaestroArea? gerenciaArea = null;
            MaestroArea? deptoArea = null;

            if (!string.IsNullOrWhiteSpace(gerencia))
            {
                gerencia = gerencia.Trim();
                var gu = gerencia.ToUpper();
                gerenciaArea = await db.MaestroAreas.FirstOrDefaultAsync(a =>
                    a.Nombre.ToUpper() == gu ||
                    (a.Categoria == "GERENCIAS" && a.Nombre.ToUpper().Contains(gu)));

                if (gerenciaArea == null)
                {
                    int ord = await db.MaestroAreas.AnyAsync()
                        ? await db.MaestroAreas.MaxAsync(a => a.Orden) + 1 : 1;
                    gerenciaArea = new MaestroArea
                    {
                        Nombre = gerencia,
                        Categoria = "GERENCIAS",
                        Codigo = GenerarCodigo(gerencia),
                        Activo = true,
                        Orden = ord
                    };
                    db.MaestroAreas.Add(gerenciaArea);
                    await db.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrWhiteSpace(departamento))
            {
                departamento = departamento.Trim();
                var du = departamento.ToUpper();
                deptoArea = await db.MaestroAreas.FirstOrDefaultAsync(a =>
                    a.Nombre.ToUpper() == du);

                if (deptoArea == null)
                {
                    int ord = await db.MaestroAreas.AnyAsync()
                        ? await db.MaestroAreas.MaxAsync(a => a.Orden) + 1 : 1;
                    deptoArea = new MaestroArea
                    {
                        Nombre = departamento,
                        Categoria = "DEPARTAMENTOS",
                        Codigo = GenerarCodigo(departamento),
                        PadreId = gerenciaArea?.Id,
                        Activo = true,
                        Orden = ord
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
                int? areaId = deptoArea?.Id ?? gerenciaArea?.Id;
                var correoUpper = correo?.ToUpper() ?? "";
                var nombreUpper = nombre?.ToUpper() ?? "";

                var resp = await db.MaestroResponsables.FirstOrDefaultAsync(r =>
                    (!string.IsNullOrWhiteSpace(correo) && r.Correo.ToUpper() == correoUpper) ||
                    (!string.IsNullOrWhiteSpace(nombre) && r.Nombre.ToUpper() == nombreUpper));

                if (resp != null)
                {
                    if (!string.IsNullOrWhiteSpace(cargo)) resp.Cargo = cargo;
                    if (!string.IsNullOrWhiteSpace(correo)) resp.Correo = correo;
                    if (!string.IsNullOrWhiteSpace(nombre)) resp.Nombre = nombre;
                    if (areaId.HasValue) resp.AreaId = areaId.Value;
                    await db.SaveChangesAsync();
                }
                else
                {
                    int ord = await db.MaestroResponsables.AnyAsync()
                        ? await db.MaestroResponsables.MaxAsync(r => r.Orden) + 1 : 1;
                    db.MaestroResponsables.Add(new MaestroResponsable
                    {
                        Nombre = string.IsNullOrWhiteSpace(nombre) ? correo : nombre,
                        Correo = correo ?? "",
                        Cargo = string.IsNullOrWhiteSpace(cargo) ? "Personal GIR" : cargo,
                        AreaId = areaId,
                        Activo = true,
                        Orden = ord
                    });
                    await db.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SincronizarOrg] Error: {ex.Message}");
        }
    }

    public async Task<bool> CambiarPasswordAsync(int usuarioId,
        string passwordActual, string passwordNuevo)
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
        _db.Usuarios.AddRange(
            new Usuario
            {
                Nombre = "Super Admin",
                Correo = "superadmin@gmail.com",
                Username = "superadmin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"),
                Rol = "SuperAdmin",
                Activo = true
            },
            new Usuario
            {
                Nombre = "Admin",
                Correo = "admin@gmail.com",
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"),
                Rol = "Admin",
                Activo = true
            },
            new Usuario
            {
                Nombre = "Personal",
                Correo = "personal@gmail.com",
                Username = "personal",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"),
                Rol = "Personal",
                Activo = true
            }
        );
        await _db.SaveChangesAsync();
    }

    // ─── HELPERS PRIVADOS ─────────────────────────────────────────────────
    private static JsonElement? DecodeJwtPayload(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1].PadRight(
                parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(
                payload.Replace('-', '+').Replace('_', '/'));
            return JsonDocument.Parse(Encoding.UTF8.GetString(bytes)).RootElement;
        }
        catch { return null; }
    }

    private static string GetStr(JsonElement el, params string[] names)
    {
        foreach (var name in names)
            if (el.TryGetProperty(name, out var val) &&
                val.ValueKind == JsonValueKind.String)
            {
                var s = val.GetString()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
        return "";
    }

    private static string GetJsonProp(JsonElement el, params string[] names)
    {
        try
        {
            if (el.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in el.EnumerateObject())
                {
                    if (names.Any(n => string.Equals(n, prop.Name,
                        StringComparison.OrdinalIgnoreCase)))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            var s = prop.Value.GetString()?.Trim() ?? "";
                            if (!string.IsNullOrWhiteSpace(s)) return s;
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.Number)
                            return prop.Value.ToString();
                    }
                }
                foreach (var prop in el.EnumerateObject())
                    if (prop.Value.ValueKind == JsonValueKind.Object ||
                        prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var r = GetJsonProp(prop.Value, names);
                        if (!string.IsNullOrWhiteSpace(r)) return r;
                    }
            }
            else if (el.ValueKind == JsonValueKind.Array)
                foreach (var item in el.EnumerateArray())
                {
                    var r = GetJsonProp(item, names);
                    if (!string.IsNullOrWhiteSpace(r)) return r;
                }
        }
        catch { }
        return "";
    }

    private static string GenerarCodigo(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return "ORG";
        var ac = string.Concat(
            nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length > 2 && !new[] { "del", "las", "los", "para" }
                .Contains(p.ToLower()))
            .Take(4).Select(p => char.ToUpper(p[0])));
        return string.IsNullOrWhiteSpace(ac)
            ? nombre.Substring(0, Math.Min(4, nombre.Length)).ToUpper() : ac;
    }
}