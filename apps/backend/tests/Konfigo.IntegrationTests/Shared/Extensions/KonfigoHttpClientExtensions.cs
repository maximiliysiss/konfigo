using System;
using System.Net.Http;
using System.Threading.Tasks;
using Konfigo.Controllers.Models.Audit;
using Konfigo.Controllers.Models.Entry;
using Konfigo.Controllers.Models.Services;
using Konfigo.Controllers.Models.Versions;
using Konfigo.Domain.ValueType;
using Konfigo.IntegrationTests.Shared.Responses;

namespace Konfigo.IntegrationTests.Shared.Extensions;

public static class KonfigoHttpClientExtensions
{
    public static Task<HttpResponseMessage> SendGetMeAsync(this HttpClient client) =>
        client.GetAsync("/auth/me");

    public static async Task<MeResponse> GetMeAsync(this HttpClient client)
    {
        var response = await client.SendGetMeAsync();
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredKonfigoJsonAsync<MeResponse>();
    }

    public static Task<HttpResponseMessage> SendCreateServiceAsync(
        this HttpClient client,
        CreateOrUpdateServiceRequest request) =>
        client.PostAsKonfigoJsonAsync("/api/services", request);

    public static async Task<ServiceResponse> CreateServiceAsync(
        this HttpClient client,
        CreateOrUpdateServiceRequest request)
    {
        var response = await client.SendCreateServiceAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredKonfigoJsonAsync<ServiceResponse>();
    }

    public static Task<HttpResponseMessage> SendUpdateServiceAsync(
        this HttpClient client,
        Guid serviceId,
        CreateOrUpdateServiceRequest request) =>
        client.PutAsKonfigoJsonAsync($"/api/services/{serviceId}", request);

    public static Task<HttpResponseMessage> SendDeleteServiceAsync(this HttpClient client, Guid serviceId) =>
        client.DeleteAsync($"/api/services/{serviceId}");

    public static Task<HttpResponseMessage> SendGetServiceAsync(this HttpClient client, Guid serviceId) =>
        client.GetAsync($"/api/services/{serviceId}");

    public static async Task AddMemberAsync(this HttpClient client, Guid serviceId, Guid userId)
    {
        var response = await client.SendAddMemberAsync(serviceId, userId);
        response.EnsureSuccessStatusCode();
    }

    public static async Task RemoveMemberAsync(this HttpClient client, Guid serviceId, Guid userId)
    {
        var response = await client.SendRemoveMemberAsync(serviceId, userId);
        response.EnsureSuccessStatusCode();
    }

    public static Task<HttpResponseMessage> SendAddMemberAsync(this HttpClient client, Guid serviceId, Guid userId)
        => client.PostAsync($"/api/services/{serviceId}/members?userId={userId}", null);

    public static Task<HttpResponseMessage> SendRemoveMemberAsync(this HttpClient client, Guid serviceId, Guid userId)
        => client.DeleteAsync($"/api/services/{serviceId}/members?userId={userId}");

    public static async Task<ServiceResponse?> GetServiceAsync(this HttpClient client, Guid serviceId)
    {
        var response = await client.SendGetServiceAsync(serviceId);
        response.EnsureSuccessStatusCode();
        return await response.ReadKonfigoJsonAsync<ServiceResponse>();
    }

    public static Task<HttpResponseMessage> SendSearchServicesAsync(
        this HttpClient client,
        SearchServicesRequest request) =>
        client.PostAsKonfigoJsonAsync("/api/services/search", request);

    public static async Task<PageResponse<ServiceResponse>> SearchServicesAsync(
        this HttpClient client,
        SearchServicesRequest request)
    {
        var response = await client.SendSearchServicesAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredKonfigoJsonAsync<PageResponse<ServiceResponse>>();
    }

    public static async Task<VersionResponse> CreateConfigVersionAsync(
        this HttpClient client,
        Guid serviceId,
        CreateConfigVersionRequest request)
    {
        var response = await client.SendCreateConfigVersionAsync(serviceId, request);
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredKonfigoJsonAsync<VersionResponse>();
    }

    public static Task<HttpResponseMessage> SendCreateConfigVersionAsync(
        this HttpClient client,
        Guid serviceId,
        CreateConfigVersionRequest request) =>
        client.PostAsKonfigoJsonAsync($"/api/configversions/{serviceId}", request);

    public static Task<HttpResponseMessage> SendUpdateConfigVersionAsync(
        this HttpClient client,
        Guid serviceId,
        Guid versionId,
        UpdateConfigVersionRequest request) =>
        client.PutAsKonfigoJsonAsync($"/api/configversions/{serviceId}/{versionId}", request);

    public static async Task<VersionResponse[]> GetConfigVersionsAsync(this HttpClient client, Guid serviceId)
    {
        var response = await client.GetAsync($"/api/configversions/{serviceId}");
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredKonfigoJsonAsync<VersionResponse[]>();
    }

    public static async Task<VersionResponse> GetConfigVersionAsync(
        this HttpClient client,
        Guid serviceId,
        Guid versionId)
    {
        var response = await client.GetAsync($"/api/configversions/{serviceId}/{versionId}");
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredKonfigoJsonAsync<VersionResponse>();
    }

    public static async Task<EntryResponse> CreateConfigEntryAsync(
        this HttpClient client,
        Guid serviceId,
        Guid versionId,
        CreateConfigEntryRequest request)
    {
        var response = await client.SendCreateConfigEntryAsync(serviceId, versionId, request);
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredKonfigoJsonAsync<EntryResponse>();
    }

    public static Task<HttpResponseMessage> SendCreateConfigEntryAsync(
        this HttpClient client,
        Guid serviceId,
        Guid versionId,
        CreateConfigEntryRequest request) =>
        client.PostAsKonfigoJsonAsync($"/api/configentries/{serviceId}/{versionId}", request);

    public static Task<HttpResponseMessage> SendUpdateConfigEntryAsync(
        this HttpClient client,
        Guid serviceId,
        Guid versionId,
        Guid entryId,
        UpdateConfigEntryRequest request) =>
        client.PutAsKonfigoJsonAsync($"/api/configentries/{serviceId}/{versionId}/{entryId}", request);

    public static Task<HttpResponseMessage> SendSetConfigEntriesAsync(
        this HttpClient client,
        Guid serviceId,
        Guid versionId,
        SetConfigEntryRequest[] request) =>
        client.PutAsKonfigoJsonAsync($"/api/configentries/{serviceId}/{versionId}/set", request);

    public static Task<HttpResponseMessage> SendDeleteConfigEntryAsync(
        this HttpClient client,
        Guid serviceId,
        Guid versionId,
        Guid entryId) =>
        client.DeleteAsync($"/api/configentries/{serviceId}/{versionId}/{entryId}");

    public static async Task<EntryResponse[]> GetConfigEntriesAsync(
        this HttpClient client,
        Guid serviceId,
        Guid versionId)
    {
        var response = await client.GetAsync($"/api/configentries/{serviceId}/{versionId}");
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredKonfigoJsonAsync<EntryResponse[]>();
    }

    public static async Task<PageResponse<AuditEntryResponse>> SearchAuditLogsAsync(
        this HttpClient client,
        Guid serviceId,
        SearchAuditRequest request)
    {
        var response = await client.PostAsKonfigoJsonAsync($"/api/audit/{serviceId}/search", request);
        response.EnsureSuccessStatusCode();
        return await response.ReadRequiredKonfigoJsonAsync<PageResponse<AuditEntryResponse>>();
    }
}
