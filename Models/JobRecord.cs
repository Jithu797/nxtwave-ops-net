using System.ComponentModel.DataAnnotations;

namespace LMSDashboard.Models;

public class JobRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(200)]
    public string JobType { get; set; } = string.Empty;

    public string? PayloadJson { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    [MaxLength(2000)]
    public string? Result { get; set; }
}
