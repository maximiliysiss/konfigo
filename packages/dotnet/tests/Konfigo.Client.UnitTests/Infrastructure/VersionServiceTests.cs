using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Client.Grpc;
using Konfigo.Client.Infrastructure.Assemblies;
using Konfigo.Client.Infrastructure.Client;
using Konfigo.Client.Infrastructure.Versions;
using Konfigo.Client.Models;
using Konfigo.Client.Options;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using ValueType = Konfigo.Client.Grpc.ValueType;

namespace Konfigo.Client.UnitTests.Infrastructure;

public class VersionServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldDoNothing_WhenVersionExists()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();

        var realtimeConfigGrpcService = Substitute.For<IRealtimeConfigClient>();
        realtimeConfigGrpcService
            .IsVersionExistsAsync(Arg.Any<IsVersionExistRequest>(), Arg.Any<CancellationToken>())
            .Returns(new IsVersionExistResponse { VersionId = id });

        var versionService = Create(service: realtimeConfigGrpcService);

        // Act
        var versionId = await versionService.CreateAsync(CancellationToken.None);

        // Assert
        versionId.Value.Should().Be(id);
    }

    [Fact]
    public async Task CreateAsync_ShouldCallChanges_WhenThereIsChanges()
    {
        // Arrange
        CreateVersionRequest? request = null;

        var id = Guid.NewGuid().ToString();

        var realtimeConfigGrpcService = Substitute.For<IRealtimeConfigClient>();
        realtimeConfigGrpcService
            .CreateVersionAsync(Arg.Any<CreateVersionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateVersionResponse { VersionId = id })
            .AndDoes(x => request = x.Arg<CreateVersionRequest>());

        realtimeConfigGrpcService
            .IsVersionExistsAsync(Arg.Any<IsVersionExistRequest>(), Arg.Any<CancellationToken>())
            .Returns(new IsVersionExistResponse());

        var newEntry = new ClassDefinition.OptionDefinition(
            Key: "Name",
            Name: string.Empty,
            Description: null,
            Type: ValueType.Array,
            DefaultValue: null,
            EnumValues: null);

        var updated = new ClassDefinition.OptionDefinition(
            Key: "Name+",
            Name: string.Empty,
            Description: null,
            Type: ValueType.Boolean,
            DefaultValue: null,
            EnumValues: null);

        var classDefinition = new ClassDefinition(
            Key: string.Empty,
            Type: typeof(object),
            Name: string.Empty,
            Description: string.Empty,
            Options: [newEntry, updated]);

        var assemblyService = Substitute.For<IAssemblyService>();
        assemblyService
            .GetDefinitions()
            .Returns([classDefinition]);

        var versionService = Create(service: realtimeConfigGrpcService, assemblyService: assemblyService);

        // Act
        var versionId = await versionService.CreateAsync(CancellationToken.None);

        // Assert
        var expected = new
        {
            Classes = new[]
            {
                new
                {
                    Entries = new[]
                    {
                        new { Key = newEntry.Key, Value = newEntry.DefaultValue, ValueType = newEntry.Type },
                        new { Key = updated.Key, Value = updated.DefaultValue, ValueType = updated.Type }
                    }
                }
            }
        };

        request.Should().BeEquivalentTo(expected);

        versionId.Value.Should().Be(id);
    }

    private static VersionService Create(
        RealtimeConfigOptions? rtcOptions = null,
        IRealtimeConfigClient? service = null,
        IAssemblyService? assemblyService = null)
    {
        return new VersionService(
            Microsoft.Extensions.Options.Options.Create(rtcOptions ?? new RealtimeConfigOptions()),
            service ?? Substitute.For<IRealtimeConfigClient>(),
            NullLogger<VersionService>.Instance,
            assemblyService ?? Substitute.For<IAssemblyService>());
    }
}
