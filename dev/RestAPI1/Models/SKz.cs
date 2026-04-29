using System.ComponentModel.DataAnnotations;

namespace RestAPI1.Models;

public class SKz
{
    [Key]
    public string IDS { get; set; } = null!;
    public decimal? PURCHASE_PRICE { get; set; } 
    public string DESCRIPTION { get; set; }
    public string? StockCat { get; set; }
    public string? VPrAttr1 { get; set; }
    public string? VPrAttr2 { get; set; }
    public string? VPrAttr3 { get; set; }
    public string? VPrAttr4 { get; set; }
    public string? VPrAttr5 { get; set; }
    public string? VPrAttr6 { get; set; }
    public string? VPrAttr7 { get; set; }
    public string? VPrAttr8 { get; set; }
    public string? VPrAttr9 { get; set; }
}