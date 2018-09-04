#region

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Radon.Core;
using Radon.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ActionType = Radon.Services.ActionType;

#endregion

namespace Radon.Modules
{
    [CommandCategory(CommandCategory.Moderation)]
    [CheckServer]
    [CheckState]
    public class ModerationModule : CommandBase
    {
        private readonly LogService _logService;
        private readonly ServerService _serverService;

        public ModerationModule(LogService logService, ServerService serverService)
        {
            _logService = logService;
            _serverService = serverService;
        }

        [Command("ban")]
        [Description("Bans a user")]
        [CheckPermission(GuildPermission.BanMembers)]
        [CheckBotPermission(GuildPermission.BanMembers)]
        [Summary("Bans a user for a given reason")]
        public async Task BanAsync([CheckBotHierarchy] [CheckUserHierarchy]
            IGuildUser user, [Remainder] string reason = null)
        {
            await user.SendMessageAsync(embed: NormalizeEmbed("You got banned",
                    $"Server ❯ {Context.Guild.Name}\nResponsible User ❯ {Context.User.Mention}\nReason ❯ {reason ?? "none"}")
                .Build());

            await user.BanAsync(reason: reason);

            var logItem = _serverService.AddLogItem(Server, ActionType.Ban, reason, Context.User.Id, user.Id);

            await ReplyEmbedAsync("Member Banned",
                $"User ❯ {user.Mention}\nReason ❯ {reason ?? $"none, {Context.User.Mention} use {$"reason {logItem.LogId} <reason>".InlineCode()}"}");

            await _logService.SendLog(Context.Guild, "Member banned",
                $"User ❯ {Context.User.Mention} ({(Context.User as IGuildUser)?.Nickname ?? Context.User.Username})\nResponsible User ❯ {Context.User.Mention}\nReason ❯ {reason ?? $"none, {Context.User.Mention} use {$"reason {logItem.LogId} <reason>".InlineCode()}"}\nId ❯ {logItem.LogId}",
                server: Server);
        }

        [Command("kick")]
        [Description("Kicks a user")]
        [CheckPermission(GuildPermission.KickMembers)]
        [CheckPermission(GuildPermission.KickMembers)]
        [Summary("Kicks a user for a given reason")]
        public async Task KickAsync([CheckBotHierarchy] [CheckUserHierarchy]
            IGuildUser user, [Remainder] string reason = null)
        {
            await user.SendMessageAsync(embed: NormalizeEmbed("You got kicked",
                    $"Server ❯ {Context.Guild.Name}\nResponsible User ❯ {Context.User.Mention}\nReason ❯ {reason ?? "none"}")
                .Build());

            await user.KickAsync(reason);

            var logItem = _serverService.AddLogItem(Server, ActionType.Kick, reason, Context.User.Id, user.Id);

            await ReplyEmbedAsync("Member Kicked",
                $"User ❯ {user.Mention}\nReason ❯ {reason ?? $"none, {Context.User.Mention} use {$"reason {logItem.LogId} <reason>".InlineCode()}"}");

            await _logService.SendLog(Context.Guild, "Member Kicked",
                $"User ❯ {Context.User.Mention} ({(Context.User as IGuildUser)?.Nickname ?? Context.User.Username})\nResponsible User ❯ {Context.User.Mention}\nReason ❯ {reason ?? $"none, {Context.User.Mention} use {$"reason {logItem.LogId} <reason>".InlineCode()}"}\nId ❯ {logItem.LogId}",
                server: Server);
        }

        [Command("clear")]
        [CheckPermission(GuildPermission.ManageMessages)]
        [CheckPermission(ChannelPermission.ManageMessages)]
        [Summary("Clears some messages from the current chat")]
        public async Task ClearAsync(int count, [Remainder] string reason = null)
        {
            var messages = await Context.Channel.GetMessagesAsync(count + 1).FlattenAsync();
            messages = messages.Where(x => x.Timestamp >= DateTimeOffset.Now - TimeSpan.FromDays(14));
            await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);
            await ReplyAndDeleteAsync(embed: NormalizeEmbed("Chat Cleared",
                $"Deleted the last {$"{messages.Count() - 1}".InlineCode()} messages").Build());
            await _logService.SendLog(Context.Guild, "Chat cleared",
                $"Responsible User ❯ {Context.User.Mention}\nMessages ❯ {messages.Count() - 1}\nReason ❯ none",
                server: Server);
        }

        [Command("mute")]
        [Alias("m")]
        [CheckPermission(ChannelPermission.MuteMembers)]
        [CheckBotPermission(GuildPermission.ManageRoles)]
        [Summary("Mutes a user for a given reason")]
        public async Task MuteAsync([CheckUserHierarchy] [CheckBotHierarchy]
            SocketGuildUser user, [Remainder] string reason = null)
        {
            var roleName = $"{Context.Client.CurrentUser.Username}-Muted";
            var role = Context.Guild.Roles.All(x => x.Name != roleName)
                ? Context.Guild.GetRole(
                    (await Context.Guild.CreateRoleAsync(roleName,
                        new GuildPermissions(sendMessages: false))).Id)
                : Context.Guild.Roles.FirstOrDefault(x => x.Name == roleName);

            if (user.Roles.Contains(role))
            {
                await ReplyEmbedAsync("Already Muted", $"{user.Mention} is already muted");
                return;
            }

            await user.AddRoleAsync(role);

            var logItem = _serverService.AddLogItem(Server, ActionType.Mute, reason, Context.User.Id, user.Id);
            await _logService.SendLog(Context.Guild, "User Muted",
                $"Responsible User ❯ {Context.User.Mention}\nUser ❯ {user.Mention} ({user.Nickname ?? user.Username}#{user.Discriminator})\nReason ❯ {reason ?? $"none, {Context.User.Mention} use {$"reason {logItem.LogId} <reason>".InlineCode()}"}\nId ❯ {logItem.LogId}",
                server: Server);
            await ReplyEmbedAsync("User Muted",
                $"Responsible User ❯ {Context.User.Mention}\nReason ❯ {reason ?? $"none, {Context.User.Mention} use {$"reason {logItem.LogId} <reason>".InlineCode()}"}");

            try
            {
                await user.SendMessageAsync(embed: NormalizeEmbed("You Got Muted",
                        $"Responsible User ❯ {Context.User.Mention} ({(Context.User as IGuildUser)?.Nickname ?? Context.User.Username})")
                    .Build());
            }
            catch (Exception)
            {
                // ignored
            }

            var overwritePermissions = new OverwritePermissions(sendMessages: PermValue.Deny,
                addReactions: PermValue.Deny, speak: PermValue.Deny);
            var overwrite = new Overwrite(role.Id, PermissionTarget.Role, overwritePermissions);

            foreach (var channel in Context.Guild.Channels)
                if (!channel.PermissionOverwrites.Contains(overwrite))
                    await channel.AddPermissionOverwriteAsync(role, overwritePermissions);
        }

        [Command("unmute")]
        [Alias("um")]
        [CheckPermission(ChannelPermission.MuteMembers)]
        [CheckBotPermission(GuildPermission.ManageRoles)]
        [Summary("Unmutes a user for a given reason")]
        public async Task UnMuteAsync([CheckUserHierarchy] [CheckBotHierarchy]
            SocketGuildUser user, [Remainder] string reason = null)
        {
            var roleName = $"{Context.Client.CurrentUser.Username}-Muted";
            var role = Context.Guild.Roles.All(x => x.Name != roleName)
                ? Context.Guild.GetRole(
                    (await Context.Guild.CreateRoleAsync(roleName,
                        new GuildPermissions(sendMessages: false))).Id)
                : Context.Guild.Roles.FirstOrDefault(x => x.Name == roleName);

            if (!user.Roles.Contains(role))
            {
                await ReplyEmbedAsync("Not Muted", $"{user.Mention} is already unmuted");
                return;
            }

            await user.RemoveRoleAsync(role);

            var logItem = _serverService.AddLogItem(Server, ActionType.Unmute, reason, Context.User.Id, user.Id);
            await _logService.SendLog(Context.Guild, "User Unmuted",
                $"Responsible User ❯ {Context.User.Mention}\nUser ❯ {user.Mention} ({user.Nickname ?? user.Username}#{user.Discriminator})\nReason ❯ {reason ?? $"none, {Context.User.Mention} use {$"reason {logItem.LogId} <reason>".InlineCode()}"}\nId ❯ {logItem.LogId}",
                server: Server);
            await ReplyEmbedAsync("User Unmuted",
                $"Responsible User ❯ {Context.User.Mention}\nReason ❯ {reason ?? $"none, {Context.User.Mention} use {$"reason {logItem.LogId} <reason>".InlineCode()}"}");

            try
            {
                await user.SendMessageAsync(embed: NormalizeEmbed("You Got Unmuted",
                        $"Responsible User ❯ {Context.User.Mention} ({(Context.User as IGuildUser)?.Nickname ?? Context.User.Username})")
                    .Build());
            }
            catch (Exception)
            {
                // ignored
            }

            var overwritePermissions = new OverwritePermissions(sendMessages: PermValue.Deny,
                addReactions: PermValue.Deny, speak: PermValue.Deny);
            var overwrite = new Overwrite(role.Id, PermissionTarget.Role, overwritePermissions);

            foreach (var channel in Context.Guild.Channels)
                if (!channel.PermissionOverwrites.Contains(overwrite))
                    await channel.AddPermissionOverwriteAsync(role, overwritePermissions);

        }

        [Command("bulk")]
        [CheckPermission(ChannelPermission.ManageChannels)]
        [CheckBotPermission(ChannelPermission.ManageChannels)]
        [Summary("Deletes all messages from this channel")]
        public async Task BulkAsync(string reason = null)
        {
            var oldChannel = (ITextChannel)Context.Channel;
            await oldChannel.DeleteAsync();
            var channel = (ITextChannel)await Context.Guild.CreateTextChannelAsync(Context.Channel.Name);
            await channel.ModifyAsync(x =>
            {
                x.IsNsfw = oldChannel.IsNsfw;
                x.Topic = oldChannel.Topic;
                x.CategoryId = oldChannel.CategoryId;
                x.Position = oldChannel.Position;
            });
            foreach (var permissionOverwrite in oldChannel.PermissionOverwrites)
                switch (permissionOverwrite.TargetType)
                {
                    case PermissionTarget.Role:
                        var role = Context.Guild.GetRole(permissionOverwrite.TargetId);
                        await channel.AddPermissionOverwriteAsync(role, permissionOverwrite.Permissions);
                        break;
                    case PermissionTarget.User:
                        var user = Context.Guild.GetUser(permissionOverwrite.TargetId);
                        await channel.AddPermissionOverwriteAsync(user, permissionOverwrite.Permissions);
                        break;
                }
            await Interactive.ReplyAndDeleteAsync(channel,
                embed: NormalizeEmbed("Chat Cleared", "Deleted all messages from this channel").Build());
            var logItem = _serverService.AddLogItem(Server, ActionType.Bulk, reason, Context.User.Id, 0);
            await _logService.SendLog(Context.Guild, "Bulk delete",
                $"Responsible User ❯ {Context.User.Mention}\nChannel ❯ {channel.Mention}\nReason ❯ {reason ?? $"none, {Context.User.Mention} use {$"reason {logItem.LogId} <reason>".InlineCode()}"}\nId ❯ {logItem.LogId}",
                server: Server);
        }

        [Group("reason")]
        [CheckState]
        [CommandCategory(CommandCategory.Moderation)]
        [CheckServer]
        [Summary("Lets you see or edit the reason of a log")]
        public class ReasonModule : CommandBase
        {
            [Command("")]
            [Summary("Edits the reason of a log item")]
            [Priority(-1)]
            public async Task ReasonAsync(ulong id, [Remainder] string reason)
            {
                if (Server.ModLog.TryGetValue(id, out var logItem))
                {
                    if (logItem.ResponsibleUserId == Context.User.Id)
                    {
                        logItem.Reason = reason;
                        Server.ModLog[logItem.LogId] = logItem;
                        await ReplyEmbedAsync("Reason Updated",
                            $"Updated the reason for {$"{logItem.LogId}".InlineCode()} to {$"{reason}".InlineCode()}");
                    }
                    else
                    {
                        await ReplyEmbedAsync("Missing Permissions", "This log item doesn\'t belong to you");
                    }
                }
                else
                {
                    await ReplyEmbedAsync("Not Found", $"Couldn't find a log item with the id {$"{id}".InlineCode()}");
                }
            }

            [Command("")]
            [Summary("Shows the reason of a log item")]
            [Priority(-1)]
            public async Task ReasonAsync(ulong id)
            {
                if (Server.ModLog.TryGetValue(id, out var logItem))
                    await ReplyEmbedAsync("Reason",
                        $"The reason of {$"{logItem.LogId}".InlineCode()} is {$"{logItem.Reason ?? "not set"}".InlineCode()}");
                else
                    await ReplyEmbedAsync("Not Found", $"Couldn't find a log item with the id {$"{id}".InlineCode()}");
            }
        }

        [Group("log")]
        [CommandCategory(CommandCategory.Moderation)]
        [CheckState]
        [CheckServer]
        [Summary("Shows the last 10 log items")]
        public class LogModule : CommandBase
        {
            [Command("")]
            [Summary("Shows all log items")]
            [Priority(-1)]
            public async Task LogAsync()
            {
                var logItems = Server.ModLog.Take(10);
                if (!logItems.Any())
                {
                    await ReplyEmbedAsync("Log Empty", "There are no log entrys on this server yet");
                    return;
                }

                await ReplyEmbedAsync($"{Context.Guild.Name} Log",
                    string.Join("\n",
                        logItems.Select(x =>
                            $"❯ {$"{x.Value.ActionType}".ToLower().InlineCode()} {Context.Guild.GetUser(x.Value.ResponsibleUserId)?.Mention ?? "invalid-user"} ⇒ {Context.Guild.GetUser(x.Value.UserId)?.Mention ?? "invalid-user"} ❯ {x.Value.Reason ?? "no reason"}")));
            }

            [Command("")]
            [Summary("Shows all log items with a specific type")]
            [Priority(-1)]
            public async Task LogAsync([Remainder] string category)
            {
                if (Enum.TryParse(typeof(ActionType), category, true, out var specificObject))
                {
                    var specificCategory = (ActionType)specificObject;
                    var logItems = Server.ModLog.Where(x => x.Value.ActionType == specificCategory).Take(10);
                    if (!logItems.Any())
                    {
                        await ReplyEmbedAsync("Log Empty",
                            $"There are no log entrys with the category {$"{specificCategory}".ToLower().InlineCode()} yet");
                        return;
                    }

                    await ReplyEmbedAsync($"{Context.Guild.Name} Log",
                        string.Join("\n",
                            logItems.Select(x =>
                                $"❯ {$"{x.Value.ActionType}".ToLower().InlineCode()} {Context.Guild.GetUser(x.Value.ResponsibleUserId)?.Mention ?? "invalid-user"} ⇒ {Context.Guild.GetUser(x.Value.UserId)?.Mention ?? "invalid-user"} ❯ {x.Value.Reason ?? "no reason"}")));
                }
                else
                {
                    await ReplyEmbedAsync("Category Not Found",
                        $"Aviable categorys: {string.Join(", ", Enum.GetValues(typeof(ActionType)).Cast<ActionType>().Select(x => $"{x}".ToLower().InlineCode()))}");
                }
            }

            [Command("")]
            [Summary("Shows all log items with a specific user")]
            [Priority(-1)]
            public async Task LogAsync(IUser user)
            {
                var logItems = Server.ModLog
                    .Where(x => x.Value.ResponsibleUserId == user.Id || x.Value.UserId == user.Id).Take(10);
                if (!logItems.Any())
                {
                    await ReplyEmbedAsync("Log Empty", "There are no log entrys from this user yet");
                    return;
                }

                await ReplyEmbedAsync($"{Context.Guild.Name} Log",
                    string.Join("\n",
                        logItems.Select(x =>
                            $"❯ {$"{x.Value.ActionType}".ToLower().InlineCode()} {Context.Guild.GetUser(x.Value.ResponsibleUserId)?.Mention ?? "invalid-user"} ⇒ {Context.Guild.GetUser(x.Value.UserId)?.Mention ?? "invalid-user"} ❯ {x.Value.Reason ?? "no reason"}")));
            }
        }
    }
}