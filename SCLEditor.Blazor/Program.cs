using BlazorDownloadFile;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Sequence.Utilities.SCLEditor.Components;

namespace Sequence.Utilities.SCLEditor.Blazor;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        // builder.Services.AddScoped(
        //     sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
        // );

        builder.Services.AddBlazorDownloadFile();
        builder.Services.AddBlazoredLocalStorage();
        builder.Services.AddScoped<CompoundFileSystem>();

        builder.Services.AddMudBlazorJsApi().AddMudServices();

        builder.Services.AddHttpClient();

        await builder.Build().RunAsync();
    }
}
