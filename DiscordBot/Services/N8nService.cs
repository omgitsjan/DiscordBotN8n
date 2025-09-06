using DiscordBot.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace DiscordBot.Services
{
    /// <summary>
    /// Service for communication with the n8n AI Agent Workflow.
    /// Handles authentication, HTTP requests, and safely parses responses.
    /// </summary>
    public class N8nService(IHttpService httpService, IConfiguration configuration) : IIn8nService
    {
        private readonly IHttpService _httpService = httpService ?? throw new ArgumentNullException(nameof(httpService));
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        /// <summary>
        /// Sends a message to the n8n-powered AI agent and returns the reply.
        /// Supports both raw text and JSON-based responses from the n8n workflow.
        /// </summary>
        /// <param name="prompt">User's input message</param>
        /// <param name="userId">Discord user ID – used for session continuity</param>
        /// <returns>(success, response or error message)</returns>
        public async Task<Tuple<bool, string>> AiAgentAsync(string prompt, string userId)
        {
            bool success = false;
            string? responseText;

            // Load and validate configuration
            string agentUrl = _configuration["n8n:AiAgentWorkflowUrl"] ?? "https://n8n.janpetry.de/webhook-test/discordbot";
            string apiKey = _configuration["n8n:ApiKey"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(agentUrl) || string.IsNullOrWhiteSpace(apiKey))
            {
                const string errorMessage = "No n8n URL or API key provided. Please contact the developer to add valid configuration!";
                Program.Log($"{nameof(AiAgentAsync)}: {errorMessage}", LogLevel.Error);
                return Tuple.Create(success, errorMessage);
            }

            // Prepare headers and request body
            var headers = new List<KeyValuePair<string, string>>
            {
                new("Content-Type", "application/json"),
                new("ApiKey", apiKey)
            };
            var requestBody = new
            {
                prompt,
                userId
            };

            // Send request to n8n
            HttpResponse response = await _httpService.GetResponseFromUrl(
                agentUrl,
                Method.Post,
                $"{nameof(AiAgentAsync)}: Unknown HTTP error occurred",
                headers,
                requestBody
            );

            if (response is { IsSuccessStatusCode: true, Content: not null })
            {
                string content = response.Content.Trim();

                // Try to parse as JSON, fallback to plain text
                if ((content.StartsWith('{') && content.EndsWith('}')) ||
                    (content.StartsWith('[') && content.EndsWith(']')))
                {
                    try
                    {
                        var json = JsonConvert.DeserializeObject<dynamic>(content);
                        responseText = json?["result"]?.ToString()
                                     ?? json?["content"]?.ToString()
                                     ?? json?["message"]?.ToString()
                                     ?? content;
                    }
                    catch (Exception ex)
                    {
                        responseText = content;
                        Program.Log($"{nameof(AiAgentAsync)}: JSON parse error, using raw content. Exception: {ex.Message}", LogLevel.Warning);
                    }
                }
                else
                {
                    responseText = content;
                }

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    responseText = "Could not deserialize response from n8n AI Workflow!";
                    Program.Log($"{nameof(AiAgentAsync)}: {responseText}", LogLevel.Error);
                    return Tuple.Create(success, responseText.TrimStart('\n'));
                }

                success = true;
            }
            else
            {
                responseText = response.Content ?? "Empty response from API.";
            }

            return Tuple.Create(success, responseText.TrimStart('\n'));
        }
    }
}
