using MiniIdp.Data;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace MiniIdp.Application.Configuration
{
    public class TestClient : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public TestClient(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync(cancellationToken);

            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

            if (await manager.FindByClientIdAsync("my-console-app", cancellationToken) is null)
            {
                await manager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    // Client Id
                    ClientId = "my-console-app",
                    // Client Secret
                    ClientSecret = "388D45FA-B36B-4988-BA59-B187D329C207",
                    DisplayName = "My Console App",
                    Permissions =
                    {
                        // 允許使用 token endpoint
                        Permissions.Endpoints.Token,
                        // 允許使用 client credentials flow                
                        Permissions.GrantTypes.ClientCredentials,
                    }
                }, cancellationToken);
            }

            //introspection client
            if (await manager.FindByClientIdAsync("resource_server") == null)
            {
                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = "resource_server",
                    ClientSecret = "846B62D0-DEF9-4215-A99D-86E6B8DAB342",
                    DisplayName = "Resource Server Introspection",
                    Permissions =
                    {
                        Permissions.Endpoints.Introspection
                    }
                };

                await manager.CreateAsync(descriptor);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
