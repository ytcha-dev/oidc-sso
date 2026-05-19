using System.ComponentModel.DataAnnotations;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace MiniIdp.ViewModels
{
    public class ClientListItemViewModel
    {
        public string? ClientId { get; set; }
        public string? DisplayName { get; set; }
        public string? ClientType { get; set; }  // ✅ 新增
    }

    public class CreateClientViewModel
    {
        [Required(ErrorMessage = "必填")]
        [Display(Name = "Client ID")]
        public string ClientId { get; set; } = string.Empty;

        [Display(Name = "Client Secret")]
        public string? ClientSecret { get; set; } = Guid.NewGuid().ToString().ToUpper();

        [Required(ErrorMessage = "必填")]
        [Display(Name = "顯示名稱")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>confidential 或 public</summary>
        [Required]
        [Display(Name = "Client 類型")]
        public string ClientType { get; set; } = ClientTypes.Confidential;  // ✅ 新增

        [Display(Name = "Authorization Code Flow")]
        public bool AllowAuthorizationCodeFlow { get; set; } = true;

        [Display(Name = "Client Credentials Flow")]
        public bool AllowClientCredentialsFlow { get; set; }

        [Display(Name = "Refresh Token")]
        public bool AllowRefreshToken { get; set; }

        [Display(Name = "Redirect URIs（每行一個）")]
        public string? RedirectUris { get; set; }

        [Display(Name = "Post Logout Redirect URIs（每行一個）")]
        public string? PostLogoutRedirectUris { get; set; }

        [Display(Name = "email")]
        public bool ScopeEmail { get; set; } = true;

        [Display(Name = "profile")]
        public bool ScopeProfile { get; set; } = true;

        [Display(Name = "offline_access")]
        public bool ScopeOfflineAccess { get; set; }
    }

    public class EditClientViewModel
    {
        public string ClientId { get; set; } = string.Empty;

        [Display(Name = "Client Secret（留空則不變更）")]
        public string? ClientSecret { get; set; }

        [Required(ErrorMessage = "必填")]
        [Display(Name = "顯示名稱")]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Client 類型")]
        public string ClientType { get; set; } = ClientTypes.Confidential;  // ✅ 新增

        [Display(Name = "Authorization Code Flow")]
        public bool AllowAuthorizationCodeFlow { get; set; }

        [Display(Name = "Client Credentials Flow")]
        public bool AllowClientCredentialsFlow { get; set; }

        [Display(Name = "Refresh Token")]
        public bool AllowRefreshToken { get; set; }

        [Display(Name = "Redirect URIs（每行一個）")]
        public string? RedirectUris { get; set; }

        [Display(Name = "Post Logout Redirect URIs（每行一個）")]
        public string? PostLogoutRedirectUris { get; set; }

        [Display(Name = "email")]
        public bool ScopeEmail { get; set; }

        [Display(Name = "profile")]
        public bool ScopeProfile { get; set; }
    }
}