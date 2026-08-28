using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RiesgosElor.Data;
using RiesgosElor.Models;

namespace RiesgosElor.Services;

public class PersonaCargo
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

public class RespuestaPersonalCargo
{
    public bool Status { get; set; }
    public List<PersonaCargo> Value { get; set; } = new();
    public string Msg { get; set; } = "";
}

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

    private static List<PersonaCargo>? _cachePersonal = null;
    private static DateTime _cacheExpira = DateTime.MinValue;

    public AuthService(AppDbContext db, HttpClient httpClient)
    {
        _db = db;
        _httpClient = httpClient;
    }

    private static string NormalizarNombre(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return "";
        var sb = new StringBuilder();
        foreach (char c in texto)
        {
            if (char.IsLetter(c) || char.IsDigit(c))
                sb.Append(char.ToUpper(c));
            else if (c == ' ' || c == '\u00A0' || char.IsWhiteSpace(c))
                sb.Append(' ');
        }
        return Regex.Replace(sb.ToString().Trim(), @"\s+", " ");
    }

    private async Task<List<PersonaCargo>> ObtenerPersonalCargoAsync()
    {
        if (_cachePersonal != null && DateTime.Now < _cacheExpira)
            return _cachePersonal;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var response = await _httpClient.GetAsync(
                "https://www1.elor.com.pe/ApiPersonalCargo/api/Persona/ListarPersonaCargo",
                cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cts.Token);
                var data = JsonSerializer.Deserialize<RespuestaPersonalCargo>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data?.Status == true && data.Value?.Any() == true)
                {
                    _cachePersonal = data.Value;
                    _cacheExpira   = DateTime.Now.AddHours(1);
                    Console.WriteLine($"[ApiPersonalCargo] Cargados {_cachePersonal.Count} registros.");

                    var pinedo = _cachePersonal.FirstOrDefault(p =>
                        p.ApellidoPaterno.ToUpper().Contains("PINEDO"));
                    if (pinedo != null)
                    {
                        Console.WriteLine($"[PersonalCargo DEBUG] PINEDO:");
                        Console.WriteLine($"  nombre='{pinedo.Nombre}'");
                        Console.WriteLine($"  paterno='{pinedo.ApellidoPaterno}'");
                        Console.WriteLine($"  materno='{pinedo.ApellidoMaterno}'");
                        Console.WriteLine($"  cargo='{pinedo.Cargo}'");
                        Console.WriteLine($"  NomCompleto='{pinedo.NombreCompleto}'");
                        Console.WriteLine($"  NomNorm='{NormalizarNombre(pinedo.NombreCompleto)}'");
                        Console.WriteLine($"  Bytes: {string.Join("-", Encoding.UTF8.GetBytes(NormalizarNombre(pinedo.NombreCompleto)).Take(40))}");
                    }

                    foreach (var p in _cachePersonal.Take(3))
                        Console.WriteLine($"[PersonalCargo] '{p.NombreCompleto}' → '{p.Cargo}'");

                    return _cachePersonal;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiPersonalCargo] Error: {ex.Message}");
        }

        return _cachePersonal ?? new List<PersonaCargo>();
    }

    private static string BuscarCargoPorNombre(List<PersonaCargo> lista, string nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto) || !lista.Any())
            return "";

        var buscar = NormalizarNombre(nombreCompleto);
        Console.WriteLine($"[ApiPersonalCargo] Buscando: '{buscar}'");
        Console.WriteLine($"[ApiPersonalCargo] Bytes: {string.Join("-", Encoding.UTF8.GetBytes(buscar).Take(40))}");

        // 1. Recorrer manualmente con StringComparison.Ordinal
        PersonaCargo? encontrado = null;
        foreach (var p in lista)
        {
            var nc  = NormalizarNombre(p.NombreCompleto);
            var nca = NormalizarNombre(p.NombreCompletoAlt);
            if (string.Compare(nc,  buscar, StringComparison.Ordinal) == 0 ||
                string.Compare(nca, buscar, StringComparison.Ordinal) == 0)
            {
                encontrado = p;
                break;
            }
        }

        if (encontrado != null)
        {
            Console.WriteLine($"[ApiPersonalCargo] ✅ Exacto: '{encontrado.Cargo}'");
            return encontrado.Cargo;
        }

        // 2. Por apellido paterno + materno + algún nombre
        var palabras = buscar.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in lista)
        {
            var pat = NormalizarNombre(p.ApellidoPaterno);
            var mat = NormalizarNombre(p.ApellidoMaterno);
            var nom = NormalizarNombre(p.Nombre);
            if (palabras.Contains(pat) &&
                palabras.Contains(mat) &&
                palabras.Any(pal => nom.Contains(pal)))
            {
                Console.WriteLine($"[ApiPersonalCargo] ✅ Por apellidos: '{p.Cargo}'");
                return p.Cargo;
            }
        }

        // 3. Por cantidad de palabras coincidentes
        PersonaCargo? mejorPersona = null;
        int mejorHits = 0;
        foreach (var p in lista)
        {
            var nc  = NormalizarNombre(p.NombreCompleto);
            var nca = NormalizarNombre(p.NombreCompletoAlt);
            int hits = palabras.Count(pal => nc.Contains(pal) || nca.Contains(pal));
            if (hits >= Math.Min(3, palabras.Length) && hits > mejorHits)
            {
                mejorHits    = hits;
                mejorPersona = p;
            }
        }

        if (mejorPersona != null)
        {
            Console.WriteLine($"[ApiPersonalCargo] ✅ Parcial ({mejorHits} hits): '{mejorPersona.Cargo}'");
            return mejorPersona.Cargo;
        }

        // 4. Solo apellido paterno + primer nombre
        foreach (var p in lista)
        {
            var pat = NormalizarNombre(p.ApellidoPaterno);
            var nom = NormalizarNombre(p.Nombre);
            if (palabras.Contains(pat) &&
                nom.Split(' ').Any(n => palabras.Contains(n)))
            {
                Console.WriteLine($"[ApiPersonalCargo] ✅ Apellido+nombre: '{p.Cargo}'");
                return p.Cargo;
            }
        }

        Console.WriteLine($"[ApiPersonalCargo] ❌ Sin coincidencia para: '{buscar}'");
        var cercanos = lista
            .Where(p => palabras.Any(pal =>
                string.Compare(NormalizarNombre(p.ApellidoPaterno), pal,
                    StringComparison.Ordinal) == 0))
            .Take(5);
        foreach (var c in cercanos)
        {
            var norm = NormalizarNombre(c.NombreCompleto);
            Console.WriteLine($"  → '{norm}'");
            Console.WriteLine($"  → Bytes: {string.Join("-", Encoding.UTF8.GetBytes(norm).Take(40))}");
        }

        return "";
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
            using var request = new HttpRequestMessage(HttpMethod.Post,
                "http://www1.elor.com.pe/WSToken/api/Login");
            request.Headers.Add("x-api-key", "1");
            var payload = new { Usuario = usuarioOrCorreo, Password = password };
            request.Content = new StringContent(JsonSerializer.Serialize(payload),
                Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var response = await _httpClient.SendAsync(request, cts.Token);
            var content  = await response.Content.ReadAsStringAsync(cts.Token);

            Console.WriteLine($"[WSToken] Status: {response.StatusCode} | JSON: {content}");

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var codResp = "";
                if (root.TryGetProperty("CodResp", out var codProp))
                    codResp = codProp.GetString() ?? "";

                Console.WriteLine($"[WSToken] CodResp='{codResp}'");

                if (codResp != "2")
                {
                    Console.WriteLine($"[WSToken] Rechazado → fallback BD local.");
                    var localUser = await _db.Usuarios.FirstOrDefaultAsync(u =>
                        u.Correo == usuarioOrCorreo || u.Username == usuarioOrCorreo);
                    if (localUser != null &&
                        BCrypt.Net.BCrypt.Verify(password, localUser.PasswordHash))
                    {
                        if (!localUser.Activo)
                        {
                            result.Type = AuthResultType.PendingApproval;
                            result.User = localUser;
                            return result;
                        }
                        result.Type = AuthResultType.Success;
                        result.User = localUser;
                        return result;
                    }
                    result.Type = AuthResultType.InvalidCredentials;
                    return result;
                }

                if (!root.TryGetProperty("Data", out var data) ||
                    data.ValueKind == JsonValueKind.Null)
                {
                    Console.WriteLine("[WSToken] Data null → fallback BD local.");
                    var localUser = await _db.Usuarios.FirstOrDefaultAsync(u =>
                        u.Correo == usuarioOrCorreo || u.Username == usuarioOrCorreo);
                    if (localUser != null &&
                        BCrypt.Net.BCrypt.Verify(password, localUser.PasswordHash))
                    {
                        if (!localUser.Activo)
                        {
                            result.Type = AuthResultType.PendingApproval;
                            result.User = localUser;
                            return result;
                        }
                        result.Type = AuthResultType.Success;
                        result.User = localUser;
                        return result;
                    }
                    result.Type = AuthResultType.InvalidCredentials;
                    return result;
                }

                string nombre      = GetStr(data, "Fullname", "fullname", "NombreCompleto", "Nombre");
                string correo      = GetStr(data, "Email", "email", "Correo", "Mail");
                string usernameApi = GetStr(data, "Usuario", "Username", "User", "CodUsuario");

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
                        var jwtData = DecodeJwtPayload(jwt);
                        if (jwtData != null)
                        {
                            if (string.IsNullOrWhiteSpace(nombre))
                                nombre = GetStr(jwtData.Value, "fullname", "unique_name", "name");
                            if (string.IsNullOrWhiteSpace(correo))
                                correo = GetStr(jwtData.Value, "email", "correo");
                            if (string.IsNullOrWhiteSpace(usernameApi))
                                usernameApi = GetStr(jwtData.Value, "usuario", "unique_name");
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(nombre))      nombre      = usuarioOrCorreo;
                if (string.IsNullOrWhiteSpace(correo))
                    correo = usuarioOrCorreo.Contains("@") ? usuarioOrCorreo
                           : $"{usuarioOrCorreo}@elor.com.pe";
                if (string.IsNullOrWhiteSpace(usernameApi)) usernameApi = usuarioOrCorreo;

                var listaPersonal = await ObtenerPersonalCargoAsync();
                var cargo         = BuscarCargoPorNombre(listaPersonal, nombre);

                Console.WriteLine($"[WSToken] → Nombre='{nombre}' Correo='{correo}' Username='{usernameApi}' Cargo='{cargo}'");

                result.Nombre   = nombre;
                result.Correo   = correo;
                result.Username = usernameApi;
                result.Cargo    = cargo;

                var userLocal = await _db.Usuarios.FirstOrDefaultAsync(u =>
                    u.Username == usernameApi && u.Username != "");
                if (userLocal == null)
                    userLocal = await _db.Usuarios.FirstOrDefaultAsync(u =>
                        u.Correo == correo);
                if (userLocal == null)
                    userLocal = await _db.Usuarios.FirstOrDefaultAsync(u =>
                        u.Correo == usuarioOrCorreo || u.Username == usuarioOrCorreo);

                if (userLocal != null)
                {
                    if (!string.IsNullOrWhiteSpace(nombre))      userLocal.Nombre   = nombre;
                    if (!string.IsNullOrWhiteSpace(usernameApi)) userLocal.Username = usernameApi;
                    if (!string.IsNullOrWhiteSpace(cargo))       userLocal.Cargo    = cargo;

                    if (!string.IsNullOrWhiteSpace(correo) && correo != userLocal.Correo)
                    {
                        bool correoEnUso = await _db.Usuarios.AnyAsync(u =>
                            u.Correo == correo && u.Id != userLocal.Id);
                        if (!correoEnUso) userLocal.Correo = correo;
                    }

                    userLocal.PasswordHash            = BCrypt.Net.BCrypt.HashPassword(password);
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
                    if (correo.Equals("superadmin@gmail.com", StringComparison.OrdinalIgnoreCase) ||
                        correo.Equals("admin@gmail.com",      StringComparison.OrdinalIgnoreCase))
                    {
                        var adminSeed = new Usuario
                        {
                            Nombre        = nombre,
                            Correo        = correo,
                            Username      = usernameApi,
                            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(password),
                            Rol           = correo.Contains("superadmin") ? "SuperAdmin" : "Admin",
                            Activo        = true,
                            Cargo         = cargo,
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
                Console.WriteLine($"[WSToken] HTTP Error: {response.StatusCode}");
                result.Type = AuthResultType.InvalidCredentials;
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WSToken] Error: {ex.Message}");
        }

        var fallbackUser = await _db.Usuarios.FirstOrDefaultAsync(u =>
            u.Correo == usuarioOrCorreo || u.Username == usuarioOrCorreo);

        if (fallbackUser != null &&
            BCrypt.Net.BCrypt.Verify(password, fallbackUser.PasswordHash))
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
            using var request = new HttpRequestMessage(HttpMethod.Post,
                "http://www1.elor.com.pe/WSToken/api/Login");
            request.Headers.Add("x-api-key", "1");
            var payload = new { Usuario = usuarioOrCorreo, Password = password };
            request.Content = new StringContent(JsonSerializer.Serialize(payload),
                Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode) return false;

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var codResp = "";
            if (root.TryGetProperty("CodResp", out var codProp))
                codResp = codProp.GetString() ?? "";
            if (codResp != "2") return false;

            if (!root.TryGetProperty("Data", out var data) ||
                data.ValueKind == JsonValueKind.Null)
                return false;

            string nombre   = GetStr(data, "Fullname", "fullname", "NombreCompleto", "Nombre");
            string correo   = GetStr(data, "Email", "email", "Correo", "Mail");
            string username = GetStr(data, "Usuario", "Username", "User", "CodUsuario");

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
                    var jwtData = DecodeJwtPayload(jwt);
                    if (jwtData != null)
                    {
                        if (string.IsNullOrWhiteSpace(nombre))
                            nombre = GetStr(jwtData.Value, "fullname", "unique_name");
                        if (string.IsNullOrWhiteSpace(correo))
                            correo = GetStr(jwtData.Value, "email");
                        if (string.IsNullOrWhiteSpace(username))
                            username = GetStr(jwtData.Value, "usuario", "unique_name");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(nombre))   nombre   = usuarioOrCorreo;
            if (string.IsNullOrWhiteSpace(correo))
                correo = usuarioOrCorreo.Contains("@") ? usuarioOrCorreo
                       : $"{usuarioOrCorreo}@elor.com.pe";
            if (string.IsNullOrWhiteSpace(username)) username = usuarioOrCorreo;

            var listaPersonal = await ObtenerPersonalCargoAsync();
            var cargo         = BuscarCargoPorNombre(listaPersonal, nombre);

            var usuarioExistente = await _db.Usuarios.FirstOrDefaultAsync(u =>
                u.Username == username && u.Username != "");
            if (usuarioExistente == null)
                usuarioExistente = await _db.Usuarios.FirstOrDefaultAsync(u =>
                    u.Correo == correo);
            if (usuarioExistente == null)
                usuarioExistente = await _db.Usuarios.FirstOrDefaultAsync(u =>
                    u.Correo == usuarioOrCorreo || u.Username == usuarioOrCorreo);

            if (usuarioExistente != null)
            {
                usuarioExistente.Activo       = false;
                usuarioExistente.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                if (!string.IsNullOrWhiteSpace(nombre))   usuarioExistente.Nombre   = nombre;
                if (!string.IsNullOrWhiteSpace(username)) usuarioExistente.Username = username;
                if (!string.IsNullOrWhiteSpace(cargo))    usuarioExistente.Cargo    = cargo;
                if (!string.IsNullOrWhiteSpace(correo) && correo != usuarioExistente.Correo)
                {
                    bool enUso = await _db.Usuarios.AnyAsync(u =>
                        u.Correo == correo && u.Id != usuarioExistente.Id);
                    if (!enUso) usuarioExistente.Correo = correo;
                }
                await _db.SaveChangesAsync();
                return true;
            }

            var nuevo = new Usuario
            {
                Nombre        = nombre,
                Correo        = correo,
                Username      = username,
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword(password),
                Rol           = "Personal",
                Activo        = false,
                Cargo         = cargo,
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

    public static async Task SincronizarOrganizacionGirAsync(AppDbContext db, string nombre,
        string correo, string cargo, string gerencia, string departamento)
    {
        if (db == null) return;
        if (string.IsNullOrWhiteSpace(gerencia) && string.IsNullOrWhiteSpace(departamento) &&
            string.IsNullOrWhiteSpace(cargo))
            return;

        try
        {
            MaestroArea? gerenciaArea = null;
            MaestroArea? deptoArea    = null;

            if (!string.IsNullOrWhiteSpace(gerencia))
            {
                gerencia = gerencia.Trim();
                var gerenciaUpper = gerencia.ToUpper();
                gerenciaArea = await db.MaestroAreas.FirstOrDefaultAsync(a =>
                    a.Nombre.ToUpper() == gerenciaUpper ||
                    (a.Categoria == "GERENCIAS" && a.Nombre.ToUpper().Contains(gerenciaUpper)));

                if (gerenciaArea == null)
                {
                    int maxOrden = await db.MaestroAreas.AnyAsync()
                        ? await db.MaestroAreas.MaxAsync(a => a.Orden) + 1 : 1;
                    gerenciaArea = new MaestroArea
                    {
                        Nombre    = gerencia,
                        Categoria = "GERENCIAS",
                        Codigo    = GenerarCodigoArea(gerencia),
                        Activo    = true,
                        Orden     = maxOrden
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
                    int maxOrden = await db.MaestroAreas.AnyAsync()
                        ? await db.MaestroAreas.MaxAsync(a => a.Orden) + 1 : 1;
                    deptoArea = new MaestroArea
                    {
                        Nombre    = departamento,
                        Categoria = "DEPARTAMENTOS",
                        Codigo    = GenerarCodigoArea(departamento),
                        PadreId   = gerenciaArea?.Id,
                        Activo    = true,
                        Orden     = maxOrden
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
                var correoUpper     = correo?.ToUpper() ?? "";
                var nombreUpper     = nombre?.ToUpper() ?? "";

                var resp = await db.MaestroResponsables.FirstOrDefaultAsync(r =>
                    (!string.IsNullOrWhiteSpace(correo) && r.Correo.ToUpper() == correoUpper) ||
                    (!string.IsNullOrWhiteSpace(nombre) && r.Nombre.ToUpper() == nombreUpper));

                if (resp != null)
                {
                    if (!string.IsNullOrWhiteSpace(cargo))  resp.Cargo  = cargo;
                    if (!string.IsNullOrWhiteSpace(correo)) resp.Correo = correo;
                    if (!string.IsNullOrWhiteSpace(nombre)) resp.Nombre = nombre;
                    if (areaAsignadaId.HasValue) resp.AreaId = areaAsignadaId.Value;
                    await db.SaveChangesAsync();
                }
                else
                {
                    int maxOrdenResp = await db.MaestroResponsables.AnyAsync()
                        ? await db.MaestroResponsables.MaxAsync(r => r.Orden) + 1 : 1;
                    db.MaestroResponsables.Add(new MaestroResponsable
                    {
                        Nombre  = string.IsNullOrWhiteSpace(nombre) ? correo : nombre,
                        Correo  = correo ?? "",
                        Cargo   = string.IsNullOrWhiteSpace(cargo) ? "Personal GIR" : cargo,
                        AreaId  = areaAsignadaId,
                        Activo  = true,
                        Orden   = maxOrdenResp
                    });
                    await db.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SincronizarOrganizacionGirAsync Error]: {ex.Message}");
        }
    }

    private static JsonElement? DecodeJwtPayload(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            var json  = Encoding.UTF8.GetString(bytes);
            return JsonDocument.Parse(json).RootElement;
        }
        catch { return null; }
    }

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
                    if (propNames.Any(name =>
                        string.Equals(name, prop.Name, StringComparison.OrdinalIgnoreCase)))
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
                    if (prop.Value.ValueKind == JsonValueKind.Object ||
                        prop.Value.ValueKind == JsonValueKind.Array)
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
                && !p.Equals("del",  StringComparison.OrdinalIgnoreCase)
                && !p.Equals("las",  StringComparison.OrdinalIgnoreCase)
                && !p.Equals("los",  StringComparison.OrdinalIgnoreCase)
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
            UsuarioId            = usuarioId,
            PasswordHashAnterior = user.PasswordHash,
            PasswordHashNuevo    = BCrypt.Net.BCrypt.HashPassword(passwordNuevo)
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
            new Usuario
            {
                Nombre       = "Super Admin",
                Correo       = "superadmin@gmail.com",
                Username     = "superadmin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"),
                Rol          = "SuperAdmin",
                Activo       = true
            },
            new Usuario
            {
                Nombre       = "Admin",
                Correo       = "admin@gmail.com",
                Username     = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"),
                Rol          = "Admin",
                Activo       = true
            },
            new Usuario
            {
                Nombre       = "Personal",
                Correo       = "personal@gmail.com",
                Username     = "personal",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345678"),
                Rol          = "Personal",
                Activo       = true
            },
        };

        _db.Usuarios.AddRange(usuarios);
        await _db.SaveChangesAsync();
    }
}