using System.ComponentModel.DataAnnotations;

namespace Konfigo.Controllers.Models.Audit;

public sealed class SearchAuditRequest
{
    public string? PageToken { get; set; }
    [Range(1, 500)]
    public int PageSize { get; set; }
}
