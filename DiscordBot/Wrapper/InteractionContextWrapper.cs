using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace DiscordBot.Wrapper
{
    /// <summary>
    /// Wrapper around DSharpPlus's context, enabling easier testability and abstraction.
    /// </summary>
    public class InteractionContextWrapper(BaseContext context) : IInteractionContextWrapper
    {
        private readonly BaseContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private DiscordChannel? _testChannel;
        private DiscordUser? _testUser;

        // For testing/mocking, allows overriding of channel and user
        public void SetUpForTesting(DiscordChannel? discordChannel, DiscordUser? discordUser)
        {
            _testChannel = discordChannel;
            _testUser = discordUser;
        }

        public DiscordChannel Channel => _testChannel ?? _context.Channel;
        public DiscordUser User => _testUser ?? _context.User;

        public Task CreateResponseAsync(InteractionResponseType type, DiscordInteractionResponseBuilder? builder = null)
        {
            if (_context is InteractionContext interactionContext)
                return interactionContext.CreateResponseAsync(type, builder);
            throw new InvalidOperationException("Context is not an InteractionContext.");
        }

        public Task EditResponseAsync(DiscordWebhookBuilder builder)
        {
            if (_context is InteractionContext interactionContext)
                return interactionContext.EditResponseAsync(builder);
            throw new InvalidOperationException("Context is not an InteractionContext.");
        }

        public Task DeleteResponseAsync()
        {
            if (_context is InteractionContext interactionContext)
                return interactionContext.DeleteResponseAsync();
            throw new InvalidOperationException("Context is not an InteractionContext.");
        }

        public Task<DiscordMessage> SendMessageAsync(string content, DiscordEmbed? embed = null)
        {
            return Channel.SendMessageAsync(content, embed);
        }

        public Task<DiscordMessage> SendMessageAsync(DiscordEmbed embed)
        {
            return Channel.SendMessageAsync(embed);
        }
    }
}
