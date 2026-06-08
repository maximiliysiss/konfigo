using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Application.Repositories;
using Konfigo.Application.Services.Configurations;
using Konfigo.Application.Services.Configurations.Audit;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.UnitTests.Support;
using NSubstitute;
using Xunit;
using UpdateVersionRequest = Konfigo.Application.Services.Configurations.Models.UpdateVersionRequest;

namespace Konfigo.UnitTests.Application.Configurations;

public sealed class AuditConfigVersionServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldWriteVersionCreatedAuditLog_WhenCalled()
    {
        // Arrange
        var inner = Substitute.For<IConfigVersionService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();

        var request = TestFakes.BuildCreateVersionRequest();
        var created = TestFakes.BuildVersion(serviceId: request.ServiceId);
        inner.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(created);

        var sut = new AuditConfigVersionService(inner, auditRepo);

        // Act
        var result = await sut.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(created);
        await auditRepo.Received(1).AddAsync(
            Arg.Is<AuditLog>(log =>
                log.ServiceId == request.ServiceId &&
                log.UserId == request.CreatedBy &&
                log.Entry is VersionCreatedEntry &&
                ((VersionCreatedEntry)log.Entry).Id == created.Id &&
                ((VersionCreatedEntry)log.Entry).VersionLabel == request.VersionLabel),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldWriteVersionUpdatedAuditLog_WhenUnderlyingReturnsVersion()
    {
        // Arrange
        var inner = Substitute.For<IConfigVersionService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();

        var request = TestFakes.BuildUpdateVersionRequest();
        var updated = TestFakes.BuildVersion(id: request.VersionId, serviceId: request.ServiceId);
        inner.UpdateAsync(request, Arg.Any<CancellationToken>()).Returns(updated);

        var sut = new AuditConfigVersionService(inner, auditRepo);

        // Act
        var result = await sut.UpdateAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(updated);
        await auditRepo.Received(1).AddAsync(
            Arg.Is<AuditLog>(log =>
                log.UserId == request.UpdatedBy &&
                log.Entry is VersionUpdatedEntry &&
                ((VersionUpdatedEntry)log.Entry).Id == updated.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotWriteAuditLog_WhenUnderlyingReturnsNull()
    {
        // Arrange
        var inner = Substitute.For<IConfigVersionService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        inner.UpdateAsync(Arg.Any<UpdateVersionRequest>(), Arg.Any<CancellationToken>())
            .Returns((ConfigVersion?)null);

        var sut = new AuditConfigVersionService(inner, auditRepo);

        // Act
        var result = await sut.UpdateAsync(TestFakes.BuildUpdateVersionRequest(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await auditRepo.DidNotReceive().AddAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>());
        await auditRepo.DidNotReceive().AddAsync(Arg.Any<AuditLog[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ShouldWriteVersionAndPerEntryAuditLogs_WhenCalled()
    {
        // Arrange
        var inner = Substitute.For<IConfigVersionService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();

        var serviceId = ServiceId.New();
        var generated = TestFakes.BuildVersion(serviceId: serviceId);
        generated.ConfigEntries =
        [
            TestFakes.BuildEntry(versionId: generated.Id),
            TestFakes.BuildEntry(versionId: generated.Id),
            TestFakes.BuildEntry(versionId: generated.Id),
        ];
        var request = TestFakes.BuildGenerateVersionRequest(serviceId: serviceId);
        inner.GenerateAsync(request, Arg.Any<CancellationToken>()).Returns(new GenerateResult.New(generated));

        var sut = new AuditConfigVersionService(inner, auditRepo);

        // Act
        var result = await sut.GenerateAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<GenerateResult.New>();
        result.Version.Should().BeSameAs(generated);
        await auditRepo.Received(1).AddAsync(
            Arg.Is<AuditLog[]>(logs =>
                logs.Length == 4 &&
                logs.Count(l => l.Entry is VersionCreatedEntry) == 1 &&
                logs.Count(l => l.Entry is EntryCreatedEntry) == 3 &&
                logs.All(l => l.ServiceId == serviceId) &&
                logs.All(l => l.UserId == null)),
            Arg.Any<CancellationToken>());
    }
}
