namespace RestAPI1.Models;

public class QuoteItem
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