using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace MiniIdp.Controllers
{
    public class AuthorizationController : Controller
    {
        private readonly IOpenIddictApplicationManager _applicationManager;

        public AuthorizationController(IOpenIddictApplicationManager applicationManager)
        {
            this._applicationManager = applicationManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("~/connect/token"), Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            if (request.IsClientCredentialsGrantType())
            {
                var application = await _applicationManager.FindByClientIdAsync(request.ClientId);

                if (application == null)
                {
                    throw new InvalidOperationException("The application cannot be found in the database.");
                }

                var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

                identity.AddClaim(new Claim(Claims.Subject, await _applicationManager.GetClientIdAsync(application))
                    .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

                identity.AddClaim(new Claim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application))
                    .SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

                var principal = new ClaimsPrincipal(identity);

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else if (request.IsAuthorizationCodeGrantType())
            {
                // ✅ 從 authorization code 取回當初 Authorize() 存入的 principal
                var principal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal
                    ?? throw new InvalidOperationException("The user principal cannot be retrieved.");

                // ✅ 直接用原本的 principal 簽發 token，回傳給 SystemA
                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new InvalidOperationException("The specified grant type is not supported.");
        }

        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

            // Retrieve the user principal stored in the authentication cookie.
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // If the user principal can't be extracted, redirect the user to the login page.
            if (!result.Succeeded)
            {
                return Challenge(
                    authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties
                    {
                        RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                            Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                    });
            }

            // Create a new claims principal
            var claims = new List<Claim>
            {
                // 'subject' claim which is required
                new Claim(Claims.Subject, result.Principal.Identity.Name),
                new Claim(Claims.Name, result.Principal.Identity.Name).SetDestinations(Destinations.AccessToken, Destinations.IdentityToken),
                new Claim(Claims.Email, result.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty).SetDestinations(Destinations.IdentityToken),
            };

            var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Set requested scopes (this is not done automatically)
            claimsPrincipal.SetScopes(request.GetScopes());

            // Signing in with the OpenIddict authentiction scheme trigger OpenIddict to issue a code (which can be exchanged for an access token)
            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpGet("~/connect/userinfo")]
        public async Task<IActionResult> UserInfo()
        {
            var claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

            return Ok(new
            {
                sub = claimsPrincipal.GetClaim(Claims.Subject),
                name = claimsPrincipal.GetClaim(Claims.Name),
                email = claimsPrincipal.GetClaim(Claims.Email),
            });
        }

        [HttpGet("~/connect/logout")]
        [HttpPost("~/connect/logout")]
        public async Task<IActionResult> Logout()
        {
            // 清除 MiniIdp 的 Cookie（使用者在 IDP 的 session）
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 通知 OpenIddict 完成 end_session 流程，並跳回 Client App 的 PostLogoutRedirectUri
            return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
    }
}
