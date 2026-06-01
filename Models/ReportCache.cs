using System.ComponentModel.DataAnnotations;

namespace LMSDashboard.Models;

public class ReportCache
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(100)]
    public string ReportKey { get; set; } = string.Empty;

    public string DataJson { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
