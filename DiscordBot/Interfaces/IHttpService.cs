using RestSharp;
using DiscordBot.Services;

namespace DiscordBot.Interfaces
{
    /// <summary>
    /// Interface for sending HTTP requests and receiving wrapped responses.
    /// </summary>
    public interface IHttpService
    {
        /// <summary>
        /// Sends an HTTP request to the provided endpoint and returns a wrapped response.
        /// </summary>
        /// <param name="resource">The URL or endpoint to call.</param>
        /// <param name="method">HTTP method (GET/POST/PUT/etc.).</param>
        /// <param name="errorMessage">Custom error message if the request fails.</param>
        /// <param name="headers">Optional list of HTTP headers.</param>
        /// <param name="jsonBody">Optional request body (object serialized as JSON).</param>
        /// <returns>A wrapped response indicating request success and response content.</returns>
        Task<HttpResponse> GetResponseFromUrl(
            string resource,
            Method method = Method.Get,
            string? errorMessage = null,
            List<KeyValuePair<string, string>>? headers = null,
            object? jsonBody = null);
    }
}
