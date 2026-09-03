using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PersonalFinance.Client;
using PersonalFinance.Client.Data;
using PersonalFinance.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IndexedDbService>();
builder.Services.AddScoped<LocalAccountService>();
builder.Services.AddScoped<LocalCategoryService>();
builder.Services.AddScoped<LocalTransactionService>();

await builder.Build().RunAsync();
