#region
using Discord;
using Discord.Commands;
using GiphyDotNet.Manager;
using GiphyDotNet.Model.Parameters;
using Newtonsoft.Json.Linq;
using Radon.Core;
using Radon.Services;
using Radon.Services.External;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
#endregion

namespace Radon.Modules
{
    public class BanApiModule : CommandBase
    {
        private readonly KsoftBanApiService _banApiService;

        public BanApiModule(KsoftBanApiService banApiService)
        {
            _banApiService = banApiService;
        }

        [Command("Checkbans")]
        [Alias("Scan", "scanBans")]
        [Summary("Checks all users in the current guild for global bans")]
        public async Task CheckBans()
        {
            await _banApiService.CheckUsersAsync(Context);
        }
    }
}
