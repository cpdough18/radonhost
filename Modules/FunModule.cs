using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.Commands;
using GiphyDotNet.Manager;
using GiphyDotNet.Model.Parameters;
using Newtonsoft.Json.Linq;
using Radon.Core;
using Radon.Services;

namespace Radon.Modules
{
    [CommandCategory(CommandCategory.Fun)]
    [CheckState]
    public class FunModule : CommandBase
    {
        private readonly Configuration _configuration;
        private readonly Giphy _giphy;
        private readonly HttpClient _http;
        private readonly Random _random;

        public FunModule(HttpClient http, Random random, Configuration configuration, Giphy giphy)
        {
            _http = http;
            _random = random;
            _configuration = configuration;
            _giphy = giphy;
        }

        [Command("dog")]
        [Alias("woof")]
        [Summary("Returns a random dog image")]
        public async Task DogAsync()
        {
            string url;
            do
            {
                url = $"http://random.dog/{await _http.GetStringAsync("https://random.dog/woof")}";
            } while (url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                     url.EndsWith(".webm", StringComparison.OrdinalIgnoreCase));

            var embed = new EmbedBuilder()
                .WithImageUrl(url);
            await ReplyEmbedAsync(embed);
        }

        [Command("fox")]
        [Summary("Returns a random fox image")]
        public async Task FoxAsync()
        {
            var url = $"{JObject.Parse(await _http.GetStringAsync("https://randomfox.ca/floof/"))["image"]}";

            var embed = new EmbedBuilder()
                .WithImageUrl(url);

            await ReplyEmbedAsync(embed);
        }

        [Command("8ball")]
        [Summary("8Ball will answer your question!")]
        public async Task EightballAsync([Remainder] string question)
        {
            var data = JObject.Parse(await _http.GetStringAsync("https://nekos.life/api/v2/8ball"));

            var embed = new EmbedBuilder()
                .WithTitle("8Ball has spoken")
                .WithDescription($"Question ❯ {question}\n\n8Ball's answer ❯ {data["response"]}")
                .WithThumbnailUrl($"{data["url"]}");

            await ReplyEmbedAsync(embed);
        }

        [Command("joke")]
        [Summary("Tells a random joke :p")]
        public async Task JokeAsync()
        {
            var joke = $"{JObject.Parse(await _http.GetStringAsync("http://api.yomomma.info/"))["joke"]}";
            await ReplyAsync(joke);
        }

        [Command("lenny")]
        [Summary("Returns a lenny ( ͡° ͜ʖ ͡°)")]
        public async Task LennyAsync()
        {
            var lennys = new[]
            {
                "( ͡° ͜ʖ ͡°)", "(☭ ͜ʖ ☭)", "(ᴗ ͜ʖ ᴗ)", "( ° ͜ʖ °)", "( ͡◉ ͜ʖ ͡◉)", "( ͡☉ ͜ʖ ͡☉)", "( ͡° ͜ʖ ͡°)>⌐■-■",
                "<:::::[]=¤ (▀̿̿Ĺ̯̿̿▀̿ ̿)", "( ͡ಥ ͜ʖ ͡ಥ)", "( ͡º ͜ʖ ͡º )", "( ͡ಠ ʖ̯ ͡ಠ)", "ᕦ( ͡°╭͜ʖ╮͡° )ᕤ", "( ♥ ͜ʖ ♥)",
                "(つ ♡ ͜ʖ ♡)つ", "✩°｡⋆⸜(▀̿Ĺ̯▀̿ ̿)", "⤜(ʘ_ʘ)⤏", "¯\\_ツ_/¯", "ಠ_ಠ", "ʢ◉ᴥ◉ʡ", "^‿^", "(づ◔ ͜ʖ◔)づ", "⤜(ʘ_ʘ)⤏",
                "☞   ͜ʖ  ☞", "ᗒ ͟ʖᗕ", "/͠-. ͝-\\", "(´• ᴥ •`)", "(╯￢ ᗝ￢ ）╯︵ ┻━┻", "ᕦ(・ᨎ・)ᕥ", "◕ ε ◕", "【$ ³$】",
                "(╭☞T ε T)╭☞"
            };
            await ReplyAsync(lennys[_random.Next(lennys.Length)]);
        }

        [Command("meme")]
        [Summary("Returns a meme")]
        public async Task MemeAsync()
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", $"{_configuration.KsoftApiKey}");
            JObject data;
            do
            {
                data = JObject.Parse(await _http.GetStringAsync("https://api.ksoft.si/meme/random-meme"));
            } while (data.Value<bool>("nsfw"));

            var embed = new EmbedBuilder()
                .WithTitle($"{data["title"]}")
                .WithUrl($"{data["source"]}")
                .WithImageUrl($"{data["image_url"]}");

            await ReplyEmbedAsync(embed);

            _http.DefaultRequestHeaders.Authorization = null;
        }

        [Command("wikihow")]
        [Alias("wh")]
        [Summary("Returns random wikihow image")]
        public async Task WikiHowAsync()
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", $"{_configuration.KsoftApiKey}");
            JObject data;

            data = JObject.Parse(await _http.GetStringAsync("https://api.ksoft.si/meme/random-wikihow"));

            var embed = new EmbedBuilder()
                .WithTitle($"{data["title"]}")
                .WithUrl($"{data["article_url"]}")
                .WithImageUrl($"{data["url"]}");

            await ReplyEmbedAsync(embed);

            _http.DefaultRequestHeaders.Authorization = null;
        }

        [Command("out")]
        [Alias("door")]
        [Summary("Shows someone the door")]
        public async Task OutAsync(IUser user)
        {
            await ReplyEmbedAsync(null, $"{user.Mention}  :point_right::skin-tone-1:  :door:");
        }

        [Command("say")]
        [Alias("echo","e","s")]
        [Summary("Echoes text")]
        public async Task SayAsync([Remainder] string text)
        {
            await ReplyEmbedAsync(null, text);
        }

        [Command("lmgtfy")]
        [Alias("showgoogle", "sg", "showg", "sgoogle")]
        [Summary("Shows a dumbass how to google")]
        public async Task ShowGoogleAsync([Remainder] string query)
        {
            var url = $"http://lmgtfy.com/?q={HttpUtility.UrlEncode(query)}";
            await ReplyAsync(url);
        }

        [Command("aww")]
        [Alias("awh","cute")]
        [Summary("Returns random image from r/aww")]
        public async Task AwwAsync()
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", $"{_configuration.KsoftApiKey}");
            JObject data;

            data = JObject.Parse(await _http.GetStringAsync("https://api.ksoft.si/meme/random-aww"));

            var embed = new EmbedBuilder()
                .WithTitle($"{data["subreddit"]} | {data["title"]}")
                .WithUrl($"{data["source"]}")
                .WithImageUrl($"{data["image_url"]}")
                .WithFooter($"<:upvote:486361126658113536> {data["upvotes"]} | <:downvote:486361126641205268> {data["downvotes"]}");

            await ReplyEmbedAsync(embed);

            _http.DefaultRequestHeaders.Authorization = null;
        }

        [Command("subreddit")]
        [Alias("reddit", "sub","r","r/","sr","sreddit")]
        [Summary("Returns random image from specified subreddit")]
        public async Task SubredditAsync(string subreddit)
        {
            try
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Token", $"{_configuration.KsoftApiKey}");
                JObject data;

                data = JObject.Parse(await _http.GetStringAsync($"https://api.ksoft.si/meme/rand-reddit/{subreddit}"));

                var embed = new EmbedBuilder()
                    .WithTitle($"{data["subreddit"]} | {data["title"]}")
                    .WithUrl($"{data["source"]}")
                    .WithImageUrl($"{data["image_url"]}")
                    .WithFooter($"<:upvote:486361126658113536> {data["upvotes"]} | <:downvote:486361126641205268> {data["downvotes"]}");

                await ReplyEmbedAsync(embed);

                _http.DefaultRequestHeaders.Authorization = null;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("rip")]
        [Alias("tombstone")]
        [Summary("Sends a tombstone with a custom text")]
        public async Task RipAsync([Remainder] string text)
        {
            var url = "http://tombstonebuilder.com/generate.php" +
                      "?top1=R.I.P." +
                      $"&top2={HttpUtility.UrlEncode(text.Substring(0, Math.Min(text.Length, 25)))}" +
                      $"{(text.Length > 25 ? $"&top3={HttpUtility.UrlEncode(text.Substring(25, Math.Min(25, text.Length - 25)))}" : "")}" +
                      $"{(text.Length > 50 ? $"&top4={HttpUtility.UrlEncode(text.Substring(50))}" : "")}";
            await ReplyEmbedAsync(new EmbedBuilder().WithImageUrl(url));
        }

        [Command("sign")]
        [Alias("roadsign")]
        [Summary("Sends a roadsign with a custom text")]
        public async Task SignAsync([Remainder] string text)
        {
            var url = $"http://www.customroadsign.com/generate.php" +
                      $"?line1={HttpUtility.UrlEncode(text.Substring(0, Math.Min(15, text.Length)))}" +
                      $"{(text.Length > 15 ? $"&line2={HttpUtility.UrlEncode(text.Substring(15, Math.Min(15, text.Length - 15)))}" : "")}" +
                      $"{(text.Length > 30 ? $"&line3={HttpUtility.UrlEncode(text.Substring(30, Math.Min(30, text.Length - 30)))}" : "")}" +
                      $"{(text.Length > 45 ? $"&line4={HttpUtility.UrlEncode(text.Substring(45, Math.Min(45, text.Length - 45)))}" : "")}";
            await ReplyEmbedAsync(new EmbedBuilder().WithImageUrl(url));
        }

        [Command("qr")]
        [Alias("qrcode")]
        [Summary("Creates a qr code")]
        public async Task QrAsync([Remainder] string text)
        {
            await ReplyEmbedAsync(new EmbedBuilder().WithImageUrl(
                $"https://chart.googleapis.com/chart?cht=qr&chl={HttpUtility.UrlEncode(text)}&choe=UTF-8&chld=L&chs=500x500"));
        }

        [Command("ascii")]
        [Summary("Converts text to the ascii format")]
        public async Task AsciiAsync([Remainder] string text)
        {
            await ReplyAsync(
                $"{await _http.GetStringAsync($"http://artii.herokuapp.com/make?text={text}")}".BlockCode());
        }

        [Group("gif")]
        [Alias("giphy", "g")]
        [CommandCategory(CommandCategory.Fun)]
        [Summary("Sends a random gif or one with your tag")]
        public class GiphyModule : CommandBase
        {
            private readonly Giphy _giphy;
            private readonly Random _random;

            public GiphyModule(Giphy giphy, Random random)
            {
                _giphy = giphy;
                _random = random;
            }

            [Command("")]
            [Summary("Sends a random gif")]
            [Priority(-1)]
            public async Task GifAsync()
            {
                var gif = await _giphy.RandomGif(new RandomParameter());
                var embed = new EmbedBuilder()
                    .WithImageUrl(gif.Data.ImageUrl);

                await ReplyEmbedAsync(embed);
            }

            [Command("")]
            [Summary("Searches a gif from your query")]
            [Priority(-1)]
            public async Task GifAsync([Remainder] string query)
            {
                var gif = await _giphy.GifSearch(new SearchParameter { Query = query });
                var embed = new EmbedBuilder();
                if (!gif.Data.Any())
                    embed.WithTitle("No gif found")
                        .WithDescription($"Couldn't find any gif for {query.InlineCode()}");
                else
                    embed.WithImageUrl(gif.Data[_random.Next(gif.Data.Length)].Images.Original.Url);

                await ReplyEmbedAsync(embed);
            }
        }
    }
}