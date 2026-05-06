namespace RestAPI1.Models;

public class BomRoutingRow
{
    public int ID { get; set; }
    public string ProductNumber { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? MaterialNumber { get; set; }
    public decimal? Qty { get; set; }
    public string? UoM { get; set; }
    public DateTime? CreatedDate { get; set; }
}