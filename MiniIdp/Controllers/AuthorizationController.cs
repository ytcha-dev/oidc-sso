using Microsoft.AspNetCore;
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

                if(application == null)
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

            throw new InvalidOperationException("The specified grant type is not supported.");
        }
    }
}
