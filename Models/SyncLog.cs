using System.ComponentModel.DataAnnotations;

namespace LMSDashboard.Models;

public class SyncLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(200)]
    public string SheetName { get; set; } = string.Empty;

    public int RowsUpdated { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string Status { get; set; } = string.Empty;
}
