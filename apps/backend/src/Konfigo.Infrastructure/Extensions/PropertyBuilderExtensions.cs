using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konfigo.Infrastructure.Extensions;

internal static class PropertyBuilderExtensions
{
    public static PropertyBuilder<T> DoNotSaveChanges<T>(this PropertyBuilder<T> propertyBuilder)
    {
        propertyBuilder.Metadata.SetBeforeSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
        propertyBuilder.Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        return propertyBuilder;
    }
}
