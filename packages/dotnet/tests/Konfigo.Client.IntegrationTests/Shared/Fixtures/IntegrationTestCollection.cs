using Xunit;

namespace Konfigo.Client.IntegrationTests.Shared.Fixtures;

[CollectionDefinition(nameof(IntegrationTestCollection), DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>;
