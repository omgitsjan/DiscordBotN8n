using DSharpPlus;
using DSharpPlus.Entities;

namespace DiscordBot.Wrapper
{
    /// <summary>
    /// Abstraction for Discord interaction context, allowing production and test use.
    /// </summary>
    public interface IInteractionContextWrapper
    {
        DiscordChannel Channel { get; }
        DiscordUser User { get; }

        /// <summary>
        /// Only for test scenarios: set mock channel and user.
        /// </summary>
        void SetUpForTesting(DiscordChannel? discordChannel, DiscordUser? discordUser);

        Task CreateResponseAsync(InteractionResponseType type, DiscordInteractionResponseBuilder? builder = null);
        Task EditResponseAsync(DiscordWebhookBuilder builder);
        Task DeleteResponseAsync();
        Task<DiscordMessage> SendMessageAsync(string content, DiscordEmbed? embed = null);
        Task<DiscordMessage> SendMessageAsync(DiscordEmbed embed);
    }
}
