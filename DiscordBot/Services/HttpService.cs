using DiscordBot.Interfaces;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace DiscordBot.Services
{
    /// <summary>
    /// Provides HTTP operations for API communication, error handling, and response shaping.
    /// </summary>
    public class HttpService(IRestClient httpClient) : IHttpService
    {
        private readonly IRestClient _httpClient = httpClient;

        /// <summary>
        /// Sends a HTTP request to the specified endpoint and returns a processed response wrapper.
        /// Supports headers, body, and async operation. Logs errors on failure.
        /// </summary>
        /// <param name="resource">Endpoint URL or resource</param>
        /// <param name="method">HTTP method (GET, POST, etc.)</param>
        /// <param name="errorMessage">Custom error message to log or include with failed requests</param>
        /// <param name="headers">Headers to include in the request</param>
        /// <param name="jsonBody">Optional object to serialize as JSON body</param>
        /// <returns>A wrapped HTTP response indicating request success and content</returns>
        public async Task<HttpResponse> GetResponseFromUrl(
            string resource,
            Method method = Method.Get,
            string? errorMessage = null,
            List<KeyValuePair<string, string>>? headers = null,
            object? jsonBody = null)
        {
            var request = new RestRequest(resource, method);

            // Add headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.AddHeader(header.Key, header.Value);
                }
            }

            // Add JSON body for POST/PUT etc.
            if (jsonBody != null)
            {
                request.AddJsonBody(jsonBody);
            }

            RestResponse response;
            try
            {
                response = await _httpClient.ExecuteAsync(request);
            }
            catch (Exception ex)
            {
                var error = $"[{nameof(GetResponseFromUrl)}] HTTP error: {ex.Message}";
                Program.Log(error, LogLevel.Error);
                // Provide a standardized error response
                return new HttpResponse(false, errorMessage ?? error);
            }

            string? content = response.Content;

            if (response.IsSuccessStatusCode)
            {
                return new HttpResponse(true, content);
            }

            // Build detailed error content for logging and return
            var statusLine = $"StatusCode: {(int)response.StatusCode} ({response.StatusCode})";
            var failMessage = errorMessage ?? response.ErrorMessage ?? "Unknown HTTP error";
            var fullError = $"{statusLine} | {failMessage}";
            Program.Log(fullError, LogLevel.Error);

            return new HttpResponse(false, fullError);
        }
    }

    /// <summary>
    /// Simple wrapper for unified HTTP responses throughout the bot
    /// </summary>
    public class HttpResponse(bool isSuccessStatusCode, string? content)
    {
        public bool IsSuccessStatusCode { get; set; } = isSuccessStatusCode;
        public string? Content { get; set; } = content;
    }
}
