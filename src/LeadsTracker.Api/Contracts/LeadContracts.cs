using System.ComponentModel.DataAnnotations;
using LeadsTracker.Api.Models;

namespace LeadsTracker.Api.Contracts;

public sealed class CreateLeadRequest
{
    [Required, MaxLength(100)] public string FirstName { get; set; } = "";
    [Required, MaxLength(100)] public string LastName { get; set; } = "";

    [Required, MaxLength(254)]
    [RegularExpression(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", ErrorMessage = "Enter a valid email.")]
    public string Email { get; set; } = "";

    [Required, MaxLength(200)] public string Company { get; set; } = "";
    [Required] public LeadStatus? Status { get; set; }
    [Required] public LeadSource? Source { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(150)] public string? JobTitle { get; set; }
    [MaxLength(100)] public string? Industry { get; set; }
    [MaxLength(150)] public string? Owner { get; set; }
    [Range(0d, 9999999999.99)] public decimal? EstimatedValue { get; set; }
}

public sealed class LeadQuery
{
    public string? Q { get; set; }
    public LeadStatus? Status { get; set; }
    public LeadSource? Source { get; set; }
    public string? Industry { get; set; }
    public string? Owner { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
}

public sealed record LeadResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Company,
    LeadStatus Status,
    LeadSource Source,
    string? Phone,
    string? JobTitle,
    string? Industry,
    string? Owner,
    decimal? EstimatedValue,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record LeadListResponse(
    IReadOnlyList<LeadResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyDictionary<LeadStatus, int> StatusCounts);
