using System.ComponentModel.DataAnnotations;

namespace Konfigo.Controllers.Models.Services;

public sealed class SearchServicesRequest
{
    public string? Name { get; set; }
    public string? PageToken { get; set; }
    [Range(1, 500)]
    public int PageSize { get; set; }
}
