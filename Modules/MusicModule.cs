using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Radon.Core;
using Radon.Services.External;
using SharpLink;
using System;
using System.Threading.Tasks;

namespace Radon.Modules
{
    public class MusicModule : CommandBase
    {
        private readonly SharplinkService _sharplinkService;
        public MusicModule(SharplinkService sharplinkService) => _sharplinkService = sharplinkService;

        [Group("Play")]
        [Alias("p", "pl")]
        public class Play : CommandBase
        {
            private readonly SharplinkService _sharplinkService;
            public Play(SharplinkService sharplinkService) => _sharplinkService = sharplinkService;
            [Command("")]
            [Summary("Plays a youtube video")]
            public async Task PlayAsync([Remainder] string query)
            {
                try
                {
                    IVoiceChannel voiceChannel = null;
                    voiceChannel = voiceChannel ?? (Context.Message.Author as IGuildUser)?.VoiceChannel;
                    if (voiceChannel == null)
                    {
                        await ReplyEmbedAsync(embed: NormalizeEmbed(title: "Error", description: "User must be in a voice channel."));
                        return;
                    }
                    if ((Context.Client.CurrentUser as IGuildUser)?.VoiceChannel == null)
                    {
                        try
                        {
                            await _sharplinkService.JoinAsync(voiceChannel);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                    LavalinkTrack track = await _sharplinkService.GetTrackAsync(query);
                    await _sharplinkService.PlayAsync(guildID: Context.Guild.Id, voiceChannel: null, track: track);
                    EmbedBuilder embed = new EmbedBuilder()
                        .WithTitle("Added video to queue")
                        .WithUrl(url: track.Url.ToString())
                        .WithDescription($"`{track.Title}`| {track.Length}")
                        .WithThumbnailUrl($"http://img.youtube.com/vi/{track.Url.Replace("https://www.youtube.com/watch?v=", "")}/maxresdefault.jpg");
                    Console.WriteLine(track.Url.ToString());
                    await ReplyEmbedAsync(embed: embed);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            [Command("url")]
            [Summary("Plays a url")]
            public async Task PlayUrlAsync(string url)
            {
                try
                {
                    IVoiceChannel voiceChannel = null;
                    voiceChannel = voiceChannel ?? (Context.Message.Author as IGuildUser)?.VoiceChannel;
                    if (voiceChannel == null)
                    {
                        await ReplyEmbedAsync(embed: NormalizeEmbed("Error", "User must be in a voice channel."));
                        return;
                    }
                    if ((Context.Client.CurrentUser as IGuildUser)?.VoiceChannel == null)
                        await _sharplinkService.JoinAsync(voiceChannel);
                    LavalinkTrack track = await _sharplinkService.GetTrackAsync(url);
                    await _sharplinkService.PlayAsync(Context.Guild.Id, voiceChannel, track);
                    EmbedBuilder embed = new EmbedBuilder()
                        .WithTitle("Added video to queue")
                        .WithUrl(url: track.Url.ToString())
                        .WithDescription($"`{track.Title}`| {track.Length}")
                        .WithThumbnailUrl($"http://img.youtube.com/vi/{track.Url.Replace("https://www.youtube.com/watch?v=", "")}/maxresdefault.jpg");
                    await ReplyEmbedAsync(embed: embed);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
        [Command("Join")]
        [Alias("j")]
        [Summary("Joins your current voice channel")]
        public async Task JoinAsync(IVoiceChannel channel = null)
        {
            try
            {
                IVoiceChannel voiceChannel = null;
                voiceChannel = voiceChannel ?? (Context.Message.Author as IGuildUser)?.VoiceChannel;
                if (voiceChannel == null && channel == null)
                {
                    await ReplyEmbedAsync(embed: NormalizeEmbed(title: "Error", description: "User must be in a voice channel, or a voice channel must be passed as an argument."));
                }
                if (channel != null) voiceChannel = channel;
                if ((Context.Client.CurrentUser as IGuildUser)?.VoiceChannel != null)
                    await _sharplinkService.LeaveAsync(Context.Guild.Id);
                await _sharplinkService.JoinAsync(voiceChannel);
                await ReplyEmbedAsync(embed: NormalizeEmbed(title: "Joined", description: $"Channel: {voiceChannel.Name}"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        [Command("Leave")]
        [Alias("l", "lv")]
        [Summary("Leaves current voice channel")]
        public async Task LeaveAsync()
        {
            try
            {
                await _sharplinkService.LeaveAsync(Context.Guild.Id);
                await ReplyEmbedAsync(embed: NormalizeEmbed(embed: new EmbedBuilder().WithTitle("Left")));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        [Command("Stop")]
        [Alias("s", "st")]
        [Summary("Stops playing stuff")]
        public async Task StopAsync()
        {
            try
            {
                ulong guildID = Context.Guild.Id;
                await _sharplinkService.PauseAsync(guildID);
                await _sharplinkService.StopAsync(guildID);
                await ReplyEmbedAsync(embed: NormalizeEmbed(embed: new EmbedBuilder().WithTitle("Stopped")));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        [Command("YTSearch")]
        [Alias("search")]
        [Summary("Searches youtube")]
        public async Task SearchAsync([Remainder]string query)
        {
            try
            {
                string response = await _sharplinkService.SearchAsync(query);
                await ReplyEmbedAsync(NormalizeEmbed(title: $"Results for {query}:", description: response));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        [Command("Pause")]
        [Summary("Pauses current playback")]
        public async Task PauseAsync()
        {
            try
            {
                SocketGuild guild = Context.Guild;
                await _sharplinkService.PauseAsync(guild.Id);
                await ReplyEmbedAsync(embed: NormalizeEmbed(embed: new EmbedBuilder().WithTitle("Paused")));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        [Command("Resume")]
        [Summary("Resumes paused playback")]
        public async Task ResumeAsync()
        {
            try
            {
                SocketGuild guild = Context.Guild;
                await _sharplinkService.ResumeAsync(guild.Id);
                await ReplyEmbedAsync(embed: NormalizeEmbed(embed: new EmbedBuilder().WithTitle("Resumed")));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        [Command("Seek")]
        [Summary("Seeks to position in current playback")]
        public async Task SeekAsync(string time)
        {
            try
            {
                double seconds = TimeSpan.Parse(time).TotalSeconds;
                SocketGuild guild = Context.Guild;
                Console.WriteLine(time);
                Console.WriteLine(seconds);
                await _sharplinkService.SeekAsync(guildID: guild.Id, position: (int)seconds);
                await ReplyEmbedAsync(embed: NormalizeEmbed(embed: new EmbedBuilder().WithTitle("Seekified")));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        [Command("Volume")]
        [Alias("vol")]
        [Summary("Changes volume 0-150")]
        public async Task SetVolumeAsync(uint volume)
        {
            try
            {
                if (volume > 150 || volume < 1)
                {
                    await ReplyEmbedAsync(embed: NormalizeEmbed("Error", "Invalid value, use only 1-150"));
                    return;
                }
                SocketGuild guild = Context.Guild;
                await _sharplinkService.SetVolumeAsync(guildID: guild.Id, volume: volume);
                await ReplyEmbedAsync(embed: NormalizeEmbed(new EmbedBuilder().WithTitle("Volume changed")));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        [Command("NowPlaying")]
        [Alias("np")]
        [Summary("Displays info about what's currently playing")]
        public async Task NowPlayingAsync()
        {
            try
            {
                LavalinkTrack info = _sharplinkService.GetTrackInfo(guildID: Context.Guild.Id);
                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle("Added video to queue")
                    .WithUrl(url: info.Url.ToString())
                    .WithDescription($"`{info.Title}`| {info.Length}")
                    .WithThumbnailUrl($"http://img.youtube.com/vi/{info.Url.Replace("https://www.youtube.com/watch?v=", "")}/maxresdefault.jpg")
                    .AddField(name: "Current position", value: $"{info.Position.ToString()}")
                    ;
                await ReplyEmbedAsync(embed: embed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
