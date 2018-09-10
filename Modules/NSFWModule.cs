#region

using Discord;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using Radon.Core;
using Radon.Services.Nsfw;
using System;
using System.Net.Http;
using System.Threading.Tasks;

#endregion

namespace Radon.Modules
{
    [CheckNsfw]
    [CommandCategory(CommandCategory.Nsfw)]
    [CheckState]
    public class NsfwModule : CommandBase
    {
        private readonly HttpClient _httpClient;
        private readonly NSFWService _nsfwService;
        private readonly Random _random;

        public NsfwModule(HttpClient httpClient, Random random, NSFWService nsfwService)
        {
            _httpClient = httpClient;
            _random = random;
            _nsfwService = nsfwService;
        }

        [Command("ass")]
        [Summary("Sends some nice ass :)")]
        public async Task AssAsync()
        {
            var random = _random.Next(6012);
            var data = JArray.Parse(await _httpClient.GetStringAsync($"http://api.obutts.ru/butts/{random}")).First;
            var embed = new EmbedBuilder()
                .WithImageUrl($"http://media.obutts.ru/{data["preview"]}");

            await ReplyEmbedAsync(embed, ColorType.Normal, true);
        }

        [Command("boobs")]
        [Summary("Sends some nice boobs ;)")]
        public async Task BoobsAsync()
        {
            var random = _random.Next(12965);
            var data = JArray.Parse(await _httpClient.GetStringAsync($"http://api.oboobs.ru/boobs/{random}")).First;
            var embed = new EmbedBuilder()
                .WithImageUrl($"http://media.oboobs.ru/{data["preview"]}");

            await ReplyEmbedAsync(embed, ColorType.Normal, true);
        }

        [Command("hentai")]
        [Summary("Sends a random hentai picture :D")]
        public async Task HentaiAsync()
        {
            await _nsfwService.SendImageFromCategory(Context, "hentai", Server);
        }

        [Command("nude")]
        [Summary("Sends some nice nudes :P")]
        public async Task NudeAsync()
        {
            await _nsfwService.SendImageFromCategory(Context, "4k", Server);
        }

        [Command("nudegif")]
        [Summary("Sends a nice nude gif d:")]
        public async Task NudeGifAsync()
        {
            await _nsfwService.SendImageFromCategory(Context, "pgif", Server);
        }

        [Command("anal")]
        [Summary("Sends a carrot in a melon imao")]
        public async Task AnalAsync()
        {
            await _nsfwService.SendImageFromCategory(Context, "anal", Server);
        }

        [Command("pussy")]
        [Summary("Sends a melon with a hole")]
        public async Task PussyAsync()
        {
            await _nsfwService.SendImageFromCategory(Context, "pussy", Server);
        }
        [Group("E621")]
        [Summary("Gets an image from E621")]
        public class E621 : CommandBase
        {
            private readonly NSFWService _nsfwService;

            public E621(NSFWService nsfwService)
            {
                _nsfwService = nsfwService;
            }

            [Command("E621")]
            [Alias("e6")]
            public async Task E621Async(int count, [Remainder] string tags)
            {
                if (count > 10)
                    count = 10;
                await _nsfwService.GetE621Async(Context, count, tags);
            }
            [Command("E621")]
            [Alias("e6")]
            public async Task E621Async([Remainder] string tags)
            {
                await _nsfwService.GetE621Async(Context, 1, tags);
            }
        }

    }
}