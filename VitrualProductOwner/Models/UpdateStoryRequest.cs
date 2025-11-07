using System.ComponentModel.DataAnnotations;

namespace BlazorApp1.Models;

public class UpdateStoryRequest
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(512, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 13)]
    public int Points { get; set; } = 3;

    // Dodatkowe pola
    [StringLength(256)] public string? Area { get; set; }
    [StringLength(256)] public string? Iteration { get; set; }
    [StringLength(128)] public string? State { get; set; }
    [StringLength(256)] public string? AssignedTo { get; set; }
    public int? Priority { get; set; }
    [StringLength(128)] public string? Risk { get; set; }
    public DateTime? TargetDate { get; set; }
    [StringLength(4000)] public string? AcceptanceCriteria { get; set; }
    [StringLength(256)] public string? RelatedWorkItem { get; set; }
    [StringLength(1024)] public string? UseCase { get; set; }
}
