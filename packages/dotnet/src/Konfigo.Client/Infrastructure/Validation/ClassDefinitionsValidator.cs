using System.Collections.Generic;
using System.Linq;
using Konfigo.Client.Exceptions;
using Konfigo.Client.Models;

namespace Konfigo.Client.Infrastructure.Validation;

internal static class ClassDefinitionsValidator
{
    public static void Validate(IEnumerable<ClassDefinition> definitions)
    {
        var collisions = definitions
            .SelectMany(definition => definition.Options.Select(option => (definition.Type, Option: option)))
            .GroupBy(entry => entry.Option.Key)
            .Where(group => group.Select(entry => entry.Option.Type).Distinct().Skip(1).Any())
            .Select(group =>
            {
                var declarations = group.Select(entry => $"{entry.Option.Type} in {entry.Type.FullName}");
                return $"Key '{group.Key}' is declared with different types: {string.Join(", ", declarations)}";
            })
            .ToArray();

        if (collisions is not [])
        {
            throw new ConfigKeyCollisionException(collisions);
        }
    }
}
