#region
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using GiphyDotNet.Manager;
using GiphyDotNet.Model.Parameters;
using Newtonsoft.Json.Linq;
using Radon.Core;
using Radon.Services;
using Radon.Services.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
#endregion

namespace Radon.Services.External
{
    public class KsoftBanApiService
    {
        private readonly HttpClient _httpClient;

        public KsoftBanApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        // None of this shit works, I'm not done with it.
        public async Task CheckUsersAsync(ShardedCommandContext commandContext)
        {
            await commandContext.Guild.DownloadUsersAsync();

            IReadOnlyCollection<SocketGuildUser> user = commandContext.Guild.Users;
            var fuckingThingIDontKnowWhatToCallIt = Split(user.ToList(), 1000);

            HttpContent content = null;
            content.Headers.ContentType = null;
            _httpClient.DefaultRequestHeaders.Add("type", "list");
            await _httpClient.PostAsync("http://api.ksoft.si/bans/bulkcheck", content);
        }
        public static List<List<T>> Split<T>(List<T> collection, int size)
        {
            var chunks = new List<List<T>>();
            var chunkCount = collection.Count() / size;

            if (collection.Count % size > 0)
                chunkCount++;

            for (var i = 0; i < chunkCount; i++)
                chunks.Add(collection.Skip(i * size).Take(size).ToList());

            return chunks;
        }
    }
}
