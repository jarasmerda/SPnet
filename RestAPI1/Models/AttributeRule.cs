namespace RestAPI1.Models;

public class AttributeRule
{
    public int RuleID { get; set; }
    public int FromAttribute { get; set; }
    public string FromValue { get; set; } = null!;
    public int ToAttribute { get; set; }
    public string AllowedValues { get; set; } = null!;
}