namespace RestAPI1.Models;

public class QuoteHeader
{
    public int QuoteID { get; set; }
    public string QuoteNumber { get; set; } = null!;
    public string Status { get; set; } = "Rozpracovaná";
    public string? CustomerID { get; set; }          // ← chybělo
    public string? CustomerName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastSaved { get; set; }
    public DateTime? QuoteDate { get; set; }         // ← chybělo
    public DateTime? ValidUntil { get; set; }        // ← chybělo
    public string? PohodaOfferNumber { get; set; }
    public int? PohodaOfferInternalID { get; set; }
    public DateTime? PohodaImportDate { get; set; }
    public string? PohodaImportStatus { get; set; }
    public DateTime? LastPohodaAttemptDate { get; set; }
    public string? PohodaLastResponse { get; set; }
}