using System.ComponentModel.DataAnnotations;

namespace LMSDashboard.Models;

public enum ValidationResult { Pass, Fail }

public class ValidationLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContentItemId { get; set; }
    public ContentItem ContentItem { get; set; } = null!;

    [Required, MaxLength(200)]
    public string RuleName { get; set; } = string.Empty;

    public ValidationResult Result { get; set; }

    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
