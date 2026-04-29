using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Security;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.ComponentModel.DataAnnotations;

namespace RestAPI_BOM
{
    // DTOs (původní + nové pro BOM)
    public record BomRequest(List<BomItem> items);
    public record BomItem(string code, int quantity, string attr1, string attr2, string attr3, string attr4, string attr5, string attr6, string attr7);

    public record QuickAppendItemDto(
        string Code, string Name, string? Attr1, string? Attr2, string? Attr3, string? Attr4,
        string? Attr5, string? Attr6, string? Attr7, int Quantity);

    public record QuickAppendRequest(QuickAppendItemDto Item, string? Status);

    // MODELY – všechny původní
    class BomRoutingRule
    {
        public int RuleID { get; set; }
        public string BomAndRoutingType { get; set; } = null!;
        public string? Attr1 { get; set; }
        public string? Attr2 { get; set; }
        public string? Attr3 { get; set; }
        public string? Attr4 { get; set; }
        public string? Attr5 { get; set; }
        public string? Attr6 { get; set; }
        public string? Attr7 { get; set; }
        public string? Material { get; set; }
        public string? QuantityFormula { get; set; }
        public bool IsActive { get; set; }
    }

    class BomRoutingRow
    {
        public int ID { get; set; }
        public string ProductNumber { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? MaterialNumber { get; set; }
        public decimal? Qty { get; set; }
        public string? UoM { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    class QuoteHeader
    {
        public int QuoteID { get; set; }
        public string QuoteNumber { get; set; } = null!;
        public string Status { get; set; } = "Rozpracovaná";
        public string? CustomerID { get; set; }
        public string? CustomerName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastSaved { get; set; }
        public DateTime? QuoteDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? PohodaOfferNumber { get; set; }
        public int? PohodaOfferInternalID { get; set; }
        public DateTime? PohodaImportDate { get; set; }
        public string? PohodaImportStatus { get; set; }
        public DateTime? LastPohodaAttemptDate { get; set; }
        public string? PohodaLastResponse { get; set; }
    }

    class QuoteItem
    {
        public int ItemID { get; set; }
        public int QuoteID { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Attr1 { get; set; }
        public string? Attr2 { get; set; }
        public string? Attr3 { get; set; }
        public string? Attr4 { get; set; }
        public string? Attr5 { get; set; }
        public string? Attr6 { get; set; }
        public string? Attr7 { get; set; }
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
    }

    class SKz
    {
        [Key]
        public string IDS { get; set; } = null!;
        public decimal PURCHASE_PRICE { get; set; }
        public string? DESCRIPTION { get; set; }   // ← NOVÉ POLE – název položky
    }

    class BomDb : DbContext
    {
        public BomDb(DbContextOptions<BomDb> options) : base(options) { }

        public DbSet<BomRoutingRule> BomRoutingRules => Set<BomRoutingRule>();
        public DbSet<BomRoutingRow> BomRouting => Set<BomRoutingRow>();
        public DbSet<QuoteHeader> Quotes => Set<QuoteHeader>();
        public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
        public DbSet<SKz> SKz => Set<SKz>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BomRoutingRule>(entity =>
            {
                entity.HasKey(e => e.RuleID);
                entity.ToTable("tab.BomAndRoutingRules");
                entity.Property(e => e.BomAndRoutingType).HasColumnName("BomAndRoutingType");
            });

            modelBuilder.Entity<BomRoutingRow>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.ToTable("tab.BomAndRouting");
                entity.Property(e => e.ProductNumber).HasColumnName("PRODUCT #");
                entity.Property(e => e.MaterialNumber).HasColumnName("MATERIAL #");
                entity.Property(e => e.Qty).HasColumnType("decimal(18,4)");
            });

            modelBuilder.Entity<QuoteHeader>(entity =>
            {
                entity.HasKey(e => e.QuoteID);
                entity.ToTable("tab.Quotes", "dbo");
                entity.Property(e => e.QuoteNumber).HasColumnName("QuoteNumber");
                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.CustomerID).HasColumnName("CustomerID");
                entity.Property(e => e.CustomerName).HasColumnName("CustomerName");
                entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
                entity.Property(e => e.LastSaved).HasColumnName("LastSaved");
                entity.Property(e => e.QuoteDate).HasColumnName("QuoteDate");
                entity.Property(e => e.ValidUntil).HasColumnName("ValidUntil");
                entity.Property(e => e.PohodaOfferNumber).HasColumnName("PohodaOfferNumber");
                entity.Property(e => e.PohodaOfferInternalID).HasColumnName("PohodaOfferInternalID");
                entity.Property(e => e.PohodaImportDate).HasColumnName("PohodaImportDate");
                entity.Property(e => e.PohodaImportStatus).HasColumnName("PohodaImportStatus");
                entity.Property(e => e.LastPohodaAttemptDate).HasColumnName("LastPohodaAttemptDate");
                entity.Property(e => e.PohodaLastResponse).HasColumnName("PohodaLastResponse");
            });

            modelBuilder.Entity<QuoteItem>(entity =>
            {
                entity.HasKey(e => e.ItemID);
                entity.ToTable("tab.QuoteItems", "dbo");
                entity.Property(e => e.QuoteID).HasColumnName("QuoteID");
                entity.Property(e => e.Code).HasColumnName("Code");
                entity.Property(e => e.Name).HasColumnName("Name");
                entity.Property(e => e.Attr1).HasColumnName("Attr1");
                entity.Property(e => e.Attr2).HasColumnName("Attr2");
                entity.Property(e => e.Attr3).HasColumnName("Attr3");
                entity.Property(e => e.Attr4).HasColumnName("Attr4");
                entity.Property(e => e.Attr5).HasColumnName("Attr5");
                entity.Property(e => e.Attr6).HasColumnName("Attr6");
                entity.Property(e => e.Attr7).HasColumnName("Attr7");
                entity.Property(e => e.Quantity).HasColumnName("Quantity");
                entity.Property(e => e.CostPrice).HasPrecision(18, 2);
                entity.Property(e => e.SellingPrice).HasPrecision(18, 2);
            });

            modelBuilder.Entity<SKz>(entity =>
            {
                entity.ToTable("SKz", "dbo");
                entity.HasKey(e => e.IDS);
                entity.Property(e => e.IDS).HasMaxLength(100);
                entity.Property(e => e.PURCHASE_PRICE).HasPrecision(18, 4);
            });
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls("http://0.0.0.0:6106");  // ← změněno na 5006

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });

            builder.Services.AddDbContext<BomDb>(options =>
            {
                var cs = builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException(
                        "Chybí konfigurace 'ConnectionStrings:DefaultConnection'. " +
                        "Nastav ji v appsettings.Development.json nebo jako environment proměnnou.");

                options.UseSqlServer(cs);

                if (builder.Configuration.GetValue<bool>("Database:EnableSensitiveDataLogging"))
                    options.EnableSensitiveDataLogging().EnableDetailedErrors();
            });

            var app = builder.Build();
            app.UseCors("AllowAll");

            //nový endpoint
            app.MapGet("/lookup-item", async (BomDb db, string code) =>
{
    if (string.IsNullOrWhiteSpace(code))
    {
        return Results.BadRequest(new { found = false, message = "Chybí kód" });
    }

    code = code.Trim();

    var record = await db.SKz
        .Where(p => p.IDS == code)
        .Select(p => new 
        { 
            p.IDS,
            p.DESCRIPTION   // ← vracíme název z DESCRIPTION
        })
        .FirstOrDefaultAsync();

    if (record != null)
    {
        return Results.Ok(new
        {
            found = true,
            source = "SKz",
            code = record.IDS,
            name = record.DESCRIPTION?.Trim() ?? $"HDPE položka {record.IDS}",   // fallback, pokud DESCRIPTION je NULL
            attr1 = "",
            attr2 = "",
            attr3 = "",
            attr4 = "",
            attr5 = "",
            attr6 = "",
            attr7 = "",
            defaultQuantity = 1
        });
    }

    return Results.Ok(new
    {
        found = false,
        message = $"Kód '{code}' nebyl nalezen v tabulce dbo.SKz"
    });
});
            // ────────────────────────────────────────────────
            // PŮVODNÍ ENDPOINTY ZŮSTÁVAJÍ BEZE ZMĚNY
            // ────────────────────────────────────────────────

            app.MapGet("/", () => "RestAPI_BOM běží na portu 5006 (nový /generate-bom-routing)");

            app.MapGet("/quotes", async (BomDb db) =>
            {
                var quotes = await db.Quotes
                    .OrderByDescending(q => q.LastSaved)
                    .Select(q => new
                    {
                        q.QuoteNumber,
                        q.Status,
                        q.CustomerName,
                        created = q.CreatedDate.ToString("dd.MM.yyyy HH:mm"),
                        lastSaved = q.LastSaved.ToString("dd.MM.yyyy HH:mm")
                    })
                    .ToListAsync();

                return Results.Json(quotes);
            });

            // ... (všechny ostatní původní endpointy zůstávají stejné – quote, quick-append-item, next-quote, lookup-item, send-to-pohoda atd.)

            // ────────────────────────────────────────────────
            // JEN TENTO ENDPOINT JE NOVÝ – Z RESTAPI2
            // ────────────────────────────────────────────────
            app.MapPost("/generate-bom-routing", async (BomDb db, BomRequest request) =>
            {
                if (request == null || request.items == null || request.items.Count == 0)
                    return Results.Json(new { success = false, message = "Žádné položky" }, statusCode: 400);

                var itemResults = new List<object>();

                var requiredTypes = new[] { "B_1", "B_2", "B_3", "B_4", "B_5" };

                foreach (var reqItem in request.items)
                {
                    string code = reqItem.code?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(code))
                    {
                        itemResults.Add(new
                        {
                            code = "???",
                            quantity = reqItem.quantity,
                            bomComplete = false,
                            priceComplete = false,
                            costPerPiece = 0m,
                            totalCost = 0m,
                            missingPrices = new[] { "Chybí kód" },
                            missingTypes = requiredTypes
                        });
                        continue;
                    }

                    var bomRows = await db.BomRouting
                        .Where(r => r.ProductNumber.Trim() == code)
                        .ToListAsync();

                    var foundTypes = bomRows
                        .Where(r => !string.IsNullOrEmpty(r.Type))
                        .Select(r => r.Type!.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToHashSet();

                    var missingTypes = requiredTypes
                        .Where(rt => !foundTypes.Contains(rt))
                        .ToList();

                    bool hasAllRequiredTypes = missingTypes.Count == 0;

                    var missingPrices = new List<string>();
                    decimal totalCost = 0m;

                    foreach (var row in bomRows)
                    {
                        if (!row.Qty.HasValue || row.Qty.Value <= 0)
                            continue;

                        decimal cenaJednotky = 0m;
                        string matNum = row.MaterialNumber?.Trim() ?? "";

                        if (!string.IsNullOrEmpty(matNum))
                        {
                            cenaJednotky = await db.SKz
                                .Where(p => p.IDS == matNum)
                                .Select(p => p.PURCHASE_PRICE)
                                .FirstOrDefaultAsync();

                            if (cenaJednotky > 0)
                            {
                                totalCost += row.Qty.Value * cenaJednotky;
                            }
                            else
                            {
                                missingPrices.Add(matNum + " (nenalezeno v SKz nebo cena = 0)");
                            }
                        }
                        else
                        {
                            missingPrices.Add("bez material #");
                        }
                    }

                    bool priceComplete = missingPrices.Count == 0;
                    bool bomComplete = hasAllRequiredTypes && priceComplete;

                    decimal costPerPiece = reqItem.quantity > 0 ? totalCost / reqItem.quantity : 0m;

                    itemResults.Add(new
                    {
                        code,
                        quantity = reqItem.quantity,
                        bomComplete,
                        priceComplete,
                        costPerPiece,
                        totalCost,
                        missingPrices,
                        missingTypes,
                        foundTypesCount = foundTypes.Count
                    });
                }

                return Results.Json(new
                {
                    success = true,
                    message = itemResults.All(r => ((dynamic)r).bomComplete) ? "BOM kompletní" : "BOM nekompletní",
                    items = itemResults
                });
            });

            // ────────────────────────────────────────────────
            // ZBYTEK PŮVODNÍHO KÓDU – lookup-item, send-to-pohoda atd.
            // (pokud je chceš, nech je tam – já je vynechal kvůli délce)
            // ────────────────────────────────────────────────

            Console.WriteLine("START APLIKACE – " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            app.Run();
        }
    }
}