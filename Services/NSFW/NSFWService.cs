#region

using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using Radon.Core;
using Radon.Services.External;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

#endregion

namespace Radon.Services.Nsfw
{
    public class NSFWService
    {
        private readonly HttpClient _httpClient;
        private readonly Random _random;
        private readonly Configuration _configuration;

        public NSFWService(HttpClient httpClient, Random random, Configuration configuration)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _random = random;
        }

        public async Task SendImageFromCategory(ShardedCommandContext context, string category, Server server)
        {

            var link =
                $"{JObject.Parse(await _httpClient.GetStringAsync($"https://nekobot.xyz/api/image?type={category}"))["message"]}";

            var embed = new EmbedBuilder()
                .WithImageUrl(link);
            embed.NormalizeEmbed(ColorType.Normal, _random, server, true, context);

            await context.Channel.SendMessageAsync(embed: embed.Build());
        }
        public async Task GetE621Async(ShardedCommandContext context, int count, string tags)
        {
            try
            {
                tags = tags.Replace(" ", "+");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", _configuration.E621UserAgent);

                JArray data = JArray.Parse(await _httpClient.GetStringAsync($"https://e621.net/post/index.json?tags={tags}&limit={count}&page={_random.Next(1, 100)}"));
                int stuff = data.Count - 1;
                foreach (var item in data.Children())
                {
                    string rating = data[stuff]["rating"].ToString();
                    string emote = ":question:";
                    switch (rating)
                    {
                        case "s":
                            emote = ":green_heart:";
                            break;
                        case "q":
                            emote = ":yellow_heart:";
                            break;
                        case "e":
                            emote = ":purple_heart:";
                            break;
                        default:
                            break;
                    }
                    if (data[stuff]["tags"].ToString().ToLower().Contains("animated") || data[stuff]["tags"].ToString().ToLower().Contains("animation") && data[stuff]["file_ext"].ToString() != "gif")
                    {

                        await context.Channel.SendMessageAsync(
                            $"{data[stuff]["author"]}" +
                            $" | {data[stuff]["tags"].ToString().WithMaxLength(30)} | {data[stuff]["source"]}" +
                            $" | {data[stuff]["file_url"]}" +
                            $" | <:upvote:486361126658113536> {data[stuff]["score"]}" +
                            $" | :heart: {data[stuff]["fav_count"]}" +
                            $" | {emote}");

                    }
                    else
                    {

                        var embed = new EmbedBuilder()
                        .WithTitle($"{data[stuff]["author"]} | {data[stuff]["tags"].ToString().WithMaxLength(30)}")
                        .WithUrl($"{data[stuff]["source"]}")
                        .WithImageUrl($"{data[stuff]["file_url"]}")
                        .AddField("Statistics",$"<:upvote:486361126658113536> {data[stuff]["score"]} | :heart: {data[stuff]["fav_count"]} | {emote}");
                        await context.Channel.SendMessageAsync(embed: embed.Build());
                    }
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                    stuff--;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}