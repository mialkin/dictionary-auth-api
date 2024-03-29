using System.Security.Claims;
using Dictionary.Auth.Controllers.Auth.Constants;
using Dictionary.Auth.Controllers.Auth.Requests;
using Dictionary.Auth.Controllers.Auth.Settings;
using Dictionary.Auth.UseCases.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Dictionary.Auth.Controllers.Auth;

[Authorize]
public class AuthController(ISender sender, IOptions<AuthSettings> options) : Controller
{
    [HttpGet("/login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            return Redirect(options.Value.LoginRedirectUri!);
        }

        return View();
    }

    [HttpPost("/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromForm] LoginRequest request, CancellationToken cancellationToken)
    {
        // TODO protect against brute-force attack:
        // Consider adding: 1) metrics + alerts 2) captcha
        var result = await sender.Send(new LoginCommand(request.Login, request.Password), cancellationToken);

        if (!result)
        {
            ViewBag.ErrorMessage = "Invalid credentials";
            return View();
        }

        await HttpContext.SignInAsync(
            principal: new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims: new[] { new Claim(type: ClaimTypes.Name, value: request.Login) },
                    authenticationType: DefaultAuthenticationScheme.Name
                )
            ),
            properties: new AuthenticationProperties { IsPersistent = true }
        );

        return Redirect(options.Value.LoginRedirectUri!);
    }

    [HttpPost("/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            scheme: DefaultAuthenticationScheme.Name,
            properties: new AuthenticationProperties { IsPersistent = true }
        );

        return Redirect(options.Value.LogoutRedirectUri!);
    }
}
