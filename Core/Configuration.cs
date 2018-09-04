#region

using Newtonsoft.Json;

#endregion

namespace Radon.Core
{
    public class Configuration
    {
        [JsonProperty("BotDiscordInviteLink")] public readonly string BotDiscordInviteLink = "https://discordapp.com/oauth2/authorize?client_id=481695914801496065&permissions=8&scope=bot";

        [JsonProperty("BotPrefixes")] public readonly string[] BotPrefixes = { "$" };

        [JsonProperty("BotToken")] public readonly string BotToken = "NDgxNjk1OTE0ODAxNDk2MDY1.DmAIRg.SLjjjG8aNokJaldpsPIf_tAdNKo";

        [JsonProperty("DatabaseConnectionString")] public readonly string DatabaseConnectionString = "http://127.0.0.1:8080";

        [JsonProperty("DefaultJoinMessage")] public readonly string DefaultJoinMessage = "bop";

        [JsonProperty("DefaultLeaveMessage")] public readonly string DefaultLeaveMessage = "pob";

        [JsonProperty("DefaultLevelUpMessage")] public readonly string DefaultLevelUpMessage = "Eat ass";

        [JsonProperty("GiphyApiKey")] public readonly string GiphyApiKey = "YsZ9PGPpZcYu7NVpYpXUCKvgHNbLUFwf";

        [JsonProperty("GitHubLink")] public readonly string GitHubLink = "";

        [JsonProperty("KsoftApiKey")] public readonly string KsoftApiKey = "533847961cdb71e867a0445f768a324971477598";

        [JsonProperty("LiscordApiKey")] public readonly string LiscordApiKey = "";

        [JsonProperty("LolApiKey")] public readonly string LolApiKey = "";

        [JsonProperty("e621UserAgent")] public readonly string E621UserAgent = "fooooooooooooooo";

        [JsonProperty("OwnerIds")] public readonly ulong[] OwnerIds = { 131283250042634241 };

        [JsonProperty("ShardCount")] public readonly int ShardCount = 1;
    }
}