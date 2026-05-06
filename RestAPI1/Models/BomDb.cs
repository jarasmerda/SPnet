using Microsoft.EntityFrameworkCore;
using RestAPI1.Models;

namespace RestAPI1.Models;

public class BomDb : DbContext
{
    public BomDb(DbContextOptions<BomDb> options) : base(options) { }

    // DbSet-y – přidány chybějící
    public DbSet<QuoteHeader>   Quotes          => Set<QuoteHeader>();
    public DbSet<QuoteItem>     QuoteItems      => Set<QuoteItem>();
    public DbSet<InquiryHeader>   Inquiries          => Set<InquiryHeader>();
    public DbSet<InquiryItem>     InquiryItems      => Set<InquiryItem>();

    public DbSet<BomRoutingRow> BomRouting      => Set<BomRoutingRow>();
    public DbSet<SKz>           SKz             => Set<SKz>();
    public DbSet<AttributeValue2>  AttributeValues  => Set<AttributeValue2>();
    public DbSet<AttributeRule>   AttributesRules  => Set<AttributeRule>();
    public DbSet<Customer>        Customers        => Set<Customer>();
    

    // Pokud používáš i pravidla (BomRoutingRule), přidej i toto:
    // public DbSet<BomRoutingRule> BomRoutingRules => Set<BomRoutingRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // QuoteHeader
        modelBuilder.Entity<QuoteHeader>(entity =>
        {
            entity.HasKey(e => e.QuoteID);
            entity.ToTable("tab.Quotes", "dbo");  // ← schema dbo nebo tab podle tvé DB
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

               // QuoteItem
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

        // InquiryHeader
        modelBuilder.Entity<InquiryHeader>(entity =>
        {
            entity.HasKey(e => e.InquiryID);
            entity.ToTable("tab.Inquiries", "dbo");  // ← schema dbo nebo tab podle tvé DB
            entity.Property(e => e.InquiryNumber).HasColumnName("Inquirynumber");
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.CustomerID).HasColumnName("CustomerID");
            entity.Property(e => e.CustomerName).HasColumnName("CustomerName");
            entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
            entity.Property(e => e.LastSaved).HasColumnName("LastSaved");
            entity.Property(e => e.InquiryDate).HasColumnName("InquiryDate");
            entity.Property(e => e.ValidUntil).HasColumnName("ValidUntil");
            entity.Property(e => e.PohodaInquiryNumber).HasColumnName("PohodaInquiryNumber");
            entity.Property(e => e.PohodaInquiryInternalID).HasColumnName("PohodaInquiryInternalID");
            entity.Property(e => e.PohodaImportDate).HasColumnName("PohodaImportDate");
            entity.Property(e => e.PohodaImportStatus).HasColumnName("PohodaImportStatus");
            entity.Property(e => e.LastPohodaAttemptDate).HasColumnName("LastPohodaAttemptDate");
            entity.Property(e => e.PohodaLastResponse).HasColumnName("PohodaLastResponse");
        });

               // InquriyItem
        modelBuilder.Entity<InquiryItem>(entity =>
        {
            entity.HasKey(e => e.ItemID);
            entity.ToTable("tab.InquiryItems", "dbo");
            entity.Property(e => e.InquiryID).HasColumnName("InquiryID");
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

        // BomRoutingRow (tab.BomAndRouting)
        modelBuilder.Entity<BomRoutingRow>(entity =>
        {
            entity.HasKey(e => e.ID);
            entity.ToTable("tab.BomAndRouting");  // ← uprav schema pokud je jiné (tab nebo dbo)
            entity.Property(e => e.ProductNumber).HasColumnName("PRODUCT #");
            entity.Property(e => e.Type).HasColumnName("Type");
            entity.Property(e => e.MaterialNumber).HasColumnName("MATERIAL #");
            entity.Property(e => e.Qty).HasColumnType("decimal(18,4)");
            entity.Property(e => e.UoM).HasColumnName("UoM");
            entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
        });

        // SKz (dbo.SKz)
        modelBuilder.Entity<SKz>(entity =>
        {
            entity.ToTable("SKz", "dbo");  // ← schema dbo nebo tab podle tvé DB
            entity.HasKey(e => e.IDS);
            entity.Property(e => e.IDS).HasMaxLength(100);
            entity.Property(e => e.PURCHASE_PRICE).HasPrecision(18, 4);
        });

        modelBuilder.Entity<AttributeValue2>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("AttributeValues", "dbo");
            entity.Property(e => e.AttributeNumber).HasColumnName("ATTRIBUTE#");
            entity.Property(e => e.Attribute).HasColumnName("ATTRIBUTE");
            entity.Property(e => e.AttributeValueNumber).HasColumnName("ATTRIBUTE_VALUE#");
            entity.Property(e => e.AttributeValue).HasColumnName("ATTRIBUTE_VALUE");
        });

        modelBuilder.Entity<AttributeRule>(entity =>
        {
            entity.HasKey(e => e.RuleID);
            entity.ToTable("AttributesRules", "dbo");
        });


        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("StwPh.AD", "dbo");
            entity.Property(e => e.ID).HasColumnName("ID");
            entity.Property(e => e.Firma).HasColumnName("Firma");
            entity.Property(e => e.Jmeno).HasColumnName("Jmeno");
            entity.Property(e => e.Firma2).HasColumnName("Firma2");
            entity.Property(e => e.Jmeno2).HasColumnName("Jmeno2");
        });

        // Pokud používáš i BomRoutingRule, přidej konfiguraci i pro ni:
        // modelBuilder.Entity<BomRoutingRule>(entity =>
        // {
        //     entity.HasKey(e => e.RuleID);
        //     entity.ToTable("tab.BomAndRoutingRules");
        //     entity.Property(e => e.BomAndRoutingType).HasColumnName("BomAndRoutingType");
        //     // ... další vlastnosti
        // });
    }
}