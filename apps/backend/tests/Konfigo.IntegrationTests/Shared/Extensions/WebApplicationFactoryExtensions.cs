using System;
using System.Net.Http;
using Konfigo.IntegrationTests.Shared.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Konfigo.IntegrationTests.Shared.Extensions;

internal static class WebApplicationFactoryExtensions
{
    public static HttpClient CreateAdminClient<TEntryPoint>(this WebApplicationFactory<TEntryPoint> factory)
        where TEntryPoint : class
        => factory.CreateAuthenticatedClient(roles: "admin");

    public static HttpClient CreateUserClient<TEntryPoint>(this WebApplicationFactory<TEntryPoint> factory)
        where TEntryPoint : class
        => factory.CreateAuthenticatedClient(roles: "user");

    public static HttpClient CreateAnonymousClient<TEntryPoint>(this WebApplicationFactory<TEntryPoint> factory)
        where TEntryPoint : class
        => factory.CreateClient();

    public static HttpClient CreateAuthenticatedClient<TEntryPoint>(
        this WebApplicationFactory<TEntryPoint> factory,
        string roles = "admin",
        string services = "all",
        Guid? userId = null)
        where TEntryPoint : class
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, (userId ?? Guid.NewGuid()).ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, roles);
        client.DefaultRequestHeaders.Add(TestAuthHandler.ServicesHeader, services);
        return client;
    }
}
