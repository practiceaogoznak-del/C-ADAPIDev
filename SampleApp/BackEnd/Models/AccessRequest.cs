using System.ComponentModel.DataAnnotations;

namespace BackEnd.Models;

public class AccessRequest
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public string Position { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public AccessRequestType RequestType { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public List<string> ResourceGroups { get; set; } = new();

    [Required]
    public List<string> Workstations { get; set; } = new();

    [Required]
    public AccessDuration Duration { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Pending;

    public string? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum AccessRequestType
{
    Register,
    Remove,
    ModifyAccess
}

public enum AccessDuration
{
    Permanent,
    Temporary
}

public enum AccessRequestStatus
{
    Pending,
    ApprovedByManager,
    ApprovedByResourceOwner,
    Rejected,
    Completed
}