namespace RestAPI1.Models;

public class InquiryHeader
{
    public int InquiryID { get; set; }
    public string InquiryNumber { get; set; } = null!;
    public string Status { get; set; } = "Rozpracovaná";
    public string? CustomerID { get; set; }          // ← chybělo
    public string? CustomerName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastSaved { get; set; }
    public DateTime? InquiryDate { get; set; }         // ← chybělo
    public DateTime? ValidUntil { get; set; }        // ← chybělo
    public string? PohodaInquiryNumber { get; set; }
    public int? PohodaInquiryInternalID { get; set; }
    public DateTime? PohodaImportDate { get; set; }
    public string? PohodaImportStatus { get; set; }
    public DateTime? LastPohodaAttemptDate { get; set; }
    public string? PohodaLastResponse { get; set; }
}