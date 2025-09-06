namespace DiscordBot.Interfaces
{
    /// <summary>
    /// Interface for services that provide AI workflow integrations (e.g., with n8n).
    /// </summary>
    public interface IIn8nService
    {
        /// <summary>
        /// Sends a message and user/session ID to the AI workflow and returns a (success, reply/error) tuple.
        /// </summary>
        Task<Tuple<bool, string>> AiAgentAsync(string prompt, string userId);
    }
}
