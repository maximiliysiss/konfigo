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

        await realtimeConfigGrpcService
            .DidNotReceive()
            .CreateVersionAsync(Arg.Any<CreateVersionRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ShouldCheckVersionByConfiguredServiceIdAndVersion()
    {
        // Arrange
        IsVersionExistRequest? request = null;

        var options = new RealtimeConfigOptions
        {
            ServiceId = "service-id",
            Version = "1.2.3",
        };

        var realtimeConfigGrpcService = Substitute.For<IRealtimeConfigClient>();
        realtimeConfigGrpcService
            .IsVersionExistsAsync(Arg.Any<IsVersionExistRequest>(), Arg.Any<CancellationToken>())
            .Returns(new IsVersionExistResponse { VersionId = Guid.NewGuid().ToString() })
            .AndDoes(x => request = x.Arg<IsVersionExistRequest>());

        var versionService = Create(rtcOptions: options, service: realtimeConfigGrpcService);

        // Act
        await versionService.CreateAsync(CancellationToken.None);

        // Assert
        request.Should().BeEquivalentTo(new
        {
            options.ServiceId,
            options.Version,
        });
    }

    [Fact]
    public async Task CreateAsync_ShouldCallChanges_WhenThereIsChanges()
    {
        // Arrange
        CreateVersionRequest? request = null;

        var id = Guid.NewGuid().ToString();

        var options = new RealtimeConfigOptions
        {
            ServiceId = "service-id",
            Version = "1.2.3",
        };

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
            Name: "Created",
            Description: "Created description",
            Type: ValueType.Array,
            DefaultValue: "[]",
            EnumValues: null);

        var updated = new ClassDefinition.OptionDefinition(
            Key: "Name+",
            Name: "Updated",
            Description: "Updated description",
            Type: ValueType.Enum,
            DefaultValue: "B",
            EnumValues: ["A", "B"]);

        var classDefinition = new ClassDefinition(
            Key: string.Empty,
            Type: typeof(object),
            Name: "Class name",
            Description: "Class description",
            Options: [newEntry, updated]);

        var assemblyService = Substitute.For<IAssemblyService>();
        assemblyService
            .GetDefinitions()
            .Returns([classDefinition]);

        var versionService = Create(
            rtcOptions: options,
            service: realtimeConfigGrpcService,
            assemblyService: assemblyService);

        // Act
        var versionId = await versionService.CreateAsync(CancellationToken.None);

        // Assert
        var expected = new
        {
            options.ServiceId,
            options.Version,
            Classes = new[]
            {
                new
                {
                    Name = classDefinition.Name,
                    Description = classDefinition.Description,
                    Entries = new[]
                    {
                        new
                        {
                            newEntry.Key,
                            newEntry.Name,
                            newEntry.Description,
                            Value = newEntry.DefaultValue,
                            ValueType = newEntry.Type,
                            EnumValues = string.Empty,
                        },
                        new
                        {
                            updated.Key,
                            updated.Name,
                            updated.Description,
                            Value = updated.DefaultValue,
                            ValueType = updated.Type,
                            EnumValues = "A,B",
                        }
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
