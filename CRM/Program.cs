using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// poslouchat na všech rozhraních na portu 5127
builder.WebHost.UseUrls("http://localhost:6127");

// připojení k databázi (SQLite - app.db v kořeni projektu)
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection") 
    ?? "Data Source=app.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// nutné pro SignInManager a UserManager
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SignInManager<IdentityUser>>();
builder.Services.AddScoped<UserManager<IdentityUser>>();

// Identity – základní služby (bez automatických endpointů)
builder.Services
    .AddIdentityCore<IdentityUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// autentizace – cookie
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignOutScheme = IdentityConstants.ApplicationScheme;
    })
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = ".AspNetCore.Identity.Application";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.None
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
    })
    .AddCookie(IdentityConstants.BearerScheme, options =>
    {
        options.Cookie.Name = ".AspNetCore.Identity.Bearer";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.None
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });

// autorizace
builder.Services.AddAuthorization();

var app = builder.Build();

// ────────────────────────────────────────────────
// Middleware: ochrana index.html a kořenové cesty
// POŘADÍ: UseAuthentication MUSÍ BÝT PŘED tímto middlewarem!
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    // chráníme index.html a kořen /
    if (path == "/" || path.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase))
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            var returnUrl = Uri.EscapeDataString(path + context.Request.QueryString);
            context.Response.Redirect($"/login?returnUrl={returnUrl}");
            return;
        }
    }

    await next();
});

// ────────────────────────────────────────────────
// GET /login – přihlašovací formulář
app.MapGet("/login", async (HttpContext ctx) =>
{
    var returnUrl = ctx.Request.Query["returnUrl"].FirstOrDefault() ?? "/index.html";

    var html = $@"
<!DOCTYPE html>
<html lang=""cs"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Přihlášení do HDPE CRM</title>
    <style>
        body {{ font-family: Arial, sans-serif; background: #f8f9fa; margin: 0; padding: 0; display: flex; justify-content: center; align-items: center; min-height: 100vh; }}
        .form-container {{ background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.15); width: 100%; max-width: 400px; }}
        h1 {{ color: #d32f2f; text-align: center; margin-bottom: 30px; }}
        label {{ display: block; margin: 10px 0 5px; font-weight: bold; }}
        input {{ width: 100%; padding: 12px; font-size: 16px; border: 1px solid #ddd; border-radius: 6px; box-sizing: border-box; }}
        button {{ width: 100%; padding: 14px; background: #d32f2f; color: white; border: none; border-radius: 6px; font-size: 16px; cursor: pointer; margin-top: 20px; }}
        button:hover {{ background: #b71c1c; }}
    </style>
</head>
<body>
    <div class=""form-container"">
        <h1>Přihlášení</h1>
        <form method=""POST"" action=""/login"">
            <input type=""hidden"" name=""returnUrl"" value=""{returnUrl}"" />
            <label for=""email"">Email:</label>
            <input type=""email"" id=""email"" name=""email"" required autofocus />

            <label for=""password"">Heslo:</label>
            <input type=""password"" id=""password"" name=""password"" required />

            <button type=""submit"">Přihlásit se</button>
        </form>
    </div>
</body>
</html>";

    ctx.Response.ContentType = "text/html; charset=utf-8";
    await ctx.Response.WriteAsync(html);
});

// ────────────────────────────────────────────────
// POST /login – univerzální verze (formulář + JSON)
app.MapPost("/login", async (SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, HttpContext ctx) =>
{
    try
    {
        string email = null;
        string password = null;
        string returnUrl = ctx.Request.Query["returnUrl"].FirstOrDefault() ?? "/index.html";

        var contentType = ctx.Request.ContentType?.ToLowerInvariant() ?? "";

        if (contentType.Contains("application/json"))
        {
            var json = await ctx.Request.ReadFromJsonAsync<Dictionary<string, string>>();
            if (json == null)
                return Results.BadRequest("Neplatný JSON formát");

            email = json.GetValueOrDefault("email");
            password = json.GetValueOrDefault("password");
            returnUrl = json.GetValueOrDefault("returnUrl") ?? returnUrl;
        }
        else
        {
            var form = await ctx.Request.ReadFormAsync();
            email = form["email"].FirstOrDefault();
            password = form["password"].FirstOrDefault();
            returnUrl = form["returnUrl"].FirstOrDefault() ?? returnUrl;
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            return Results.Content($@"
                <!DOCTYPE html>
                <html><body style='text-align:center; padding:50px; font-family:Arial;'>
                    <h1>Chyba přihlášení</h1>
                    <p>Email a heslo jsou povinné.</p>
                    <a href='/login?returnUrl={Uri.EscapeDataString(returnUrl)}'>Zpět na přihlášení</a>
                </body></html>");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            return Results.Content($@"
                <!DOCTYPE html>
                <html><body style='text-align:center; padding:50px; font-family:Arial;'>
                    <h1>Chyba přihlášení</h1>
                    <p>Neplatný email nebo heslo.</p>
                    <a href='/login?returnUrl={Uri.EscapeDataString(returnUrl)}'>Zpět na přihlášení</a>
                </body></html>");
        }

        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return Results.Redirect(returnUrl);
        }

        ctx.Response.ContentType = "text/html; charset=utf-8";
        return Results.Content($@"
            <!DOCTYPE html>
            <html><body style='text-align:center; padding:50px; font-family:Arial;'>
                <h1>Chyba přihlášení</h1>
                <p style='color:red;'>Neplatné přihlašovací údaje. Zkuste to znovu.</p>
                <a href='/login?returnUrl={Uri.EscapeDataString(returnUrl)}'>Zpět na přihlášení</a>
            </body></html>");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LOGIN ERROR] {ex.Message}\n{ex.StackTrace}");
        ctx.Response.ContentType = "text/html; charset=utf-8";
        return Results.Content($@"
            <!DOCTYPE html>
            <html><body style='text-align:center; padding:50px; font-family:Arial;'>
                <h1>Interní chyba serveru</h1>
                <p>Zkuste to později nebo kontaktujte administrátora.</p>
            </body></html>", statusCode: 500);
    }
});

// ────────────────────────────────────────────────
app.UseDefaultFiles();      // vrací index.html pro /
app.UseStaticFiles();       // wwwroot

app.UseHttpsRedirection();

// ────────────────────────────────────────────────
// POŘADÍ JE KLÍČOVÉ: UseAuthentication a UseAuthorization MUSÍ BÝT PŘED middlewarem ochrany!
app.UseAuthentication();
app.UseAuthorization();

// testovací chráněný endpoint
app.MapGet("/secret", () => "Toto vidí jen přihlášení uživatelé!")
   .RequireAuthorization();

// příklad dalšího endpointu (volitelné)
app.MapGet("/api/hello", () => new { message = "Ahoj světe" });

// konfigurace pro frontend – vrátí URL backendu dle prostředí
app.MapGet("/api/config", (IConfiguration config) => Results.Ok(new
{
    apiBase = config["App:RestApi1BaseUrl"] ?? ""
}));

app.Run();

// DbContext
public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}