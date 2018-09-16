#region

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using MoreLinq;
using Radon.Core;
using Radon.Services;
using Radon.Services.External;
using Raven.Client.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

#endregion

namespace Radon.Modules
{
    [CommandCategory(CommandCategory.General)]
    [Summary("Displays a message with all commands or some information about a command")]
    public class GeneralModule : CommandBase
    {
        private readonly CommandService _commands;
        private readonly Configuration _configuration;
        private readonly DiscordShardedClient _client;
        public GeneralModule(DiscordShardedClient client, CommandService commands, Configuration configuration)
        {
            _client = client;
            _commands = commands;
            _configuration = configuration;
        }
        [Command("ping")]
        [Summary("Displays the bot's latency")]
        public async Task PingAsync()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            EmbedBuilder embed = NormalizeEmbed(":ping_pong: Pong", $"Gateway Latency ❯ {Context.Client.Latency}ms");
            IUserMessage message = await ReplyAsync(embed: embed.Build());
            watch.Stop();
            embed.Description += $"\nMessage Latency ❯ {watch.Elapsed.TotalMilliseconds}ms";
            await message.ModifyAsync(x => x.Embed = embed.Build());
        }
        [Command("info")]
        [Alias("i")]
        [Summary("Shows some basic informations about the bot")]
        public async Task InfoAsync()
        {
            await ReplyEmbedAsync(
                $"{Context.Guild.CurrentUser.Nickname ?? Context.Guild.CurrentUser.Username} Information",
                $"[Official Server]({Configuration.BotDiscordInviteLink})" +
                $"\n[Invite](https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot&permissions=2146958591)" +
                //$"\n[Listcord](https://listcord.com/bot/{Context.Client.CurrentUser.Id})" +
                $"\nShards ❯ {Context.Client.Shards.Count}" +
                $"\nLast Restart ❯ {Process.GetCurrentProcess().StartTime.Humanize()}" +
                $"\nGuilds ❯ {Context.Client.Guilds.Count}" +
                $"\nUsers ❯ {Context.Client.Guilds.Sum(x => x.MemberCount)}" +
                $"\nLatency ❯ {Context.Client.Latency}ms");
        }
        [Command("serverinfo")]
        [Alias("server")]
        [Summary("Shows information about this server")]
        public async Task ServerInfoAsync()
        {
            string emojis = string.Join(" ", Context.Guild.Emotes.Select(x => x.ToString()));
            if (emojis.Length > 1024)
            {
                emojis = emojis.Substring(0, Math.Min(1024, emojis.Length));
                emojis = emojis.Substring(0, emojis.LastIndexOf(' '));
            }
            string roles = string.Join(", ", Context.Guild.Roles.Select(x => x.Mention));
            if (roles.Length > 512)
            {
                roles = roles.Substring(0, Math.Min(512, roles.Length));
                roles = roles.Substring(0, roles.LastIndexOf(','));
            }
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .AddField("General Information",
                    $"Name ❯ {Context.Guild.Name}" +
                    $"\nId ❯ {Context.Guild.Id}" +
                    $"\nOwner ❯ {Context.Guild.Owner.Mention}" +
                    $"\nVerification ❯ {Context.Guild.VerificationLevel}" +
                    $"\nAfk Channel ❯ {(Context.Guild.AFKChannel == null ? "None" : Context.Guild.AFKChannel.Name)}" +
                    $"\nAfk Timeout ❯ {Context.Guild.AFKTimeout.Minutes().TotalMinutes} minutes" +
                    $"\nHighest Role ❯ {Context.Guild.Roles.OrderByDescending(x => x.Position).First().Mention}" +
                    $"\nCreated On ❯ {Context.Guild.CreatedAt:G}",
                    true)
                .AddField($"Members - {Context.Guild.MemberCount}",
                    $"<:online:486264689546887169> Online ❯ {Context.Guild.Users.Count(x => x.Status == UserStatus.Online && !x.IsBot)}" +
                    $"\n<:idle:486264689408344074> Idle ❯ {Context.Guild.Users.Count(x => x.Status == UserStatus.Idle && !x.IsBot)}" +
                    $"\n<:donotdisturb:486264689953603584> DoNotDisturb ❯ {Context.Guild.Users.Count(x => x.Status == UserStatus.DoNotDisturb && !x.IsBot)}" +
                    $"\n<:streaming:486264689509269504> Streaming ❯ {Context.Guild.Users.Count(x => x.Activity?.Type == ActivityType.Streaming && !x.IsBot)}" +
                    $"\n<:offline:486264689244897311> Offline ❯ {Context.Guild.MemberCount - Context.Guild.Users.Count(x => x.Status == UserStatus.Offline)}" +
                    $"\n<:bot:486264689089708033> Bots ❯ {Context.Guild.Users.Count(x => x.IsBot)}",
                    true)
                .AddField($"Channels ❯ {Context.Guild.Channels.Count}",
                    $"Categories ❯ {Context.Guild.CategoryChannels.Count}" +
                    $"\nText Channels ❯ {Context.Guild.TextChannels.Count}" +
                    $"\nVoice Channels ❯ {Context.Guild.VoiceChannels.Count}",
                    true);

            if (roles.Any())
                embed.AddField($"Roles - {Context.Guild.Roles.Count}",
                    roles, true);

            if (emojis.Any())
                embed.AddField($"Emojis - {Context.Guild.Emotes.Count}",
                    emojis, true);

            await ReplyEmbedAsync(embed);
        }
        [Command("Feedback")]
        [Alias("fb")]
        [Summary("Sends a message to the owner of the bot with your feedback")]
        public async Task FeedbackAsync([Remainder]string message)
        {
            foreach (ulong item in _configuration.OwnerIds)
            {
                EmbedBuilder builder = new EmbedBuilder()
                    .WithTitle("Feedback message:")
                    .AddField($"From: {Context.User.Username.ToString()}", $"{message}")
                    .WithCurrentTimestamp();

                IDMChannel dmChannel = await _client.GetUser(item).GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync(embed: builder.Build());
            }
            EmbedBuilder embed = new EmbedBuilder().WithTitle("Thanks for your feedback").WithCurrentTimestamp();
            await ReplyEmbedAsync(embed: embed);
        }
        [CommandCategory(CommandCategory.General)]
        [Group("help")]
        [Summary("Gives you help with a command or category")]
        public class HelpModule : CommandBase
        {
            private readonly CommandService _commands;
            public HelpModule(CommandService commands)
            {
                _commands = commands;
            }
            [Command("")]
            [Summary("Displays information about a specific command or all commands with a specific category")]
            [Priority(-1)]
            public async Task HelpAsync(string command)
            {
                SearchResult result = _commands.Search(command);
                EmbedBuilder embed = new EmbedBuilder();
                if (!result.IsSuccess)
                {
                    if (Enum.TryParse(enumType: typeof(CommandCategory), value: command, ignoreCase: true, result: out object categoryObj))
                    {
                        CommandCategory category = (CommandCategory)categoryObj;
                        if (Server != null && Server.DisabledCategories.Contains(category))
                        {
                            await ReplyEmbedAsync("Unknown command",
                                $"Couldn't find a command or category for {command}");
                            return;
                        }
                        IEnumerable<ModuleInfo> modules = _commands.Modules.Where(x =>
                            x.Attributes.OfType<CommandCategoryAttribute>().FirstOrDefault()?.Category == category);
                        if (modules.Count() <= 8)
                        {
                            embed.WithTitle($"{category.Humanize(LetterCasing.Title)} Commands")
                                .WithColor(Color.Purple);
                            foreach (ModuleInfo module in modules)
                            {
                                if (string.IsNullOrWhiteSpace(module.Group))
                                {
                                    foreach (CommandInfo commandInfo in module.Commands)
                                    {
                                        embed.AddField($"{commandInfo.GetName().Humanize(LetterCasing.Title)}",
                                            $"{commandInfo.Summary}\n{commandInfo.GetUsage(Context).InlineCode()}");
                                    }
                                }
                                else
                                {
                                    embed.AddField($"{module.Group.Humanize(LetterCasing.Title)}",
                                        $"{module.Summary}\n{module.GetUsage(Context).InlineCode()}");
                                }
                            }

                            await ReplyEmbedAsync(embed);
                        }
                        else
                        {
                            List<EmbedBuilder> embeds = new List<EmbedBuilder>();
                            foreach (IEnumerable<ModuleInfo> mdls in modules.Batch(8))
                            {
                                foreach (ModuleInfo module in mdls)
                                {
                                    if (string.IsNullOrWhiteSpace(module.Group))
                                    {
                                        foreach (CommandInfo commandInfo in module.Commands)
                                        {
                                            embed.AddField($"{commandInfo.GetName().Humanize(LetterCasing.Title)}",
                                                $"{commandInfo.Summary}\n{commandInfo.GetUsage(Context).InlineCode()}");
                                        }
                                    }
                                    else
                                    {
                                        embed.AddField($"{module.Group.Humanize(LetterCasing.Title)}",
                                            $"{module.Summary}\n{module.GetUsage(Context).InlineCode()}");
                                    }
                                }

                                EmbedBuilder embd = NormalizeEmbed($"{category.Humanize(LetterCasing.Title)} Commands", null);
                                embeds.Add(embd);
                            }
                            embeds = MoreEnumerable.ToHashSet(embeds).ToList();
                            await PagedReplyAsync(embeds);
                        }
                        return;
                    }
                    await ReplyEmbedAsync("Unknown command",
                        $"Couldn't find a command or category for {command}");
                    return;
                }
                CommandInfo specificCommand = result.Commands.First().Command;
                CommandCategory? specificCategory =
                    specificCommand.Attributes.OfType<CommandCategoryAttribute>().FirstOrDefault()?.Category ??
                    specificCommand.Module.Attributes.OfType<CommandCategoryAttribute>().FirstOrDefault()?.Category;

                if (specificCategory.HasValue && Server.DisabledCategories.Contains(specificCategory.Value))
                {
                    await ReplyEmbedAsync("Unknown command",
                        $"Couldn't find a command or category for {command}");
                    return;
                }
                embed.WithTitle($"{command.Humanize(LetterCasing.Title)} Command Help")
                    .WithDescription(result.Commands.First().Command.Summary)
                    .AddField("Usage",
                        string.IsNullOrWhiteSpace(result.Commands.First().Command.Module.Group)
                            ? result.Commands.Select(x => x.Command).GetUsage(Context).InlineCode()
                            : result.Commands.First().Command.Module.GetUsage(Context).InlineCode());
                if (specificCommand.Aliases.Count > 1)
                    embed.AddField("Aliases",
                        string.Join(", ", specificCommand.Aliases.Select(Formatter.InlineCode)));
                await ReplyEmbedAsync(embed);
            }
            [Command("")]
            [Summary("Displays all commands")]
            [Priority(-1)]
            public async Task HelpAsync()
            {
                List<EmbedBuilder> embeds = new List<EmbedBuilder>();
                foreach (object value in Enum.GetValues(typeof(CommandCategory)))
                {
                    CommandCategory category = (CommandCategory)value;
                    if (Server != null && Server.DisabledCategories.Contains(category)) continue;
                    EmbedBuilder embed = NormalizeEmbed($"{category.Humanize(LetterCasing.Title)} Commands",
                        $"Use {"help <command/category>".InlineCode()} to see more information about a specific command/categoory" +
                        $"\n\n{"< >".InlineCode()} indicates a required parameter\n{"( )".InlineCode()} indicates an optional parameter");
                    IEnumerable<ModuleInfo> modules = _commands.Modules.Where(x =>
                        x.Attributes.OfType<CommandCategoryAttribute>().FirstOrDefault()?.Category == category);
                    switch (modules.Count())
                    {
                        case 1:
                            {
                                foreach (ModuleInfo module in modules)
                                {
                                    if (string.IsNullOrWhiteSpace(module.Group))
                                    {
                                        foreach (CommandInfo commandInfo in module.Commands)
                                        {
                                            embed.AddField($"{commandInfo.GetName().Humanize(LetterCasing.Title)}",
                                                $"{commandInfo.Summary}\n{commandInfo.GetUsage(Context).InlineCode()}");
                                        }
                                    }
                                    else
                                        embed.AddField($"{module.Group.Humanize(LetterCasing.Title)}",
                                            $"{module.Summary}\n{module.GetUsage(Context).InlineCode()}");
                                }

                                embeds.Add(embed);
                                break;
                            }
                        default:
                            {
                                List<EmbedBuilder> pageEmbeds = new List<EmbedBuilder>();
                                foreach (IEnumerable<ModuleInfo> batchedModules in modules.Batch(4))
                                {
                                    foreach (ModuleInfo module in batchedModules)
                                    {
                                        if (string.IsNullOrWhiteSpace(module.Group))
                                        {
                                            foreach (CommandInfo commandInfo in module.Commands)
                                            {
                                                embed.AddField($"{commandInfo.GetName().Humanize(LetterCasing.Title)}",
                                                    $"{commandInfo.Summary}\n{commandInfo.GetUsage(Context).InlineCode()}");
                                            }
                                        }
                                        else
                                            embed.AddField($"{module.Group.Humanize(LetterCasing.Title)}",
                                                $"{module.Summary}\n{module.GetUsage(Context).InlineCode()}");
                                    }

                                    EmbedBuilder pageEmbed = NormalizeEmbed(embed);
                                    pageEmbeds.Add(pageEmbed);
                                }
                                embeds.AddRange(pageEmbeds);
                                break;
                            }
                    }
                }
                switch (embeds.Count)
                {
                    case 1:
                        await ReplyAsync(embed: embeds.First().Build());
                        break;
                    default:
                        embeds = MoreEnumerable.ToHashSet(embeds).ToList();
                        await PagedReplyAsync(embeds);
                        break;
                }
            }
        }
        [Group("tag")]
        [CommandCategory(CommandCategory.General)]
        [Summary("Lets you create and show tags")]
        [CheckServer]
        public class TagModule : CommandBase
        {
            [Command("")]
            [Priority(-1)]
            public async Task TagAsync([Remainder] string tag)
            {
                EmbedBuilder embed = new EmbedBuilder();
                if (Server.Tags.TryGetValue(tag, out Tag specificTag))
                {
                    SocketGuildUser user = Context.Guild.GetUser(specificTag.AuthorId);
                    embed.WithTitle($"{specificTag.Name.Humanize(LetterCasing.Title)}")
                        .WithDescription(specificTag.Message)
                        .WithFooter($"by {(user == null ? "invalid-user" : user.Nickname ?? user.Username)}")
                        .WithTimestamp(specificTag.TimeStamp);
                }
                else
                {
                    embed.WithTitle("Tag Not Found")
                        .WithDescription($"Couldn't find a tag for {tag.InlineCode()}");
                    IEnumerable<KeyValuePair<string, Tag>> bestMatchingTags = Server.Tags.OrderBy(pair => tag.CalculateDifference(pair.Key))
                        .Take(3);
                    if (bestMatchingTags.Any())
                        embed.Description += string.Join("",
                            bestMatchingTags.Select(x => $"\n- {x.Key.InlineCode()}"));
                }
                await ReplyEmbedAsync(embed);
            }
            [Command("add")]
            [Alias("a")]
            public async Task AddTagAsync(string name, [Remainder] string message)
            {
                if (Server.Tags.TryGetValue(name, out Tag tag))
                {
                    if (!CheckPermissions(tag)) return;
                    tag.AuthorId = Context.User.Id;
                    tag.Name = name;
                    tag.Message = message;
                    await ReplyEmbedAsync("Tag Updated", $"Updated the message for the tag {name.InlineCode()}");
                }
                else
                {
                    tag = new Tag
                    {
                        AuthorId = Context.User.Id,
                        Name = name,
                        Message = message,
                        TimeStamp = DateTimeOffset.Now
                    };
                    await ReplyEmbedAsync("Tag Added", $"Added the tag {tag.Name.InlineCode()}");
                }
                Server.Tags[name] = tag;
            }
            [Command("make", RunMode = RunMode.Async)]
            [Alias("m")]
            public async Task MakeAsync()
            {
                await ReplyEmbedAsync("Make A New Tag",
                    $"What should be the name of the tag? (Type {"cancel".InlineCode()} to stop)");
                string name = (await NextMessageAsync()).Content;
                if (string.Equals(name, "cancel", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyEmbedAsync("Cancelled", "Cancelled the tag creation");
                    return;
                }
                await ReplyEmbedAsync("Make A New Tag",
                    $"What should be the message of the tag? (Type {"cancel".InlineCode()} to stop)");
                string message = (await NextMessageAsync()).Content;
                if (string.Equals(message, "cancel", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyEmbedAsync("Cancelled", "Cancelled the tag creation");
                    return;
                }
                await AddTagAsync(name, message);
            }
            [Command("claim")]
            public async Task ClaimAsync([Remainder] string tag)
            {
                if (Server.Tags.TryGetValue(tag, out Tag specificTag))
                {
                    if (!CheckPermissions(specificTag)) return;
                    specificTag.AuthorId = Context.User.Id;
                    specificTag.Name = tag;
                }
                else
                {
                    specificTag = new Tag
                    {
                        AuthorId = Context.User.Id,
                        Message = "None set",
                        Name = tag,
                        TimeStamp = DateTimeOffset.Now
                    };
                }
                await ReplyEmbedAsync("Tag Claimed", $"Claimed the tag {specificTag.Name.InlineCode()}");
                Server.Tags[tag] = specificTag;
            }
            [Command("edit", RunMode = RunMode.Async)]
            public async Task EditAsync()
            {
                await ReplyEmbedAsync("Edit Tag",
                    $"What is the name of the tag? (Type {"cancel".InlineCode()} to stop)");
                String name = (await NextMessageAsync()).Content;
                if (string.Equals(name, "cancel", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyEmbedAsync("Cancelled", "Cancelled the tag editing");
                    return;
                }
                await EditAsync(name);
            }
            [Command("edit", RunMode = RunMode.Async)]
            public async Task EditAsync([Remainder] string name)
            {
                await ReplyEmbedAsync("Edit Tag",
                    $"What should be the new message of the tag? (Type {"cancel".InlineCode()} to stop)");
                string message = (await NextMessageAsync()).Content;
                if (string.Equals(message, "cancel", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyEmbedAsync("Cancelled", "Cancelled the tag editing");
                    return;
                }
                await EditAsync(name, message);
            }
            [Command("edit", RunMode = RunMode.Async)]
            public async Task EditAsync(string name, [Remainder] string message)
            {
                if (Server.Tags.TryGetValue(name, out Tag tag))
                {
                    if (!CheckPermissions(tag)) return;
                    tag.AuthorId = Context.User.Id;
                    tag.Name = name;
                    tag.Message = message;
                    await ReplyEmbedAsync("Tag Updated", $"Updated the message for the tag {name.InlineCode()}");
                }
                else
                {
                    await ReplyEmbedAsync("Tag Not Found", $"Couldn't find the tag {name.InlineCode()}");
                    return;
                }
                Server.Tags[name] = tag;
            }
            [Command("alias", RunMode = RunMode.Async)]
            public async Task AliasAsync()
            {
                await ReplyEmbedAsync("Edit Tag",
                    $"What is the name of the tag? (Type {"cancel".InlineCode()} to stop)");
                string name = (await NextMessageAsync()).Content;
                if (string.Equals(name, "cancel", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyEmbedAsync("Cancelled", "Cancelled the tag editing");
                    return;
                }
                await AliasAsync(name);
            }
            [Command("alias", RunMode = RunMode.Async)]
            public async Task AliasAsync([Remainder] string name)
            {
                await ReplyEmbedAsync("Edit Tag",
                    $"What should be the new name of the tag? (Type {"cancel".InlineCode()} to stop)");
                string newName = (await NextMessageAsync()).Content;
                if (string.Equals(newName, "cancel", StringComparison.OrdinalIgnoreCase))
                {
                    await ReplyEmbedAsync("Cancelled", "Cancelled the tag editing");
                    return;
                }
                await AliasAsync(name, newName);
            }
            [Command("alias", RunMode = RunMode.Async)]
            public async Task AliasAsync(string name, [Remainder] string newname)
            {
                if (Server.Tags.TryGetValue(name, out Tag tag))
                {
                    if (!CheckPermissions(tag)) return;
                    await ReplyEmbedAsync("Tag Updated",
                        $"Updated the name of the tag {tag.Name.InlineCode()} to {newname.InlineCode()}");
                    tag.AuthorId = Context.User.Id;
                    tag.Name = newname;
                }
                else
                {
                    await ReplyEmbedAsync("Tag Not Found", $"Couldn't find the tag {name.InlineCode()}");
                    return;
                }
                Server.Tags[newname] = tag;
                Server.Tags.Remove(name);
            }
            [Command("delete")]
            [Alias("d", "del")]
            public async Task DeleteAsync([Remainder] string tag)
            {
                if (Server.Tags.TryGetValue(tag, out Tag specificTag))
                {
                    if (!CheckPermissions(specificTag)) return;
                    Server.Tags.Remove(tag);
                    await ReplyEmbedAsync("Tag Removed", $"Removed the tag {specificTag.Name.InlineCode()}");
                }
                else
                {
                    await ReplyEmbedAsync("Tag Not Found", $"Couldn't find a tag for {tag.InlineCode()}");
                }
            }
            [Command("all")]
            public async Task AllTagsAsync()
            {
                if (!Server.Tags.Any())
                {
                    await ReplyEmbedAsync("No Tags", "There are no tags on this server yet");
                    return;
                }
                if (Server.Tags.Count > 8)
                {
                    IEnumerable<IEnumerable<KeyValuePair<string, Tag>>> seperatedTags = Server.Tags.Batch(8);
                    List<EmbedBuilder> pages = seperatedTags.Select(tags => tags.Select(tag => $"❯ {tag.Value.Name} ({(DateTime.Now - tag.Value.TimeStamp).Humanize()}) ago")
                            .ToList())
                        .Select(tagList => NormalizeEmbed("Tags", string.Join("\n", tagList)))
                        .ToList();

                    await PagedReplyAsync(pages);
                }
                else
                {
                    IEnumerable<string> descriptions = Server.Tags.Select(tag =>
                        $"❯ {tag.Value.Name} ({(DateTimeOffset.Now - tag.Value.TimeStamp).Humanize()} ago)");
                    await ReplyEmbedAsync("Tags", string.Join("\n", descriptions));
                }
            }
            private bool CheckPermissions(Tag tag)
            {
                SocketGuildUser target = Context.Guild.GetUser(tag.AuthorId);
                if (target == null) return true;
                if (target.Id == Context.User.Id) return true;

                return target.Hierarchy < ((SocketGuildUser)Context.User).Hierarchy;
            }
        }
        [Group("profile")]
        [Alias("p")]
        [CommandCategory(CommandCategory.General)]
        [CheckServer]
        public class ProfileModule : CommandBase
        {
            [Command("")]
            public async Task ProfileAsync() => await ProfileAsync((SocketGuildUser)Context.User);
            [Command("")]
            public async Task ProfileAsync(SocketGuildUser user)
            {
                EmbedBuilder embed = new EmbedBuilder()
                    .WithAuthor($"{user.Nickname ?? user.Username}'s profile", user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
                    .WithDescription($"Id ❯ {user.Id}" +
                                     $"\nJoined This Server ❯ {user.JoinedAt:dd.MM.yyyy HH:mm:ss} ({user.JoinedAt.Humanize()})" +
                                     $"\nJoined Discord ❯ {user.CreatedAt:dd.MM.yyyy HH:mm:ss} ({user.CreatedAt.Humanize()})" +
                                     $"\nPosition ❯ {(user.Hierarchy == int.MaxValue ? Context.Guild.Roles.Max(x => x.Position) : user.Hierarchy)}/{Context.Guild.Roles.Max(x => x.Position)}" +
                                     $"\nStatus ❯ {user.Status.Humanize(LetterCasing.Title)}");
                await ReplyEmbedAsync(embed);
            }
        }
    }
}