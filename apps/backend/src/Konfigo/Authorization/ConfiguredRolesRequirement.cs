using Microsoft.AspNetCore.Authorization;

namespace Konfigo.Authorization;

internal sealed record ConfiguredRolesRequirement(string PolicyKey) : IAuthorizationRequirement;
