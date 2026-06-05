using Konfigo.Client.Models;

namespace Konfigo.Client.Infrastructure.Assemblies;

internal sealed class AssemblyService : IAssemblyService
{
    public ClassDefinition[] GetDefinitions() => Extensions.Assemblies.GetDefinitions();
}
