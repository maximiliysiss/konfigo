using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Services.LocalUsers;
using Konfigo.Application.Services.LocalUsers.Models;
using Konfigo.Authorization;
using Konfigo.Controllers.Models.Users;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ApiCreateRequest = Konfigo.Controllers.Models.Users.CreateLocalUserRequest;
using ApiUpdateRequest = Konfigo.Controllers.Models.Users.UpdateLocalUserRequest;
using AppCreateRequest = Konfigo.Application.Services.LocalUsers.Models.CreateLocalUserRequest;
using AppUpdateRequest = Konfigo.Application.Services.LocalUsers.Models.UpdateLocalUserRequest;

namespace Konfigo.Controllers;

[Route("api/users")]
[Authorize(Policy = AuthorizationPolicyNames.CanAll)]
[ApiController]
public sealed class LocalUsersController : ControllerBase
{
    private readonly ILocalUsersService _localUsersService;
    private readonly IOptionsMonitor<KonfigoAuthenticationOptions> _authenticationOptions;

    public LocalUsersController(
        ILocalUsersService localUsersService,
        IOptionsMonitor<KonfigoAuthenticationOptions> authenticationOptions)
    {
        _localUsersService = localUsersService;
        _authenticationOptions = authenticationOptions;
    }

    [HttpGet]
    public async Task<ActionResult<LocalUserResponse[]>> GetAll(CancellationToken cancellationToken)
    {
        if (!IsLocalProviderEnabled())
            return NotFound();

        var users = await _localUsersService.GetAllAsync(cancellationToken);

        return Ok(users.Select(ToResponse).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<LocalUserResponse>> Create([FromBody] ApiCreateRequest request, CancellationToken cancellationToken)
    {
        if (!IsLocalProviderEnabled())
            return NotFound();

        var result = await _localUsersService.CreateAsync(
            new AppCreateRequest(request.Username, request.Password, request.Role),
            cancellationToken);

        return result switch
        {
            CreateLocalUserResult.Created created => Ok(ToResponse(created.User)),
            CreateLocalUserResult.AlreadyExists => Conflict("A user with this username already exists."),
            CreateLocalUserResult.InvalidRole => BadRequest("Invalid role."),
            _ => BadRequest(),
        };
    }

    [HttpPut("{username}")]
    public async Task<ActionResult<LocalUserResponse>> Update(
        [FromRoute] string username,
        [FromBody] ApiUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsLocalProviderEnabled())
            return NotFound();

        if (request.Role is not null && !LocalUserRoles.IsValid(request.Role))
            return BadRequest("Invalid role.");

        var user = await _localUsersService.UpdateAsync(
            new AppUpdateRequest(username, request.Password, request.Role),
            cancellationToken);

        if (user is null)
            return NotFound();

        return Ok(ToResponse(user));
    }

    [HttpDelete("{username}")]
    public async Task<IActionResult> Delete([FromRoute] string username, CancellationToken cancellationToken)
    {
        if (!IsLocalProviderEnabled())
            return NotFound();

        if (string.Equals(HttpContext.GetUser().Id.Value, username, StringComparison.Ordinal))
            return BadRequest("You cannot delete your own account.");

        var deleted = await _localUsersService.DeleteAsync(new DeleteLocalUserRequest(username), cancellationToken);

        return deleted ? NoContent() : NotFound();
    }

    private bool IsLocalProviderEnabled() => _authenticationOptions.CurrentValue.Provider == AuthenticationProvider.Local;

    private static LocalUserResponse ToResponse(LocalUser user) => new()
    {
        Username = user.Username,
        Role = user.Role,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
    };
}
