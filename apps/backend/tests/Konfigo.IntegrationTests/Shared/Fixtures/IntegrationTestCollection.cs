using Xunit;

namespace Konfigo.IntegrationTests.Shared.Fixtures;

[CollectionDefinition(nameof(IntegrationTestCollection), DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>;
