namespace RestAPI1.Models;   // ← přizpůsob namespace tvému projektu

/// <summary>
/// DTO pro jednu položku při rychlém přidání do nabídky
/// </summary>
public record QuickAppendItemDto
{
    public string Code { get; init; }
    public string Name { get; init; }
    public string? Attr1 { get; init; }
    public string? Attr2 { get; init; }
    public string? Attr3 { get; init; }
    public string? Attr4 { get; init; }
    public string? Attr5 { get; init; }
    public string? Attr6 { get; init; }
    public string? Attr7 { get; init; }
    public int Quantity { get; init; }

    // Primární konstruktor – vše povinné + defaulty
    public QuickAppendItemDto(
        string code,
        string name,
        string? attr1 = null,
        string? attr2 = null,
        string? attr3 = null,
        string? attr4 = null,
        string? attr5 = null,
        string? attr6 = null,
        string? attr7 = null,
        int quantity = 1)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Attr1 = attr1;
        Attr2 = attr2;
        Attr3 = attr3;
        Attr4 = attr4;
        Attr5 = attr5;
        Attr6 = attr6;
        Attr7 = attr7;
        Quantity = quantity > 0 ? quantity : 1;
    }

    // Pro fallback / testování – prázdný konstruktor
    public QuickAppendItemDto() : this("", "Bez názvu", quantity: 1) { }
}

/// <summary>
/// Celý request pro endpoint AddItemManually
/// </summary>
public record QuickAppendRequest
{
    public QuickAppendItemDto Item { get; init; }
    public string? Status { get; init; }

    // Primární konstruktor – povinný Item
    public QuickAppendRequest(QuickAppendItemDto item, string? status = null)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        Status = status;
    }

    // Pro fallback / testování – defaultní hodnoty
    public QuickAppendRequest() : this(new QuickAppendItemDto()) { }
}