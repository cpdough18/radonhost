using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Radon.Core;

namespace Radon.Services.External
{
    public class StatisticsService
    {
        private readonly DiscordShardedClient _client;
        private readonly Configuration _configuration;

        public StatisticsService(Configuration configuration, DiscordShardedClient client)
        {
            _configuration = configuration;
            _client = client;

            _client.ShardReady += ShardReady;
            _client.JoinedGuild += GuildUpdated;
            _client.LeftGuild += GuildUpdated;
        }

        private async Task GuildUpdated(SocketGuild arg)
        {
            await UpdateStatistics();
        }

        private async Task ShardReady(DiscordSocketClient client)
        {
            await UpdateStatistics();
        }

        private async Task UpdateStatistics()
        {
            foreach (var shard in _client.Shards)
                await shard.SetActivityAsync(new Game(
                    $"you | {_configuration.BotPrefixes.First()}help | Shard {shard.ShardId + 1}/{_client.Shards.Count} | {_client.Guilds.Count} servers",
                    ActivityType.Watching));
        }
    }
}