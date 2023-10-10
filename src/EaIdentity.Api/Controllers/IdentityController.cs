using EaIdentity.Application.Dtos;
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
        if (result.Success)
        {
            return Ok(new AuthSuccessDto(result.Token, result.RefreshToken));
        }
        return BadRequest(new AuthFailedDto(result.Errors));
        // Fail
        return BadRequest(new AuthFailedDto(ex.ValidationError.Errors));
    }
    // [HttpPost("/register/")]
    // public async Task<IActionResult> Register([FromBody] UserRegistrationDto dto, CancellationToken ct)
    // {
    //     try
    //     {
    //         var result = await _mediator.Send(dto, ct);
    //         if (result.Success)
    //         {
    //             return Ok(new AuthSuccessDto(result.Token, result.RefreshToken));
    //         }
    //         return BadRequest(new AuthFailedDto(result.Errors));
    //     }
    //     catch (ValidationException ex)
    //     {
    //         return BadRequest(new AuthFailedDto(ex.ValidationError.Errors));
    //     }
    // }

    [HttpPost("/login/")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequestDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(dto, ct);
        if (result.Success)
        {
            return Ok(new AuthSuccessDto(result.Token, result.RefreshToken));
        }
        return BadRequest(new AuthFailedDto(result.Errors));
    }

    [HttpPost("/refresh/")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto, CancellationToken ct)
    {
        // var authResponse = await _identityService.RefreshTokenAsync(request.Token, request.RefreshToken);
        var result = await _mediator.Send(dto, ct);
        if (result.Success)
        {
            return Ok(new AuthSuccessDto(result.Token, result.RefreshToken));
        }
        return BadRequest(new AuthFailedDto(result.Errors));
    }
}