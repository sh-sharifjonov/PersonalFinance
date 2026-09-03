using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PersonalFinance.Client;
using PersonalFinance.Client.Auth;
using PersonalFinance.Client.Data;
using PersonalFinance.Client.Services;
using PersonalFinance.Client.Sync;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped<IndexedDbService>();
builder.Services.AddScoped<LocalAccountService>();
builder.Services.AddScoped<LocalCategoryService>();
builder.Services.AddScoped<LocalTransactionService>();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());

builder.Services.AddScoped<AuthService>();
builder.Services.AddTransient<AuthorizationMessageHandler>();

builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient("ApiAuthorized", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddScoped<OutboxService>();
builder.Services.AddScoped<SyncStateService>();
builder.Services.AddScoped<ConnectivityService>();
builder.Services.AddScoped<SyncService>();

var host = builder.Build();

// Fire-and-forget: connectivity/JS-interop setup must never block the app from
// rendering. A failure here should not produce a blank page.
_ = InitializeSyncAsync(host.Services);

await host.RunAsync();

static async Task InitializeSyncAsync(IServiceProvider services)
{
    try
    {
        var connectivity = services.GetRequiredService<ConnectivityService>();
        await connectivity.InitializeAsync();

        var sync = services.GetRequiredService<SyncService>();
        connectivity.OnlineStatusChanged += isOnline =>
        {
            if (isOnline)
            {
                _ = sync.SyncAsync();
            }
        };

        if (connectivity.IsOnline)
        {
            _ = sync.SyncAsync();
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Sync initialization failed: {ex}");
    }
}
