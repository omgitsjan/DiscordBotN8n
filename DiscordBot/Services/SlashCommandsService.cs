using System.Net.NetworkInformation;
using DiscordBot.Interfaces;
using DiscordBot.Wrapper;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Services
{
    /// <summary>
    /// Service that implements key slash command logic for the Discord bot.
    /// Handles ping and AI-powered chat commands with n8n workflow support.
    /// </summary>
    public class SlashCommandsService(IIn8nService n8nService) : ISlashCommandsService
    {
        private readonly IIn8nService _n8nService = n8nService;

        /// <summary>
        /// Ping command: checks bot latency and responsiveness.
        /// </summary>
        public async Task PingSlashCommandAsync(IInteractionContextWrapper ctx)
        {
            // Indicate to Discord that the bot is processing
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Pinging..."));

            // Measure network latency to Google's public DNS
            long latencyMs;
            try
            {
                using var ping = new Ping();
                var pingReply = ping.Send("google.com");
                latencyMs = pingReply?.RoundtripTime ?? -1;
            }
            catch
            {
                latencyMs = -1;
            }

            // Build the reply embed
            var embed = new DiscordEmbedBuilder
            {
                Title = latencyMs >= 0 ? "🏓 Pong!" : "❓ Pong?",
                Description = latencyMs >= 0
                    ? $"Latency is: {latencyMs} ms"
                    : "Failed to measure latency. (Network error?)",
                Url = "https://github.com/omgitsjan/DiscordBotAI",
                Timestamp = DateTimeOffset.UtcNow,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "omgitsjan/DiscordBot",
                    IconUrl = "https://avatars.githubusercontent.com/u/42674570?v=4"
                }
            };

            // Respond with the embed and delete the thinking state
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

            // Log execution
            Program.Log(
                $"Command 'PingSlashCommandAsync' executed by {ctx.User.Username} ({ctx.User.Id}).");
        }

        /// <summary>
        /// AI Agent command: sends user prompt and session ID to n8n agent, renders result.
        /// </summary>
        public async Task AiAgentSlashCommandAsync(IInteractionContextWrapper ctx, string prompt)
        {
            string userId = ctx.User.Id.ToString();

            // Defer the response
            await ctx.CreateResponseAsync(
                InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Thinking..."));

            // Call the AI/n8n backend
            (bool success, string? message) = await _n8nService.AiAgentAsync(prompt, userId);

            // Build the embed for the AI reply
            var embed = new DiscordEmbedBuilder
            {
                Title = "AI Agent Response",
                Description = message,
                Timestamp = DateTime.UtcNow,
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = ctx.User.Username,
                    IconUrl = ctx.User.AvatarUrl
                },
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Powered by n8n & OpenAI",
                    IconUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/ef/ChatGPT-Logo.svg/768px-ChatGPT-Logo.svg.png"
                }
            };

            // Indicate what the user sent
            embed.AddField("💬 Command Used", $"`/aiagent {prompt}`", false);

            if (!success)
            {
                Program.Log($"AiAgentSlashCommandAsync: {message}", LogLevel.Error);
            }

            // Edit the deferred response with the embed
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

            // Log the command execution
            Program.Log(
                $"Command 'AiAgentSlashCommandAsync' executed by {ctx.User.Username} ({ctx.User.Id}). Input: {prompt}");
        }
    }
}
