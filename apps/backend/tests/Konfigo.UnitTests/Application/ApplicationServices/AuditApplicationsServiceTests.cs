using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Application.Infrastructure.DateTime;
using Konfigo.Application.Repositories;
using Konfigo.Application.Services.ApplicationServices;
using Konfigo.Application.Services.ApplicationServices.Audit;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.UnitTests.Support;
using NSubstitute;
using Xunit;

namespace Konfigo.UnitTests.Application.ApplicationServices;

public sealed class AuditApplicationsServiceTests
{
    [Fact]
    public async Task AddAsync_ShouldWriteServiceCreatedAuditLog_WhenCalled()
    {
        // Arrange
        var inner = Substitute.For<IApplicationsService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var dateTime = new DateTimeProvider();

        var request = TestFakes.BuildCreateServiceRequest(name: "billing");
        var created = TestFakes.BuildService(name: "billing");
        inner.AddAsync(request, Arg.Any<CancellationToken>()).Returns(created);

        var sut = new AuditApplicationsService(inner, auditRepo, dateTime);

        // Act
        var result = await sut.AddAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(created);
        await auditRepo.Received(1).AddAsync(
            Arg.Is<AuditLog>(log =>
                log.ServiceId == created.Id &&
                log.UserId == request.CreatedBy &&
                log.CreatedAt == created.CreatedAt &&
                log.Entry is ServiceCreatedEntry &&
                ((ServiceCreatedEntry)log.Entry).Name == request.Name &&
                ((ServiceCreatedEntry)log.Entry).Description == request.Description &&
                ((ServiceCreatedEntry)log.Entry).RepositoryUrl == request.RepositoryUrl &&
                ((ServiceCreatedEntry)log.Entry).GitLabProjectId == request.GitLabProjectId &&
                ((ServiceCreatedEntry)log.Entry).ContactEmail == request.ContactEmail),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldWriteServiceUpdatedLog_WhenUnderlyingReturnsService()
    {
        // Arrange
        var inner = Substitute.For<IApplicationsService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var dateTime = new DateTimeProvider();

        var request = TestFakes.BuildUpdateServiceRequest(name: "renamed");
        var updated = TestFakes.BuildService(id: request.Id, name: "renamed");
        updated.UpdatedAt = TestFakes.Now.AddMinutes(5);
        inner.UpdateAsync(request, Arg.Any<CancellationToken>()).Returns(updated);

        var sut = new AuditApplicationsService(inner, auditRepo, dateTime);

        // Act
        var result = await sut.UpdateAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(updated);
        await auditRepo.Received(1).AddAsync(
            Arg.Is<AuditLog>(log =>
                log.ServiceId == updated.Id &&
                log.UserId == request.UpdatedBy &&
                log.CreatedAt == updated.UpdatedAt &&
                log.Entry is ServiceUpdatedEntry &&
                ((ServiceUpdatedEntry)log.Entry).Name == request.Name &&
                ((ServiceUpdatedEntry)log.Entry).Description == request.Description &&
                ((ServiceUpdatedEntry)log.Entry).RepositoryUrl == request.RepositoryUrl &&
                ((ServiceUpdatedEntry)log.Entry).GitLabProjectId == request.GitLabProjectId &&
                ((ServiceUpdatedEntry)log.Entry).ContactEmail == request.ContactEmail),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldWriteServiceDeletedLog_WhenUnderlyingReturnsService()
    {
        // Arrange
        var inner = Substitute.For<IApplicationsService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var dateTime = new DateTimeProvider();

        var request = TestFakes.BuildDeleteServiceRequest();
        var deleted = TestFakes.BuildService(id: request.Id);
        inner.DeleteAsync(request, Arg.Any<CancellationToken>()).Returns(deleted);

        var sut = new AuditApplicationsService(inner, auditRepo, dateTime);

        // Act
        var result = await sut.DeleteAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(deleted);
        await auditRepo.Received(1).AddAsync(
            Arg.Is<AuditLog>(log =>
                log.ServiceId == request.Id &&
                log.UserId == request.DeletedBy &&
                log.Entry is ServiceDeletedEntry &&
                ((ServiceDeletedEntry)log.Entry).Name == deleted.Name &&
                ((ServiceDeletedEntry)log.Entry).Description == deleted.Description &&
                ((ServiceDeletedEntry)log.Entry).RepositoryUrl == deleted.RepositoryUrl &&
                ((ServiceDeletedEntry)log.Entry).GitLabProjectId == deleted.GitLabProjectId &&
                ((ServiceDeletedEntry)log.Entry).ContactEmail == deleted.ContactEmail),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldUseDateTimeUtcNowForLogTimestamp_WhenCalled()
    {
        // Arrange
        var inner = Substitute.For<IApplicationsService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var dateTime = Substitute.For<IDateTimeProvider>();
        var now = TestFakes.Now.AddMinutes(7);
        dateTime.GetNow().Returns(now);

        var request = TestFakes.BuildDeleteServiceRequest();
        var deleted = TestFakes.BuildService(id: request.Id);
        inner.DeleteAsync(request, Arg.Any<CancellationToken>()).Returns(deleted);

        var sut = new AuditApplicationsService(inner, auditRepo, dateTime);

        // Act
        await sut.DeleteAsync(request, CancellationToken.None);

        // Assert
        await auditRepo.Received(1).AddAsync(
            Arg.Is<AuditLog>(log => log.CreatedAt == now),
            Arg.Any<CancellationToken>());
    }
}
