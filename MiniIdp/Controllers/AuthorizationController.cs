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
                // Retrieve the claims principal stored in the authorization code
                var claimsPrincipal = (await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;
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
                new Claim(OpenIddictConstants.Claims.Subject, result.Principal.Identity.Name),
                new Claim("some claim", "some value").SetDestinations(OpenIddictConstants.Destinations.AccessToken)
            };

            var claimsIdentity = new ClaimsIdentity(claims, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Set requested scopes (this is not done automatically)
            claimsPrincipal.SetScopes(request.GetScopes());

            // Signing in with the OpenIddict authentiction scheme trigger OpenIddict to issue a code (which can be exchanged for an access token)
            return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
    }
}
