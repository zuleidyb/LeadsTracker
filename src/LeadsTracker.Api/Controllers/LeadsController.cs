using LeadsTracker.Api.Contracts;
using LeadsTracker.Api.Data;
using LeadsTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LeadsTracker.Api.Controllers;

[ApiController]
[Route("api/leads")]
public class LeadsController(LeadsDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<LeadResponse>> Create(
        CreateLeadRequest request,
        CancellationToken cancellationToken)
    {
        var lead = new Lead
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email,
            Company = request.Company.Trim(),
            Status = request.Status!.Value,
            Source = request.Source!.Value,
            Phone = Clean(request.Phone),
            JobTitle = Clean(request.JobTitle),
            Industry = Clean(request.Industry),
            Owner = Clean(request.Owner),
            EstimatedValue = request.EstimatedValue
        };

        db.Leads.Add(lead);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Leads_Email"
        })
        {
            return Problem(
                title: "A lead with this email already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        return CreatedAtAction(nameof(GetById), new { id = lead.Id }, ToResponse(lead));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeadResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var lead = await db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        return lead is null ? NotFound() : ToResponse(lead);
    }

    [HttpGet]
    public async Task<ActionResult<LeadListResponse>> List(
        [FromQuery] LeadQuery query,
        CancellationToken cancellationToken)
    {
        var leads = ApplyFilters(db.Leads.AsNoTracking(), query);

        var counts = await leads
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);

        var statusCounts = Enum.GetValues<LeadStatus>()
            .ToDictionary(status => status, status => counts.GetValueOrDefault(status));

        var page = await leads
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new LeadListResponse(
            page.Select(ToResponse).ToList(),
            statusCounts.Values.Sum(),
            query.Page,
            query.PageSize,
            statusCounts);
    }

    private static IQueryable<Lead> ApplyFilters(IQueryable<Lead> leads, LeadQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim()
                .Replace(@"\", @"\\")
                .Replace("%", @"\%")
                .Replace("_", @"\_");
            var pattern = $"%{term}%";

            leads = leads.Where(l =>
                EF.Functions.ILike(l.FirstName + " " + l.LastName, pattern, @"\") ||
                EF.Functions.ILike(l.Company, pattern, @"\") ||
                EF.Functions.ILike(l.Email, pattern, @"\"));
        }

        if (query.Status is { } status)
        {
            leads = leads.Where(l => l.Status == status);
        }

        if (query.Source is { } source)
        {
            leads = leads.Where(l => l.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(query.Industry))
        {
            leads = leads.Where(l => l.Industry == query.Industry);
        }

        if (query.Owner == "Unassigned")
        {
            leads = leads.Where(l => l.Owner == null);
        }
        else if (!string.IsNullOrWhiteSpace(query.Owner))
        {
            leads = leads.Where(l => l.Owner == query.Owner);
        }

        if (query.From is { } from)
        {
            var start = StartOfDay(from);
            leads = leads.Where(l => l.CreatedAt >= start);
        }

        if (query.To is { } to)
        {
            var end = StartOfDay(to.AddDays(1));
            leads = leads.Where(l => l.CreatedAt < end);
        }

        return leads;
    }

    private static DateTimeOffset StartOfDay(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static LeadResponse ToResponse(Lead lead) => new(
        lead.Id,
        lead.FirstName,
        lead.LastName,
        lead.Email,
        lead.Company,
        lead.Status,
        lead.Source,
        lead.Phone,
        lead.JobTitle,
        lead.Industry,
        lead.Owner,
        lead.EstimatedValue,
        lead.CreatedAt,
        lead.UpdatedAt);
}
