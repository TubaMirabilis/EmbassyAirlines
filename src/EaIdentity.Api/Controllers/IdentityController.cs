using EaCommon.Errors;
using EaIdentity.Application.Dtos;
using FluentResults;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace EaIdentity.Api.Controllers;

[ApiController]
public class IdentityController : Controller
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
        if (result.IsSuccess)
        {
            var value = result.Value;
            return Ok(new AuthSuccessDto(value.Token, value.RefreshToken));
        }
        var firstError = result.Errors[0];
        if (firstError is DuplicateEmailError)
        {
            return Conflict("Email already exists.");
        }
        return HandleError(firstError);
    }
    [HttpPost("/login/")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequestDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(dto, ct);
        if (result.IsSuccess)
        {
            var value = result.Value;
            return Ok(new AuthSuccessDto(value.Token, value.RefreshToken));
        }
        var firstError = result.Errors[0];
        return HandleError(firstError);
    }
    [HttpPost("/refresh/")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(dto, ct);
        if (result.IsSuccess)
        {
            var value = result.Value;
            return Ok(new AuthSuccessDto(value.Token, value.RefreshToken));
        }
        var firstError = result.Errors[0];
        return HandleError(firstError);
    }
    private IActionResult HandleError(IError error)
    {
        if (error is NotFoundError)
        {
            return NotFound(error.Message);
        }
        if (error is AuthError)
        {
            return Unauthorized(error.Message);
        }
        return BadRequest(error.Message);
    }
}