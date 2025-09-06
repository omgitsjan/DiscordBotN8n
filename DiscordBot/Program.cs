using DiscordBot.Interfaces;
using DiscordBot.Services;
using DiscordBot.Wrapper;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using RestSharp;
using System.Diagnostics;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Timer = System.Timers.Timer;

namespace DiscordBot
{
    /// <summary>
    /// Main entry point for the Discord AI Bot powered by n8n workflows.
    /// This bot provides a simple slash command interface to interact with powerful AI workflows.
    /// </summary>
    public class Program
    {
        private string? _discordToken;

        /// <summary>
        /// The Discord client instance for bot communication
        /// </summary>
        public DiscordClient? Client { get; private set; }

        /// <summary>
        /// Global logger instance for application-wide logging
        /// </summary>
        public static ILogger? Logger { get; private set; }

        /// <summary>
        /// Application entry point
        /// </summary>
        public static Task Main() => new Program().MainAsync();

        /// <summary>
        /// Main application logic - initializes services, configures Discord client, and starts the bot
        /// </summary>
        public async Task MainAsync()
        {
            try
            {
                // Configure logging
                LogManager.Setup().LoadConfigurationFromFile("nlog.config");

                // Load application configuration
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables() // Allow environment variable overrides
                    .Build();

                // Configure dependency injection
                await using ServiceProvider services = ConfigureServices(configuration);

                // Initialize core services
                Logger = services.GetRequiredService<ILogger<Program>>();

                Log("🤖 Discord AI Bot Starting...", LogLevel.Information);
                Log("⚙️  Powered by n8n workflows", LogLevel.Information);

                // Validate Discord token
                _discordToken = configuration["DiscordBot:Token"];
                if (string.IsNullOrWhiteSpace(_discordToken))
                {
                    Log("❌ Discord token not found! Please set 'DiscordBot:Token' in configuration.", LogLevel.Critical);
                    Environment.Exit(1);
                    return;
                }

                Log($"✅ Discord token loaded (ending with: ...{_discordToken[^4..]})", LogLevel.Debug);

                // Configure Discord client
                var discordConfig = new DiscordConfiguration
                {
                    Token = _discordToken,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMessages,
                    LoggerFactory = services.GetService<ILoggerFactory>(),
                    AutoReconnect = true
                };

                Client = new DiscordClient(discordConfig);

                // Configure slash commands
                var slashCommands = Client.UseSlashCommands(new SlashCommandsConfiguration
                {
                    Services = services
                });
                slashCommands.RegisterCommands<SlashCommands>();

                // Configure interactivity (for future features)
                Client.UseInteractivity(new InteractivityConfiguration
                {
                    Timeout = TimeSpan.FromMinutes(2)
                });

                // Set up event handlers
                Client.Ready += OnClientReady;
                Client.GuildAvailable += OnGuildAvailable;

                Log("🚀 Starting Discord connection...", LogLevel.Information);

                // Connect to Discord
                await Client.ConnectAsync();

                // Start status rotation
                StartStatusRotation();

                Log("✅ Bot is now online and ready!", LogLevel.Information);
                Log("💡 Use /aiagents to interact with AI workflows", LogLevel.Information);

                // Keep the application running
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Log($"❌ Fatal error during startup: {ex.Message}", LogLevel.Critical);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Event handler for when the client is ready
        /// </summary>
        private Task OnClientReady(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            Log($"🎯 Bot is ready! Logged in as {sender.CurrentUser.Username}#{sender.CurrentUser.Discriminator}", LogLevel.Information);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Event handler for when a guild becomes available
        /// </summary>
        private Task OnGuildAvailable(DiscordClient sender, DSharpPlus.EventArgs.GuildCreateEventArgs args)
        {
            Log($"📡 Connected to guild: {args.Guild.Name} ({args.Guild.MemberCount} members)", LogLevel.Debug);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Starts the rotating status display showing various bot statistics
        /// </summary>
        private void StartStatusRotation()
        {
            int statusIndex = 0;
            var timer = new Timer(20000); // 20 seconds per status

            timer.Elapsed += async (_, _) =>
            {
                try
                {
                    if (Client?.CurrentUser == null) return;

                    var activity = statusIndex switch
                    {
                        0 => new DiscordActivity("🤖 AI-powered by n8n", ActivityType.Playing),
                        1 => new DiscordActivity($"📅 {DateTime.UtcNow:dd.MM.yyyy HH:mm} UTC", ActivityType.Watching),
                        2 => new DiscordActivity($"⏱️ {GetUptimeString()}", ActivityType.Watching),
                        3 => new DiscordActivity($"👥 {GetTotalMemberCount()} users", ActivityType.Watching),
                        _ => new DiscordActivity("💬 /aiagent for AI help", ActivityType.ListeningTo)
                    };

                    await Client.UpdateStatusAsync(activity);
                    statusIndex = (statusIndex + 1) % 5;
                }
                catch (Exception ex)
                {
                    Log($"Error updating status: {ex.Message}", LogLevel.Warning);
                }
            };

            timer.AutoReset = true;
            timer.Start();
        }

        /// <summary>
        /// Gets the formatted uptime string
        /// </summary>
        private static string GetUptimeString()
        {
            var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        }

        /// <summary>
        /// Gets the total member count across all guilds
        /// </summary>
        private int GetTotalMemberCount()
        {
            return Client?.Guilds?.Sum(g => g.Value.MemberCount) ?? 0;
        }

        /// <summary>
        /// Centralized logging method with enhanced formatting
        /// </summary>
        internal static void Log(string? message, LogLevel logLevel = LogLevel.Information)
        {
            if (Logger != null)
            {
                Logger.Log(logLevel, "{Message}", message);
            }
            else
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var level = logLevel.ToString().ToUpper().PadRight(11);
                Console.WriteLine($"[{timestamp}] [{level}] {message}");
            }
        }

        /// <summary>
        /// Configures dependency injection container with all required services
        /// </summary>
        private static ServiceProvider ConfigureServices(IConfiguration configuration)
        {
            return new ServiceCollection()
                .AddSingleton(configuration)
                .AddSingleton<IHttpService, HttpService>()
                .AddSingleton<IIn8nService, N8nService>()
                .AddTransient<IInteractionContextWrapper, InteractionContextWrapper>()
                .AddSingleton<ISlashCommandsService, SlashCommandsService>()
                .AddSingleton<SlashCommands>()
                .AddSingleton<IRestClient>(_ => new RestClient())
                .AddLogging(builder => builder.AddNLog())
                .BuildServiceProvider();
        }

        /// <summary>
        /// Determines if the application is running in debug mode
        /// </summary>
        private static bool IsDebug()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
