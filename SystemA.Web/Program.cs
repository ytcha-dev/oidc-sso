using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace SystemA.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var oidcConfig = builder.Configuration.GetSection("Oidc");

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.Cookie.Name = "SystemA.Session"; // ✅ 獨立命名
                })
                .AddOpenIdConnect(options =>
                {
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme; // ✅ 明確指定

                    options.Authority = oidcConfig["Authority"];
                    options.ClientId = oidcConfig["ClientId"];
                    options.ClientSecret = oidcConfig["ClientSecret"];
                    options.ResponseType = OpenIdConnectResponseType.Code;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");

                    options.SaveTokens = false;      // ✅ 關掉，不把 token 塞進 Cookie
                    options.GetClaimsFromUserInfoEndpoint = false; // ✅ userinfo 暫時關閉

                    options.MapInboundClaims = false;
                    options.TokenValidationParameters.NameClaimType = "sub"; // ✅ User.Identity.Name = sub

                    options.BackchannelHttpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };

                    options.Events = new OpenIdConnectEvents
                    {
                        // ✅ 遠端登入失敗時，導回首頁而非顯示醜陋的錯誤頁
                        OnRemoteFailure = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("/");
                            return Task.CompletedTask;
                        }
                    };
                });

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();  // ✅ 必須在 UseAuthorization 之前
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
