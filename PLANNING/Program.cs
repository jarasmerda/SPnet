using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Planning.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' není nakonfigurován.");

builder.Services.AddDbContext<PlanningDbContext>(options =>
    options.UseSqlServer(connectionString));

// Sdílené Data Protection klíče – stejná složka a ApplicationName ve všech SP projektech
var keysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? throw new InvalidOperationException("DataProtection:KeysPath není nakonfigurován.");

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("SP_INTRANET");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SignInManager<IdentityUser>>();
builder.Services.AddScoped<UserManager<IdentityUser>>();

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
    .AddEntityFrameworkStores<PlanningDbContext>()
    .AddDefaultTokenProviders();

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
        options.Cookie.Name = ".SP.Identity";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Startup seed
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PlanningDbContext>();

    // Vytvoří Identity tabulky pokud neexistují.
    // Pokud DB již obsahuje jiné tabulky (RestAPI1), EnsureCreated() selže – proto CreateTablesAsync().
    // SqlException 2714 = tabulka již existuje (vytvořil CRM) → v pořádku, pokračujeme dál.
    var tableExists = await db.Database
        .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.tables WHERE name = 'AspNetUsers'")
        .FirstOrDefaultAsync() > 0;

    if (!tableExists)
    {
        var creator = db.GetService<IRelationalDatabaseCreator>();
        await creator.CreateTablesAsync();
        Console.WriteLine("[SEED] Identity tabulky vytvořeny.");
    }
    else
    {
        Console.WriteLine("[SEED] Identity tabulky již existují (sdílené s CRM).");
    }

    var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    if (await um.FindByEmailAsync("cs@staroplastic.cz") == null)
    {
        var seedUser = new IdentityUser
        {
            UserName = "cs@staroplastic.cz",
            Email = "cs@staroplastic.cz",
            EmailConfirmed = true
        };
        seedUser.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<IdentityUser>()
            .HashPassword(seedUser, "cs");
        await um.CreateAsync(seedUser);
        Console.WriteLine("[SEED] Uživatel cs@staroplastic.cz byl vytvořen.");
    }
}

app.UseAuthentication();
app.UseAuthorization();

// Ochrana index.html a kořenové cesty
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
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

// GET /login
app.MapGet("/login", async (HttpContext ctx) =>
{
    var returnUrl = ctx.Request.Query["returnUrl"].FirstOrDefault() ?? "/index.html";
    var html = $@"
<!DOCTYPE html>
<html lang=""cs"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Přihlášení – PLANNING</title>
    <style>
        body {{ font-family: Arial, sans-serif; background: #f0f4f8; margin: 0; display: flex; justify-content: center; align-items: center; min-height: 100vh; }}
        .form-container {{ background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.15); width: 100%; max-width: 400px; }}
        h1 {{ color: #1565c0; text-align: center; margin-bottom: 30px; }}
        label {{ display: block; margin: 10px 0 5px; font-weight: bold; }}
        input {{ width: 100%; padding: 12px; font-size: 16px; border: 1px solid #ddd; border-radius: 6px; box-sizing: border-box; }}
        button {{ width: 100%; padding: 14px; background: #1565c0; color: white; border: none; border-radius: 6px; font-size: 16px; cursor: pointer; margin-top: 20px; }}
        button:hover {{ background: #0d47a1; }}
    </style>
</head>
<body>
    <div class=""form-container"">
        <h1>PLANNING</h1>
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

// POST /login
app.MapPost("/login", async (SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, HttpContext ctx) =>
{
    try
    {
        string? email = null;
        string? password = null;
        string returnUrl = ctx.Request.Query["returnUrl"].FirstOrDefault() ?? "/index.html";

        var contentType = ctx.Request.ContentType?.ToLowerInvariant() ?? "";

        if (contentType.Contains("application/json"))
        {
            var json = await ctx.Request.ReadFromJsonAsync<Dictionary<string, string>>();
            if (json == null) return Results.BadRequest("Neplatný JSON formát");
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
            return Results.Content("<html><body><p>Email a heslo jsou povinné.</p><a href='/login'>Zpět</a></body></html>", "text/html; charset=utf-8");

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return Results.Content("<html><body><p>Neplatný email nebo heslo.</p><a href='/login'>Zpět</a></body></html>", "text/html; charset=utf-8");

        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);

        return result.Succeeded
            ? Results.Redirect(returnUrl)
            : Results.Content("<html><body><p style='color:red'>Neplatné přihlašovací údaje.</p><a href='/login'>Zpět</a></body></html>", "text/html; charset=utf-8");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LOGIN ERROR] {ex.Message}");
        return Results.Content("<html><body><p>Interní chyba serveru.</p></body></html>", "text/html; charset=utf-8", statusCode: 500);
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();

// API endpoints
app.MapOpenOrders();

app.MapGet("/api/config", (IConfiguration config) => Results.Ok(new
{
    apiBase = config["App:PlanningApiBaseUrl"] ?? ""
}));

app.Run();

public class PlanningDbContext(DbContextOptions<PlanningDbContext> options)
    : IdentityDbContext<IdentityUser>(options);
