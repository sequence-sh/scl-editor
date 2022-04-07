using BlazorDownloadFile;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Reductech.Utilities.SCLEditor.Util;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.RegisterForJavaScript<Playground>(
    "scl-playground-react",
    "sclPlaygroundInit"
);

//builder.Services.AddScoped(
//    sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
//);

builder.Services.AddBlazorDownloadFile();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<CompoundFileSystem>();

builder.Services.AddMudBlazorJsApi().AddMudServices();

builder.Services.AddHttpClient();

await builder.Build().RunAsync();
