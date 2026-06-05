using Konfigo.Client.Models;

namespace Konfigo.Client.Infrastructure.Assemblies;

internal interface IAssemblyService
{
    ClassDefinition[] GetDefinitions();
}
