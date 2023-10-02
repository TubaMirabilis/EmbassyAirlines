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
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto dto)
    {
        
    }

    [HttpPost("/login/")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        var authResponse = await _identityService.LoginAsync(request.Email, request.Password);

        if (!authResponse.Success)
        {
            return BadRequest(new AuthFailedResponse
            {
                Errors = authResponse.Errors
            });
        }

        return Ok(new AuthSuccessResponse
        {
            Token = authResponse.Token,
            RefreshToken = authResponse.RefreshToken
        });
    }

    [HttpPost(ApiRoutes.Identity.Refresh)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var authResponse = await _identityService.RefreshTokenAsync(request.Token, request.RefreshToken);

        if (!authResponse.Success)
        {
            return BadRequest(new AuthFailedResponse
            {
                Errors = authResponse.Errors
            });
        }

        return Ok(new AuthSuccessResponse
        {
            Token = authResponse.Token,
            RefreshToken = authResponse.RefreshToken
        });
    }

    [HttpPost(ApiRoutes.Identity.FacebookAuth)]
    public async Task<IActionResult> FacebookAuth([FromBody] UserFacebookAuthRequest request)
    {
        var authResponse = await _identityService.LoginWithFacebookAsync(request.AccessToken);

        if (!authResponse.Success)
        {
            return BadRequest(new AuthFailedResponse
            {
                Errors = authResponse.Errors
            });
        }

        return Ok(new AuthSuccessResponse
        {
            Token = authResponse.Token,
            RefreshToken = authResponse.RefreshToken
        });
    }
}