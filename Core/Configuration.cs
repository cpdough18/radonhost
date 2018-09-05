#region

using Newtonsoft.Json;

#endregion

namespace Radon.Core
{
    public class Configuration
    {
        [JsonProperty("BotDiscordInviteLink")] public readonly string BotDiscordInviteLink = "";
        [JsonProperty("BotPrefixes")] public readonly string[] BotPrefixes = { "$" };

        [JsonProperty("BotToken")] public readonly string BotToken = "";

        [JsonProperty("DatabaseConnectionString")] public readonly string DatabaseConnectionString = "http://127.0.0.1:8080";

        [JsonProperty("DefaultJoinMessage")] public readonly string DefaultJoinMessage = "";

        [JsonProperty("DefaultLeaveMessage")] public readonly string DefaultLeaveMessage = "";

        [JsonProperty("DefaultLevelUpMessage")] public readonly string DefaultLevelUpMessage = "";

        [JsonProperty("GiphyApiKey")] public readonly string GiphyApiKey = "";

        [JsonProperty("GitHubLink")] public readonly string GitHubLink = "";

        [JsonProperty("KsoftApiKey")] public readonly string KsoftApiKey = "";

        [JsonProperty("LiscordApiKey")] public readonly string LiscordApiKey = "";

        [JsonProperty("LolApiKey")] public readonly string LolApiKey = "";

        [JsonProperty("e621UserAgent")] public readonly string E621UserAgent = "";

        [JsonProperty("OwnerIds")] public readonly ulong[] OwnerIds = {  };

        [JsonProperty("ShardCount")] public readonly int ShardCount = 1;
    }
}
