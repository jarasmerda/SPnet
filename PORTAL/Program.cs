var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Vrátí URL jednotlivých apps dle prostředí – frontend je načte dynamicky
app.MapGet("/api/apps", (IConfiguration config) => Results.Ok(new
{
    crm      = config["Apps:CrmUrl"]      ?? "",
    planning = config["Apps:PlanningUrl"] ?? ""
}));

app.Run();
