using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestAPI1.Endpoints;
using RestAPI1.Models;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ────────────────────────────────────────────────
// Logování
// ────────────────────────────────────────────────
builder.Logging.AddConsole(options => options.IncludeScopes = true);
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// ────────────────────────────────────────────────
// Swagger / OpenAPI
// ────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RestAPI1", Version = "v1" });
});

// ────────────────────────────────────────────────
// DbContext (jen jednou!)
// ────────────────────────────────────────────────
builder.Services.AddDbContext<BomDb>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Chybí konfigurace 'ConnectionStrings:DefaultConnection'. " +
            "Nastav ji v appsettings.Development.json nebo jako environment proměnnou.");

    options.UseSqlServer(cs, sqlOptions => sqlOptions.EnableRetryOnFailure());

    if (builder.Configuration.GetValue<bool>("Database:EnableSensitiveDataLogging"))
        options.EnableSensitiveDataLogging().EnableDetailedErrors();
});

// ────────────────────────────────────────────────
// Další služby
// ────────────────────────────────────────────────
builder.WebHost.UseUrls("http://0.0.0.0:5005");

builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ────────────────────────────────────────────────
// Middleware pipeline – důležité pořadí!
// ────────────────────────────────────────────────
app.UseCors("AllowAll");

// Swagger UI (jen v Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RestAPI1 v1");
        c.RoutePrefix = "swagger";
    });
}

// ────────────────────────────────────────────────
// Načtení pravidel při startu aplikace
// ────────────────────────────────────────────────
await using var scope = app.Services.CreateAsyncScope();
var dbStartup = scope.ServiceProvider.GetRequiredService<BomDb>();
int rulesCount = 0;
try
{
    var rules = await dbStartup.AttributesRules.ToListAsync();
    rulesCount = rules.Count;
    Console.WriteLine($"[START] Načteno {rulesCount} pravidel z databáze");
}
catch (Exception ex)
{
    Console.WriteLine($"[START] Tabulka AttributesRules nenalezena, pokračuji s 0 pravidly. ({ex.Message})");
}

// ────────────────────────────────────────────────
// Registrace endpointů
// ────────────────────────────────────────────────

// Root (jednoduchý text)
app.MapGet("/", () => "RestAPI1 běží na portu 5005");

// Tvé endpointy (všechny bez parametrů kromě Status)
app.MapBomEndpointsVZOR();
app.MapIndexQuotesList();
app.MapQuoteNumber();
app.MapQuoteAddItemManually();
app.MapNextQuote();
app.MapQuoteCostCalcAndCheck();
app.MapQuotePohodaItemDetail();
app.MapCpqFilterValues();
app.MapCpqItemPohodaCheck();
app.MapCpqNextCode();
app.MapQuoteCustomers();
app.MapCpqItemCreationPohoda();
app.MapQuoteNumberSendToPohoda();
app.MapCpqAttrValues();
app.MapcheckOffer();
app.MapOfferFromQuote();
app.MapOrdersFindIdsByAttrsPohoda();
app.MapOfferNextItemCodePohoda();
app.MapOrderCreationInPohoda();
app.MapIndexInquiriesList();
app.MapInquiryNumber();
app.MapNextInquiry();
app.MapInquiryCustomers();



// Status – předáváme rulesCount
app.MapApiStatus(rulesCount);

// ────────────────────────────────────────────────
// Výpis všech endpointů při startu (jen Development)
// ────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
    Console.WriteLine("\n=== Registrované endpointy ===\n");
    foreach (var endpoint in endpointDataSource.Endpoints)
    {
        if (endpoint is RouteEndpoint route)
        {
            var methods = route.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods ?? new[] { "ANY" };
            Console.WriteLine($"  {string.Join(", ", methods),-8} {route.RoutePattern.RawText,-50}");
        }
    }
    Console.WriteLine("\n==============================");
}

Console.WriteLine("\nSTART APLIKACE – " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\n");

app.Run();