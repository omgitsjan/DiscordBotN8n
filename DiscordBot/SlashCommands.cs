using DiscordBot.Interfaces;
using DiscordBot.Wrapper;
using DSharpPlus.SlashCommands;

namespace DiscordBot
{
    /// <summary>
    /// Discord slash commands for the AI-powered bot.
    /// Provides a simple interface to interact with powerful n8n AI workflows.
    /// </summary>
    public class SlashCommands(ISlashCommandsService slashCommandsService) : ApplicationCommandModule
    {
        private readonly ISlashCommandsService _slashCommandsService = slashCommandsService;

        /// <summary>
        /// Basic ping command to check bot responsiveness and latency.
        /// </summary>
        [SlashCommand("ping", "Check if the bot is online and measure response latency.")]
        public async Task PingSlashCommand(InteractionContext ctx)
        {
            var context = new InteractionContextWrapper(ctx);
            await _slashCommandsService.PingSlashCommandAsync(context);
        }

        /// <summary>
        /// Main AI Agent command – sends user input to n8n AI workflows and returns an intelligent response.
        /// Ask 'What tools do you have?' to see special agent functions beyond GPT.
        /// </summary>
        [SlashCommand(
            "aiagent",
            "Ask anything! Example: 'What tools do you have?' to see agent extensions beyond standard GPT.")]
        public async Task AiAgentSlashCommand(
            InteractionContext ctx,
            [Option("message", "Your message or question for the AI agent.")]
            string message)
        {
            var context = new InteractionContextWrapper(ctx);
            await _slashCommandsService.AiAgentSlashCommandAsync(context, message);
        }
    }
}
