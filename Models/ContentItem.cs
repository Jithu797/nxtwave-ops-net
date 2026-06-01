using System.ComponentModel.DataAnnotations;

namespace LMSDashboard.Models;

public enum ContentType { Quiz, Reading, Audio, PPT, Activity }
public enum Track { Foundation, B1, Advanced, Applied, Crescent }
public enum Difficulty { Easy, Medium, Hard }
public enum ContentStatus { Pending, InBeta, Validated, InProduction, Failed }

public class ContentItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public ContentType Type { get; set; }
    public Track Track { get; set; }
    public Difficulty Difficulty { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Pending;

    public DateTime? BetaUploadedAt { get; set; }
    public DateTime? ProdUploadedAt { get; set; }
    public DateTime? ValidatedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StatusChangedAt { get; set; }

    [MaxLength(200)]
    public string CreatedBy { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public bool IsDeleted { get; set; } = false;

    public ICollection<ValidationLog> ValidationLogs { get; set; } = new List<ValidationLog>();
}
