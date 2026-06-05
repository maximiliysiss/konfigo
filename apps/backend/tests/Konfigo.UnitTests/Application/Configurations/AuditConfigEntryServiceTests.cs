using System;
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
using UpdateEntryRequest = Konfigo.Application.Services.Configurations.Models.UpdateEntryRequest;

namespace Konfigo.UnitTests.Application.Configurations;

public sealed class AuditConfigEntryServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldWriteEntryCreatedAuditLog_WhenCalled()
    {
        // Arrange
        var inner = Substitute.For<IConfigEntryService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();

        var request = TestFakes.BuildCreateEntryRequest();
        var created = TestFakes.BuildEntry(id: EntryId.New());
        inner.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(created);

        var sut = new AuditConfigEntryService(inner, auditRepo);

        // Act
        var result = await sut.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(created);
        await auditRepo.Received(1).AddAsync(
            Arg.Is<AuditLog>(log =>
                log.ServiceId == request.ServiceId &&
                log.UserId == request.CreatedBy &&
                log.Entry is EntryCreatedEntry &&
                ((EntryCreatedEntry)log.Entry).Id == created.Id &&
                ((EntryCreatedEntry)log.Entry).RawValue == request.RawValue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldWriteEntryUpdatedAuditLog_WhenUnderlyingReturnsEntry()
    {
        // Arrange
        var inner = Substitute.For<IConfigEntryService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();

        var request = TestFakes.BuildUpdateEntryRequest();
        var updated = TestFakes.BuildEntry(id: request.Id);
        updated.UpdatedAt = TestFakes.Now.AddMinutes(2);
        inner.UpdateAsync(request, Arg.Any<CancellationToken>()).Returns(updated);

        var sut = new AuditConfigEntryService(inner, auditRepo);

        // Act
        var result = await sut.UpdateAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(updated);
        await auditRepo.Received(1).AddAsync(
            Arg.Is<AuditLog>(log =>
                log.UserId == request.UpdatedBy &&
                log.Entry is EntryUpdatedEntry &&
                ((EntryUpdatedEntry)log.Entry).Id == updated.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotWriteAuditLog_WhenUnderlyingReturnsNull()
    {
        // Arrange
        var inner = Substitute.For<IConfigEntryService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        inner.UpdateAsync(Arg.Any<UpdateEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns((ConfigEntry?)null);

        var sut = new AuditConfigEntryService(inner, auditRepo);

        // Act
        var result = await sut.UpdateAsync(TestFakes.BuildUpdateEntryRequest(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await auditRepo.DidNotReceive().AddAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>());
        await auditRepo.DidNotReceive().AddAsync(Arg.Any<AuditLog[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_ShouldWriteOneEntrySetLogPerReturnedEntry_WhenCalled()
    {
        // Arrange
        var inner = Substitute.For<IConfigEntryService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();

        var updatedBy = new UserDto(new UserId(Guid.NewGuid().ToString()), Roles: ["billing"]);
        var serviceId = ServiceId.New();
        var versionId = VersionId.New();
        var request = new SetEntryRequest(
            serviceId,
            versionId,
            [
                new SetEntryRequest.SetRequest(EntryId.New(), "v1", 1),
                new SetEntryRequest.SetRequest(EntryId.New(), "v2", 1),
            ],
            updatedBy);

        var returnedEntries = new[]
        {
            TestFakes.BuildEntry(rawValue: "v1"),
            TestFakes.BuildEntry(rawValue: "v2"),
        };
        inner.SetAsync(request, Arg.Any<CancellationToken>()).Returns(returnedEntries);

        var sut = new AuditConfigEntryService(inner, auditRepo);

        // Act
        var result = await sut.SetAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(returnedEntries);
        await auditRepo.Received(1).AddAsync(
            Arg.Is<AuditLog[]>(logs =>
                logs.Length == 2 &&
                logs.All(l => l.UserId == updatedBy.Id) &&
                logs.All<AuditLog>(l => l.Entry is EntrySetEntry)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldWriteEntryDeletedAuditLog_WhenUnderlyingReturnsEntry()
    {
        // Arrange
        var inner = Substitute.For<IConfigEntryService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();

        var request = TestFakes.BuildDeleteEntryRequest();
        var deleted = TestFakes.BuildEntry(id: request.Id);
        inner.DeleteAsync(request, Arg.Any<CancellationToken>()).Returns(deleted);

        var sut = new AuditConfigEntryService(inner, auditRepo);

        // Act
        var result = await sut.DeleteAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(deleted);
        await auditRepo.Received(1).AddAsync(
            Arg.Is<AuditLog>(log =>
                log.UserId == request.DeletedBy &&
                log.Entry is EntryDeletedEntry &&
                ((EntryDeletedEntry)log.Entry).Id == deleted.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotWriteAuditLog_WhenUnderlyingReturnsNull()
    {
        // Arrange
        var inner = Substitute.For<IConfigEntryService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        inner.DeleteAsync(Arg.Any<DeleteEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns((ConfigEntry?)null);

        var sut = new AuditConfigEntryService(inner, auditRepo);

        // Act
        var result = await sut.DeleteAsync(TestFakes.BuildDeleteEntryRequest(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await auditRepo.DidNotReceive().AddAsync(Arg.Any<AuditLog>(), Arg.Any<CancellationToken>());
        await auditRepo.DidNotReceive().AddAsync(Arg.Any<AuditLog[]>(), Arg.Any<CancellationToken>());
    }
}
