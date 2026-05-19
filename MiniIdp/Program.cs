using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MiniIdp.Application.Configuration;
using MiniIdp.Data;
using OpenIddict.Abstractions;

namespace MiniIdp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Services

            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
                options.UseOpenIddict();
            });

            #region OpenIddict M2M 版本

            /* OpenIddict M2M 版本
            builder.Services.AddOpenIddict()
                .AddCore(options =>
                {
                    options.UseEntityFrameworkCore()
                        .UseDbContext<AppDbContext>();
                })
                .AddServer(options =>
                {
                    // 啟用需要的授權流程
                    options
                        // 允許 auth server 支援 client credentials grant
                        .AllowClientCredentialsFlow();

                    // 設置端點
                    options
                        // 設定取得 access token 的 endpoint；換 token 的地方
                        .SetTokenEndpointUris("/connect/token")
                        // 設定 introspection endpoint
                        .SetIntrospectionEndpointUris("/connect/introspect");

                    // 開發環境配置臨時憑證，生產環境建議使用 X.509 certificates
                    options
                        // 產生開發用的加密金鑰，production 建議用存在本機的 X.509 certificates
                        .AddEphemeralEncryptionKey()
                        .AddEphemeralSigningKey()
                        // 停用 access token 加密，production 不建議使用
                        .DisableAccessTokenEncryption();

                    // 啟用 ASP.NET Core 的集成，這樣 OpenIddict 就會使用 ASP.NET Core 的中介軟體來處理授權請求
                    options
                        .UseAspNetCore()
                        // 啟用 token endpoint 的中介軟體，這樣當有請求到 /connect/token 時，OpenIddict 就會處理這個請求，也可以在這裡添加自定義的授權邏輯
                        .EnableTokenEndpointPassthrough();
                }); 
             */

            #endregion

            #region OpenIddict Authorization Code Flow 版本

            /* OpenIddict Authorization Code Flow 版本
             builder.Services.AddOpenIddict()
                .AddCore(options =>
                {
                    options.UseEntityFrameworkCore()
                        .UseDbContext<AppDbContext>();
                })
                .AddServer(options =>
                {
                    // 啟用需要的授權流程
                    options
                        // 允許 auth server 支援 authorization code grant
                        .AllowAuthorizationCodeFlow()
                        // 允許 Resource Owner Password Credentials Flow，不建議新系統優先使用
                        //.AllowPasswordFlow()
                        // 強制 authorization code flow 使用 PKCE
                        .RequireProofKeyForCodeExchange()
                        // 允許 auth server 支援 refresh token grant
                        .AllowRefreshTokenFlow();

                    // 設置端點
                    options
                        // 設定取得 access token 的 endpoint；換 token 的地方
                        .SetTokenEndpointUris("/connect/token")
                        // 登入授權入口
                        .SetAuthorizationEndpointUris("/connect/authorize")
                        // OIDC 的使用者資料端點；Client 拿 access token 後，可以呼叫取得使用者資料的 endpoint
                        .SetUserInfoEndpointUris("/connect/userinfo");
                    ;

                    // 開發環境配置臨時憑證，生產環境建議使用 X.509 certificates
                    options
                        // 產生開發用的加密金鑰，production 建議用存在本機的 X.509 certificates
                        .AddEphemeralEncryptionKey()
                        .AddEphemeralSigningKey()
                        // 停用 access token 加密，production 不建議使用
                        .DisableAccessTokenEncryption()
                        ;

                    // 設置授權伺服器支援的 scopes；Client 在請求 access token 時會指定需要哪些 scopes，授權伺服器會根據這些 scopes 來決定 access token 中包含哪些權限
                    options.RegisterScopes(
                        OpenIddictConstants.Scopes.OpenId,
                        OpenIddictConstants.Scopes.Email,
                        OpenIddictConstants.Scopes.Profile,
                        OpenIddictConstants.Scopes.OfflineAccess);

                    // 啟用 ASP.NET Core 的集成，這樣 OpenIddict 就會使用 ASP.NET Core 的中介軟體來處理授權請求
                    options
                        .UseAspNetCore()
                        // 啟用 authorization endpoint 的中介軟體，這樣當有請求到 /connect/authorize 時，OpenIddict 就會處理這個請求，也可以在這裡添加自定義的授權邏輯
                        .EnableAuthorizationEndpointPassthrough()
                        // 啟用 token endpoint 的中介軟體，這樣當有請求到 /connect/token 時，OpenIddict 就會處理這個請求，也可以在這裡添加自定義的授權邏輯
                        .EnableTokenEndpointPassthrough()
                        // 啟用 userinfo endpoint 的中介軟體，這樣當有請求到 /connect/userinfo 時，OpenIddict 就會處理這個請求，也可以在這裡添加自定義的授權邏輯
                        .EnableUserInfoEndpointPassthrough()
                        ;
                })
                .AddValidation(options =>
                {
                    options.UseLocalServer();
                    options.UseAspNetCore();
                });

             */

            #endregion

            // 合併配置
            builder.Services.AddOpenIddict()
                .AddCore(options =>
                {
                    options.UseEntityFrameworkCore()
                        .UseDbContext<AppDbContext>();
                })
                .AddServer(options =>
                {
                    options
                        .AllowClientCredentialsFlow()
                        .AllowAuthorizationCodeFlow()
                        .RequireProofKeyForCodeExchange()
                        .AllowRefreshTokenFlow();

                    options
                        .SetTokenEndpointUris("/connect/token")
                        .SetAuthorizationEndpointUris("/connect/authorize")
                        .SetUserInfoEndpointUris("/connect/userinfo")
                        .SetEndSessionEndpointUris("/connect/logout");  // ✅ 正確名稱

                    options
                        .AddEphemeralEncryptionKey()
                        .AddEphemeralSigningKey()
                        .DisableAccessTokenEncryption();

                    options.RegisterScopes(
                        OpenIddictConstants.Scopes.OpenId,
                        OpenIddictConstants.Scopes.Email,
                        OpenIddictConstants.Scopes.Profile,
                        OpenIddictConstants.Scopes.OfflineAccess);

                    options
                        .UseAspNetCore()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableTokenEndpointPassthrough()
                        .EnableUserInfoEndpointPassthrough()
                        .EnableEndSessionEndpointPassthrough();  // ✅ 正確名稱
                })
                .AddValidation(options =>
                {
                    options.UseLocalServer();
                    options.UseAspNetCore();
                });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.Cookie.Name = "MiniIdp.Session"; // ✅ 明確命名，避免混淆
                });

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddHostedService<TestClient>();

            #endregion

            #region Hosted Service

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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            #endregion

            app.Run();
        }
    }
}