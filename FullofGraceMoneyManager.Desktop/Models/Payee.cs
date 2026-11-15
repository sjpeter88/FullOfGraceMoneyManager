namespace FullofGraceMoneyManager.Desktop.Models;

public class Payee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? DefaultCategoryId { get; set; }
    public string? DefaultCategoryName { get; set; }
    public string? NormalizedPattern { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Notes { get; set; }
}
