using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ResourceA.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Services

            // Add services to the container.

            #region 端點不需使用 OpenIddict，應直接發送 HTTP 請求到 AuthServer 的 introspection endpoint 來驗證 access token

            //// 加入 Authentication，DefaultScheme 設定為 OpenIddict AuthenticationScheme
            //builder.Services.AddAuthentication(options =>
            //{
            //    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            //});

            //// 加入 OpenIddict validation
            //builder.Services.AddOpenIddict()
            //    .AddValidation(options =>
            //    {
            //        // AuthServer 網址
            //        options.SetIssuer("https://localhost:7238/");

            //        // 使用前篇設定 introspection 的 clientid/clientsecret
            //        options.UseIntrospection()
            //                .SetClientId("resource_server")
            //                .SetClientSecret("846B62D0-DEF9-4215-A99D-86E6B8DAB342");

            //        options.UseSystemNetHttp();
            //        options.UseAspNetCore();
            //    });

            #endregion


            // 標準 JWT Bearer 驗證，不需要 OpenIddict
            // 啟動時自動去 https://localhost:7238/.well-known/openid-configuration 抓公鑰並快取
            // 之後每次請求都用公鑰本地驗簽章，不需要打電話回 MiniIdp
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // MiniIdp 的網址，JWT Bearer 會自動處理：
                    // 1. 從 /.well-known/openid-configuration 取得 metadata
                    // 2. 從 metadata 裡的 jwks_uri 取得公鑰
                    // 3. 快取公鑰，用來本地驗證每一個進來的 JWT
                    options.Authority = "https://localhost:7238/";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // M2M 場景 token 通常不帶 audience，暫時關閉驗證
                        // 待之後 SSO 設定 scope/audience 後可改為 true
                        ValidateAudience = false
                    };
                });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            #endregion

            var app = builder.Build();

            #region Hosted Service

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            #endregion

            app.Run();
        }
    }
}
