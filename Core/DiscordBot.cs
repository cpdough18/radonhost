#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using GiphyDotNet.Manager;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Radon.Services;
using Radon.Services.External;
using SharpLink;
using Radon.Services.Nsfw;

#endregion

namespace Radon.Core
{
    public class DiscordBot
    {
        private DiscordShardedClient _client;
        private CommandService _commands;
        private Configuration _configuration;
        private DatabaseService _database;
        private HttpClient _httpClient;
        private InteractiveService _interactive;
        private IServiceProvider _services;
        private LavalinkManager _lavalinkManager;

        public async Task InitializeAsync()
        {
            _ = PublicVariables.Colors;
            _configuration = ConfigurationService.LoadNewConfig();
            _database = new DatabaseService(_configuration);
            _httpClient = new HttpClient();
            _client = new DiscordShardedClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                DefaultRetryMode = RetryMode.AlwaysRetry,
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 2048,
                TotalShards = _configuration.ShardCount
            });
            _commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Info,
                DefaultRunMode = RunMode.Sync
            });
            _lavalinkManager = new LavalinkManager(_client, new LavalinkManagerConfig
            {
                RESTHost = _configuration.RESTHost,
                RESTPort = _configuration.RESTPort,
                WebSocketHost = _configuration.WebSocketHost,
                WebSocketPort = _configuration.WebSocketPort,
                Authorization = _configuration.Authorization,
                TotalShards = _configuration.ShardCount,
            });
            _interactive = new InteractiveService(_client);
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton(_configuration)
                .AddSingleton(_database)
                .AddSingleton(_interactive)
                .AddSingleton(_httpClient)
                .AddSingleton(_lavalinkManager)
                .AddSingleton(new Giphy(_configuration.GiphyApiKey))
                .AddSingleton<StatisticsService>()
                .AddSingleton<Random>()
                .AddSingleton<LogService>()
                .AddSingleton<CachingService>()
                .AddSingleton<ServerService>()
                .AddSingleton<NSFWService>()
                .AddSingleton<KsoftBanApiService>()
                .AddSingleton<SharplinkService>()
                .BuildServiceProvider();
            _services.GetService<LogService>();
            _services.GetService<StatisticsService>();
            _client.MessageReceived += MessageReceived;
            _client.ReactionAdded += ReactionAdded;
            _client.Log += Log;
            _commands.Log += Log;
            _client.ShardReady += Ready;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            Console.WriteLine($"{_commands.Commands.Count()} commands | {_commands.Modules.Count()} modules");

            await _client.LoginAsync(TokenType.Bot, _configuration.BotToken);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task Ready(DiscordSocketClient client)
        {
            try { await _lavalinkManager.StartAsync(); }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(_client.CurrentUser.Username));
            PublicVariables.Application = await _client.GetApplicationInfoAsync();
        }

        private static Task Log(LogMessage message)
        {
            if (message.Message.StartsWith("A") || message.Message.StartsWith("Unknown")) return Task.CompletedTask;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(
                $"[{DateTimeOffset.Now:dd.MM.yyyy HH:mm:ss}] [{message.Severity}] [{message.Source}]: ");
            Console.ResetColor();
            Console.WriteLine(message.Message ?? message.Exception.Message);
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage msg)
        {
            try
            {
                if (msg.Author.IsBot || !(msg is SocketUserMessage message)) return;

                int argPos = 0;

                List<string> prefixes = new List<string>(_configuration.BotPrefixes);

                Server server = null;

                ExecutionObject executionObj;

                if (message.Channel is ITextChannel channel)
                {
                    IGuild guild = channel.Guild;
                    _database.Execute(x => { server = x.Load<Server>($"{guild.Id}") ?? new Server(); });

                    switch (server.BlockingType)
                    {
                        case BlockingType.Whitelist when !server.Whitelist.Contains(channel.Id):
                        case BlockingType.Blacklist when server.Blacklist.Contains(channel.Id):
                            return;
                        case BlockingType.None:
                            break;
                    }

                    prefixes.AddRange(server.Prefixes);

                    executionObj = new ExecutionObject { Server = server };
                }
                else
                {
                    executionObj = new ExecutionObject();
                }

                if (message.HasMentionPrefix(_client.CurrentUser, ref argPos) || prefixes.Any(x =>
                        message.HasStringPrefix(x, ref argPos, StringComparison.OrdinalIgnoreCase)))
                {
                    ShardedCommandContext context = new ShardedCommandContext(_client, message);
                    string parameters = message.Content.Substring(argPos).TrimStart('\n', ' ');
                    _services.GetService<CachingService>().ExecutionObjects[message.Id] = executionObj;
                    IResult result = await _commands.ExecuteAsync(context, parameters, _services, MultiMatchHandling.Best);
                    if (!result.IsSuccess)
                    {
                        await HandleErrorAsync(result, context, parameters, server);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task HandleErrorAsync(IResult result, ShardedCommandContext context, string parameters,
            Server server)
        {
            EmbedBuilder embed = new EmbedBuilder().NormalizeEmbed(ColorType.Normal, _services.GetService<Random>(), server);
            switch (result.Error)
            {
                case CommandError.UnknownCommand:
                    break;
                case CommandError.BadArgCount:
                case CommandError.ParseFailed:
                case CommandError.ObjectNotFound:
                    SearchResult searchResult = _commands.Search(context, parameters);
                    if (result.Error == CommandError.BadArgCount)
                    {
                        CommandMatch command = searchResult.Commands.First();
                        PreconditionResult preconditionResult = await command.CheckPreconditionsAsync(context, _services);
                        if (!preconditionResult.IsSuccess && preconditionResult.Error != CommandError.BadArgCount)
                        {
                            embed.WithTitle("Missing Permissions")
                                .WithDescription(preconditionResult.ErrorReason);
                            await context.Channel.SendMessageAsync(embed: embed.Build());
                            break;
                        }
                    }

                    embed.WithTitle(
                            $"{searchResult.Commands.First().Command.Name.Humanize(LetterCasing.Title)} Command Usage")
                        .WithDescription(string.IsNullOrWhiteSpace(searchResult.Commands.First().Command.Module.Group)
                            ? searchResult.Commands.Select(x => x.Command).GetUsage(context).InlineCode()
                            : searchResult.Commands.First().Command.Module.GetUsage(context).InlineCode());
                    await context.Channel.SendMessageAsync(embed: embed.Build());
                    break;
                case CommandError.MultipleMatches:
                    break;
                case CommandError.UnmetPrecondition:
                    embed.WithTitle("Unmet Precondition")
                        .WithDescription(result.ErrorReason);
                    await context.Channel.SendMessageAsync(embed: embed.Build());
                    break;
                case CommandError.Unsuccessful:
                case CommandError.Exception:
                case null:
                    embed.WithTitle("Internal Error")
                        .WithDescription("Error: null");
                    await context.Channel.SendMessageAsync(embed: embed.Build());
                    break;
                default:
                    break;
            }
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2,
            SocketReaction reaction)
        {
            if (!(arg2 is ITextChannel channel))
            {
                return;
            }

            Server server = null;
            if (Equals(reaction.Emote.Name, "#⃣"))
            {
                _database.Execute(x => server = x.Load<Server>($"{channel.Guild.Id}"));
                if (!server.GetSetting(Setting.Hastebin))
                {
                    return;
                }

                IUserMessage message = await arg1.GetOrDownloadAsync();
                if (message.Author.IsBot)
                {
                    return;
                }

                if (Regex.IsMatch(message.Content, PublicVariables.CodeBlockRegex,
                    RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase))
                {
                    await message.Channel.SendMessageAsync(
                        $"{message.Author.Mention} ❯ {message.Content.ToHastebin(_httpClient)}");
                }
            }
        }
    }
}