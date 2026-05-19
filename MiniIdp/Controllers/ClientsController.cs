using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using MiniIdp.ViewModels;

namespace MiniIdp.Controllers
{
    public class ClientsController : Controller
    {
        private readonly IOpenIddictApplicationManager _manager;

        public ClientsController(IOpenIddictApplicationManager manager)
        {
            _manager = manager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clients = new List<ClientListItemViewModel>();

            await foreach (var app in _manager.ListAsync())
            {
                clients.Add(new ClientListItemViewModel
                {
                    ClientId = await _manager.GetClientIdAsync(app),
                    DisplayName = await _manager.GetDisplayNameAsync(app),
                    ClientType = await _manager.GetClientTypeAsync(app),  // ✅
                });
            }

            return View(clients);
        }

        [HttpGet]
        public IActionResult Create() => View(new CreateClientViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateClientViewModel model)
        {
            var isPublic = model.ClientType == ClientTypes.Public;

            // Public Client 不能有 Secret
            if (!isPublic && string.IsNullOrWhiteSpace(model.ClientSecret))
                ModelState.AddModelError(nameof(model.ClientSecret), "Confidential Client 必須設定 Secret");

            if (!ModelState.IsValid)
                return View(model);

            if (await _manager.FindByClientIdAsync(model.ClientId) is not null)
            {
                ModelState.AddModelError(nameof(model.ClientId), "此 Client ID 已存在");
                return View(model);
            }

            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = model.ClientId,
                ClientSecret = isPublic ? null : model.ClientSecret,  // ✅ Public 不設 Secret
                ClientType = model.ClientType,                         // ✅
                DisplayName = model.DisplayName,
            };

            descriptor.Permissions.Add(Permissions.Endpoints.Token);

            if (model.AllowAuthorizationCodeFlow)
            {
                descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
                descriptor.Permissions.Add(Permissions.Endpoints.EndSession);
                descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
                descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
                // Public Client 強制 PKCE，Confidential 也建議開啟
                descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);

                foreach (var uri in ParseUris(model.RedirectUris))
                    descriptor.RedirectUris.Add(uri);

                foreach (var uri in ParseUris(model.PostLogoutRedirectUris))
                    descriptor.PostLogoutRedirectUris.Add(uri);
            }

            if (model.AllowClientCredentialsFlow)
                descriptor.Permissions.Add(Permissions.GrantTypes.ClientCredentials);

            if (model.AllowRefreshToken)
                descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);

            if (model.ScopeEmail) descriptor.Permissions.Add(Permissions.Scopes.Email);
            if (model.ScopeProfile) descriptor.Permissions.Add(Permissions.Scopes.Profile);

            await _manager.CreateAsync(descriptor);

            TempData["Success"] = $"Client「{model.DisplayName}」建立成功！";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string clientId)
        {
            var app = await _manager.FindByClientIdAsync(clientId);
            if (app is null) return NotFound();

            var descriptor = new OpenIddictApplicationDescriptor();
            await _manager.PopulateAsync(descriptor, app);

            var model = new EditClientViewModel
            {
                ClientId = clientId,
                DisplayName = descriptor.DisplayName ?? string.Empty,
                ClientType = descriptor.ClientType ?? ClientTypes.Confidential,  // ✅
                AllowAuthorizationCodeFlow = descriptor.Permissions.Contains(Permissions.GrantTypes.AuthorizationCode),
                AllowClientCredentialsFlow = descriptor.Permissions.Contains(Permissions.GrantTypes.ClientCredentials),
                AllowRefreshToken = descriptor.Permissions.Contains(Permissions.GrantTypes.RefreshToken),
                ScopeEmail = descriptor.Permissions.Contains(Permissions.Scopes.Email),
                ScopeProfile = descriptor.Permissions.Contains(Permissions.Scopes.Profile),
                RedirectUris = string.Join('\n', descriptor.RedirectUris),
                PostLogoutRedirectUris = string.Join('\n', descriptor.PostLogoutRedirectUris),
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditClientViewModel model)
        {
            var isPublic = model.ClientType == ClientTypes.Public;

            if (!isPublic && string.IsNullOrWhiteSpace(model.ClientSecret))
            {
                // 編輯時留空代表不改，不需要報錯
            }

            if (!ModelState.IsValid)
                return View(model);

            var app = await _manager.FindByClientIdAsync(model.ClientId);
            if (app is null) return NotFound();

            var descriptor = new OpenIddictApplicationDescriptor();
            await _manager.PopulateAsync(descriptor, app);

            descriptor.DisplayName = model.DisplayName;
            descriptor.ClientType = model.ClientType;  // ✅

            if (isPublic)
                descriptor.ClientSecret = null;
            else if (!string.IsNullOrWhiteSpace(model.ClientSecret))
                descriptor.ClientSecret = model.ClientSecret;

            descriptor.Permissions.Clear();
            descriptor.Requirements.Clear();
            descriptor.RedirectUris.Clear();
            descriptor.PostLogoutRedirectUris.Clear();

            descriptor.Permissions.Add(Permissions.Endpoints.Token);

            if (model.AllowAuthorizationCodeFlow)
            {
                descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
                descriptor.Permissions.Add(Permissions.Endpoints.EndSession);
                descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
                descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
                descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);

                foreach (var uri in ParseUris(model.RedirectUris))
                    descriptor.RedirectUris.Add(uri);

                foreach (var uri in ParseUris(model.PostLogoutRedirectUris))
                    descriptor.PostLogoutRedirectUris.Add(uri);
            }

            if (model.AllowClientCredentialsFlow)
                descriptor.Permissions.Add(Permissions.GrantTypes.ClientCredentials);

            if (model.AllowRefreshToken)
                descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);

            if (model.ScopeEmail) descriptor.Permissions.Add(Permissions.Scopes.Email);
            if (model.ScopeProfile) descriptor.Permissions.Add(Permissions.Scopes.Profile);

            await _manager.UpdateAsync(app, descriptor);

            TempData["Success"] = $"Client「{model.ClientId}」已更新！";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string clientId)
        {
            var app = await _manager.FindByClientIdAsync(clientId);
            if (app is not null)
                await _manager.DeleteAsync(app);

            TempData["Success"] = $"Client「{clientId}」已刪除";
            return RedirectToAction(nameof(Index));
        }

        private static IEnumerable<Uri> ParseUris(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) yield break;

            foreach (var line in input.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Uri.TryCreate(line, UriKind.Absolute, out var uri))
                    yield return uri;
            }
        }
    }
}