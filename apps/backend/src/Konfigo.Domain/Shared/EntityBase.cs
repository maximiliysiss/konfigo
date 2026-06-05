using System;

namespace Konfigo.Domain.Shared;

public abstract class EntityBase<TId>
{
    public required TId Id { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
