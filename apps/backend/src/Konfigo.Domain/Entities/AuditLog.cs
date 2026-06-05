using Konfigo.Domain.Shared;
using Konfigo.Domain.ValueType;

namespace Konfigo.Domain.Entities;

public sealed class AuditLog : EntityBase<LogId>
{
    public int Num { get; set; }
    public required ServiceId ServiceId { get; set; }
    public required UserId? UserId { get; set; }
    public required LogEntry Entry { get; set; }
}
