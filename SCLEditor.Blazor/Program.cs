using System;
using System.Net.Http;
using System.Threading.Tasks;
using BlazorDownloadFile;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Reductech.Utilities.SCLEditor.Blazor
{

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        builder.Services.AddScoped(
            sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
        );

        builder.Services.AddBlazorDownloadFile();

        builder.Services.AddMudBlazorJsApi().AddMudServices();
        await builder.Build().RunAsync();
    }
}

}
