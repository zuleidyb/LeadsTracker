namespace LeadsTracker.Api.Models;

public enum LeadStatus { New, Contacted, Qualified, Unqualified, Converted }

public enum LeadSource { Website, Referral, LinkedIn, TradeShow, ColdCall, EmailCampaign }

public class Lead
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Company { get; set; }
    public LeadStatus Status { get; set; }
    public LeadSource Source { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public string? Industry { get; set; }
    public string? Owner { get; set; }
    public decimal? EstimatedValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
