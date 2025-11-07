namespace BlazorApp1.Models;

public class Story
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Points { get; set; }
    public string? Area { get; set; }
    public string? Iteration { get; set; }
    public string? State { get; set; }
    public string? AssignedTo { get; set; }
    public int? Priority { get; set; }
    public string? Risk { get; set; }
    public DateTime? TargetDate { get; set; }
    public string? AcceptanceCriteria { get; set; }
    public string? RelatedWorkItem { get; set; }
    public string? UseCase { get; set; }

    // Approval workflow fields
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    
    // Azure DevOps integration fields
    public string? AdoWorkItemId { get; set; }
    public string? AdoWorkItemUrl { get; set; }
    public DateTime? SyncedToAdoAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum ApprovalStatus
{
    Draft = 0,          // Created, not yet submitted for approval
    PendingApproval = 1, // Submitted for approval
    Approved = 2,        // Approved by approver
    Rejected = 3         // Rejected by approver
}
