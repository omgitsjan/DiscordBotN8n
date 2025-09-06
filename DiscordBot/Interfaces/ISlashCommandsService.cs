using DiscordBot.Wrapper;

namespace DiscordBot.Interfaces
{
    /// <summary>
    /// Interface for core slash command services: handles ping and AI agent routing for the Discord bot.
    /// </summary>
    public interface ISlashCommandsService
    {
        /// <summary>
        /// Executes the ping slash command, sending latency statistics to the user.
        /// </summary>
        Task PingSlashCommandAsync(IInteractionContextWrapper ctx);

        /// <summary>
        /// Executes the AI Agent slash command, sending the prompt to the n8n-powered AI and displaying the intelligent result.
        /// </summary>
        /// <param name="ctx">Command execution context.</param>
        /// <param name="prompt">User's question or request for the AI.</param>
        Task AiAgentSlashCommandAsync(IInteractionContextWrapper ctx, string prompt);
    }
}
