using Microsoft.AspNetCore.Components.WebAssembly.Http;
using RestSharp;
using Sequence.Core;

namespace Sequence.Utilities.SCLEditor.Components;

public class BlazorRestClientFactory : IRestClientFactory
{
    public BlazorRestClientFactory(IHttpClientFactory clientFactory)
    {
        ClientFactory = clientFactory;
    }

    public IHttpClientFactory ClientFactory { get; }

    /// <inheritdoc />
    public IRestClient CreateRestClient(string baseUri)
    {
        var restClient = new RestClient(
            ClientFactory.CreateClient(),
            new RestClientOptions(baseUri),
            true
        );

        return new BlazorRestClient(restClient);
    }

    private class BlazorRestClient : IRestClient
    {
        public BlazorRestClient(RestClient restClient)
        {
            RestClient = restClient;
        }

        public RestClient RestClient { get; }

        /// <inheritdoc />
        public async Task<RestResponse> ExecuteAsync(
            RestRequest request,
            CancellationToken cancellationToken)
        {
            DisableCors(request);

            return await RestClient.ExecuteAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Stream?> DownloadStreamAsync(
            RestRequest request,
            CancellationToken cancellationToken)
        {
            DisableCors(request);
            return await RestClient.DownloadStreamAsync(request, cancellationToken);
        }

        private static void DisableCors(RestRequest request)
        {
            request.OnBeforeRequest = delegate(HttpRequestMessage message)
            {
                message.SetBrowserRequestMode(BrowserRequestMode.NoCors);
                //message.Headers.Add("sec-fetch-mode", "no-cors");
                return ValueTask.CompletedTask;
            };
        }
    }
}
