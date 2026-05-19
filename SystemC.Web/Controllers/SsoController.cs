using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace SystemC.Web.Controllers
{
    public class SsoController : Controller
    {
        /// <summary>
        /// 手動觸發 OIDC 跳轉，returnUrl 登入完回哪
        /// </summary>
        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            if (User.Identity?.IsAuthenticated == true)
                return Redirect(returnUrl);

            return Challenge(
                new AuthenticationProperties { RedirectUri = returnUrl },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// OIDC callback 由 middleware 自動處理，這個 action 只處理失敗情境
        /// </summary>
        [HttpGet]
        public IActionResult LoginCallback()
        {
            // middleware 會在 /signin-oidc 自動接收 code 並寫入 Cookie
            // 正常情況下不會進到這裡
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// 登出：清除本地 Cookie + 通知 IDP End Session
        /// </summary>
        [HttpGet]
        [HttpPost]
        public IActionResult Logout()
        {
            return SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// 顯示目前登入狀態與 Claims（debug 用）
        /// </summary>
        [HttpGet]
        public IActionResult Status()
        {
            return View();
        }
    }
}