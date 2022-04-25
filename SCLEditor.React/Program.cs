using BlazorDownloadFile;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Reductech.Utilities.SCLEditor.Components;
using Reductech.Utilities.SCLEditor.React;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<PlaygroundReact>("#scl-playground-root");

//builder.RootComponents.RegisterForJavaScript<PlaygroundReact>(
//    "scl-playground-react",
//    "sclPlaygroundInit"
//);

builder.Services.AddBlazorDownloadFile();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<CompoundFileSystem>();

builder.Services.AddMudBlazorJsApi().AddMudServices();

builder.Services.AddHttpClient();

await builder.Build().RunAsync();
