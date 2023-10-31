using EaIdentity.Application.Dtos;
using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace EaIdentity.Api.Controllers;

public class IdentityController : ApiController
{
    private readonly IMediator _mediator;
    public IdentityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("/register/")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(dto, ct);
        return result.Match(
            succ => Ok(new AuthSuccessDto(result.Value.Token, result.Value.RefreshToken)),
            errors => Problem(errors));
    }
    [HttpPost("/login/")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequestDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(dto, ct);
        return result.Match(
            succ => Ok(new AuthSuccessDto(result.Value.Token, result.Value.RefreshToken)),
            errors => Problem(errors));
    }

    [HttpPost("/refresh/")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(dto, ct);
        return result.Match(
            succ => Ok(new AuthSuccessDto(result.Value.Token, result.Value.RefreshToken)),
            errors => Problem(errors));
    }
}