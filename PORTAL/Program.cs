var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Vrátí URL jednotlivých apps – v PROD z konfigurace, v DEV dynamicky dle hostname requestu
app.MapGet("/api/apps", (IConfiguration config, HttpRequest request) =>
{
    var isProd = app.Environment.IsProduction();
    if (isProd)
    {
        return Results.Ok(new
        {
            crm      = config["Apps:CrmUrl"]      ?? "",
            planning = config["Apps:PlanningUrl"] ?? ""
        });
    }
    // DEV: použij hostname z requestu (funguje pro localhost i LAN IP)
    var host = request.Host.Host;
    return Results.Ok(new
    {
        crm      = $"http://{host}:6127",
        planning = $"http://{host}:6010"
    });
});

app.Run();
