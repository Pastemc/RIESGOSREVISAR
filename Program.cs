using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using RiesgosElor.Components;
using RiesgosElor.Data;
using RiesgosElor.Services;

var builder = WebApplication.CreateBuilder(args);

// Permitir conexiones desde cualquier IP de la red local (ej. 10.117.8.204)
builder.WebHost.UseUrls("http://0.0.0.0:5000", "http://0.0.0.0:5266");

if (builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

if (builder.Environment.IsDevelopment())
{
    var keysDirectory = new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys"));
    keysDirectory.Create();

    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(keysDirectory);
}

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .ConfigureWarnings(w => w.Ignore(
        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddScoped<AppDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

builder.Services.AddHttpClient();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<RiesgoService>();
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<GrcService>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/login";
        opt.LogoutPath = "/do-logout";
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.Cookie.SameSite = SameSiteMode.Lax;
        opt.Cookie.SecurePolicy = CookieSecurePolicy.None;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch
    {
        db.Database.EnsureCreated();
    }
    var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
    await auth.SeedAsync();
    var riesgoSvc = scope.ServiceProvider.GetRequiredService<RiesgoService>();
    await riesgoSvc.SeedMatricesAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/do-login", async (HttpContext ctx, AuthService authSvc) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var correo = form["correo"].ToString();
    var password = form["password"].ToString();

    var res = await authSvc.ProcesarLoginAsync(correo, password);

    if (res.Type == AuthResultType.InvalidCredentials)
    {
        return Results.Redirect("/login?error=1");
    }
    else if (res.Type == AuthResultType.PendingApproval)
    {
        return Results.Redirect("/login?status=pending");
    }
    else if (res.Type == AuthResultType.FirstTimeNeedsConfirmation)
    {
        string encU = Convert.ToBase64String(Encoding.UTF8.GetBytes(correo));
        string encP = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
        string encN = Convert.ToBase64String(Encoding.UTF8.GetBytes(res.Nombre));
        return Results.Redirect($"/login?first_time=1&u={Uri.EscapeDataString(encU)}&p={Uri.EscapeDataString(encP)}&name={Uri.EscapeDataString(encN)}");
    }

    var user = res.User;
    if (user == null || !user.Activo)
    {
        return Results.Redirect("/login?status=pending");
    }

    var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.Name, user.Nombre),
        new(System.Security.Claims.ClaimTypes.Email, user.Correo),
        new(System.Security.Claims.ClaimTypes.Role, user.Rol),
        new("UserId", user.Id.ToString())
    };

    var identity = new System.Security.Claims.ClaimsIdentity(
        claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new System.Security.Claims.ClaimsPrincipal(identity);

    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

    return Results.Redirect("/");
});

app.MapPost("/do-solicitar-acceso", async (HttpContext ctx, AuthService authSvc) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var usuarioEnc = form["u"].ToString();
    var passEnc = form["p"].ToString();

    string usuario = "";
    string pass = "";

    try
    {
        if (!string.IsNullOrEmpty(usuarioEnc)) usuario = Encoding.UTF8.GetString(Convert.FromBase64String(usuarioEnc));
        if (!string.IsNullOrEmpty(passEnc)) pass = Encoding.UTF8.GetString(Convert.FromBase64String(passEnc));
    }
    catch { }

    if (!string.IsNullOrEmpty(usuario) && !string.IsNullOrEmpty(pass))
    {
        await authSvc.RegistrarSolicitudPrimerIngresoAsync(usuario, pass);
        return Results.Redirect("/login?status=requested");
    }

    return Results.Redirect("/login");
});

app.MapGet("/do-logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.Cookies.Delete(".AspNetCore.Cookies");
    return Results.Redirect("/login");
});

app.MapPost("/do-logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.Cookies.Delete(".AspNetCore.Cookies");
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();