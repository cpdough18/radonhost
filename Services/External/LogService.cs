#region

using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Radon.Core;
using Radon.Services.External;

#endregion

namespace Radon.Services
{
    public class LogService
    {
        private readonly DiscordShardedClient _client;
        private readonly Configuration _configuration;
        private readonly DatabaseService _database;
        private readonly Random _random;
        private readonly ServerService _service;
        public LogService(DiscordShardedClient client, DatabaseService database, Random random, ServerService service,
            Configuration configuration)
        {
            _client = client;
            _database = database;
            _random = random;
            _service = service;
            _configuration = configuration;

            _client.ChannelCreated += ChannelCreated;
            _client.ChannelDestroyed += ChannelDestroyed;
            _client.ChannelUpdated += ChannelUpdated;
            _client.GuildMemberUpdated += GuildMemberUpdated;
            _client.MessageDeleted += MessageDeleted;
            _client.MessageUpdated += MessageUpdated;
            _client.RoleCreated += RoleCreated;
            _client.RoleDeleted += RoleDeleted;
            _client.RoleUpdated += RoleUpdated;
            _client.UserBanned += UserBanned;
            _client.UserJoined += UserJoined;
            _client.UserLeft += UserLeft;
            _client.UserUnbanned += UserUnbanned;
            _client.UserJoined += Autorole;
        }

        private async Task Autorole(SocketGuildUser user)
        {
            Server server = null;
            _database.Execute(x => { server = x.Load<Server>($"{user.Guild.Id}"); });
            var role = user.Guild.GetRole(server.AutoroleId.GetValueOrDefault());
            if (role == null) return;
            await user.AddRoleAsync(role);
        }

        private async Task UserUnbanned(SocketUser arg1, SocketGuild guild)
        {
            if (!(arg1 is SocketGuildUser user)) return;
            if (!GetAuditLogEntry(guild, out var auditLog, Discord.ActionType.Unban)) return;

            var logItem = _service.AddLogItem(guild, ActionType.Unban, auditLog.Reason, auditLog.User.Id, user.Id);

            await SendLog(guild, "Member Unbanned",
                $"User ❯ {user.Mention} ({user.Nickname ?? user.Username})\nResponsible User ❯ {auditLog.User.Mention}\nReason ❯ {auditLog.Reason ?? $"none, {auditLog.User.Mention} use {$"reason {logItem.LogId} <reason>".InlineCode()}"}\nId ❯ {auditLog.Id}");
        }

        private async Task UserLeft(SocketGuildUser user)
        {
            Server server = null;
            _database.Execute(x => { server = x.Load<Server>($"{user.Guild.Id}"); });
            if (!server.GetSetting(Setting.LeaveMessage)) return;

            if (!server.AnnounceChannelId.HasValue) return;
            var channel = user.Guild.GetTextChannel(server.AnnounceChannelId.Value);
            if (channel == null) return;

            var message = server.LeaveMessages.Any()
                ? server.LeaveMessages.ToList()[_random.Next(server.LeaveMessages.Count)]
                : _configuration.DefaultLeaveMessage;

            await channel.SendMessageAsync(message.ToMessage(user, user.Guild));
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            Server server = null;
            _database.Execute(x => { server = x.Load<Server>($"{user.Guild.Id}"); });
            if (!server.GetSetting(Setting.JoinMessage)) return;

            if (!server.AnnounceChannelId.HasValue) return;
            var channel = user.Guild.GetTextChannel(server.AnnounceChannelId.Value);
            if (channel == null) return;

            var message = server.JoinMessages.Any()
                ? server.JoinMessages.ToList()[_random.Next(server.JoinMessages.Count)]
                : _configuration.DefaultJoinMessage;

            await channel.SendMessageAsync(message.ToMessage(user, user.Guild));
        }

        private async Task UserBanned(SocketUser arg1, SocketGuild guild)
        {
            if (!(arg1 is SocketGuildUser user)) return;
            if (!GetAuditLogEntry(guild, out var auditLog, Discord.ActionType.Ban)) return;

            var logItem = _service.AddLogItem(guild, ActionType.Ban, auditLog.Reason, auditLog.User.Id, user.Id);

            await SendLog(guild, "Member Banned",
                $"User ❯ {user.Mention} ({user.Nickname ?? user.Username})\nResponsible User ❯ {auditLog.User.Mention}\nReason ❯ {auditLog.Reason ?? $"none, {auditLog.User.Mention} use {$"reason {logItem.LogId} <reason>".InlineCode()}"}\nId ❯ {auditLog.Id}");
        }

        private async Task RoleUpdated(SocketRole roleBefore, SocketRole roleAfter)
        {
            if (!GetAuditLogEntry(roleAfter.Guild, out var auditLog, Discord.ActionType.RoleUpdated)) return;
            var data = (RoleUpdateAuditLogData)auditLog.Data;
            var description =
                $"Role ❯ {roleAfter.Mention}\nResponsible User ❯ {auditLog.User.Mention}\nReason ❯ {auditLog.Reason}\nId ❯ {auditLog.Id}\nChanges ❯";
            foreach (var property in typeof(RoleEditInfo).GetProperties())
            {
                var before = property.GetValue(data.Before);
                var after = property.GetValue(data.After);
                if (before == null || after == null) continue;

                if (before.ToString() != after.ToString()) description += $"\n❯ {property.Name}: {before} ⇒ {after}";
            }

            await SendLog(roleAfter.Guild, "Role Updated", description);
        }

        private async Task RoleDeleted(SocketRole role)
        {
            if (!GetAuditLogEntry(role.Guild, out var auditLog, Discord.ActionType.RoleDeleted)) return;
            await SendLog(role.Guild, "Role Deleted",
                $"Role ❯ {role.Name}\nResponsible User ❯ {auditLog.User.Mention}\nReason ❯ {auditLog.Reason ?? "none"}\nId ❯ {auditLog.Id}");
        }

        private async Task RoleCreated(SocketRole role)
        {
            if (!GetAuditLogEntry(role.Guild, out var auditLog, Discord.ActionType.RoleCreated)) return;
            await SendLog(role.Guild, "Role Created",
                $"Role ❯ {role.Mention}\nResponsible User ❯ {auditLog.User.Mention}\nReason ❯ {auditLog.Reason ?? "none"}\nId ❯ {auditLog.Id}");
        }

        private async Task MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage messageAfter,
            ISocketMessageChannel arg2)
        {
            if (messageAfter.Author.IsBot) return;
            if (!(arg2 is ITextChannel channel)) return;
            if (messageAfter.Author.Id == _client.CurrentUser.Id) return;
            var message = await arg1.GetOrDownloadAsync();
            if (message.Content == messageAfter.Content)
                return;

            var description =
                $"Author ❯ {messageAfter.Author.Mention}\nId ❯ {messageAfter.Id}\nResponsible User ❯ {messageAfter.Author.Mention}\nChannel ❯ {channel.Mention}\nContent ❯ {message.Content} ⇒ {messageAfter.Content}\nReason ❯ none";

            await SendLog(channel.Guild, "Message Updated", description);
        }

        private async Task MessageDeleted(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg)
        {
            if (!(arg is ITextChannel channel)) return;
            GetAuditLogEntry(channel.Guild, out var auditLog, Discord.ActionType.MessageDeleted);
            var message = await arg1.GetOrDownloadAsync();
            var bulkDelete = false;

            var description =
                "Author ❯ {0}\nId ❯ {1}\nResponsible User ❯ {2}\nChannel ❯ {3}\nContent ❯ {4}\nReason ❯ {5}";
            if (auditLog != null && auditLog.Data is MessageDeleteAuditLogData auditLogData)
            {
                if (auditLogData.MessageCount == 1)
                {
                    description =
                        $"Messages ❯ {auditLogData.MessageCount}\nResponsible User ❯ {auditLog.User.Mention}\nChannel ❯ {channel.Mention}\nReason ❯ {auditLog.Reason ?? "none"}";
                    bulkDelete = true;
                }
                else
                {
                    if (message.Author.IsBot) return;
                    description = string.Format(description, message.Author.Mention, message.Id,
                        auditLog.User.Mention, channel.Mention, message.Content, auditLog.Reason ?? "none");
                }
            }
            else
            {
                if (message.Author.IsBot) return;
                description = string.Format(description, message.Author.Mention, message.Id,
                    message.Author.Mention, channel.Mention, message.Content, "none");
            }


            await SendLog(channel.Guild, "Message Deleted", description);
        }

        public async Task GuildMemberUpdated(SocketGuildUser userBefore, SocketGuildUser userAfter)
        {
            if (userBefore.Status != userAfter.Status) return;
            if (!GetAuditLogEntry(userAfter.Guild, out var auditLog, Discord.ActionType.MemberUpdated)) return;

            var description =
                $"User ❯ {userAfter.Mention}\nResponsible User ❯ {auditLog.User.Mention}\nReason ❯ {auditLog.Reason ?? "none"}\nId ❯ {auditLog.Id}\nChanges ❯";
            var oldDescription = description;
            if ((userBefore.Nickname ?? userBefore.Username) != (userAfter.Nickname ?? userAfter.Username))
                description +=
                    $"\n❯ Nickname: {userBefore.Nickname ?? userBefore.Username} ⇒ {userAfter.Nickname ?? userAfter.Username}";

            var difference = userBefore.Roles.Except(userAfter.Roles);
            if (difference.Any())
                description = difference.Aggregate(description,
                    (current, role) => current + $"\nRemoved ❯ {role.Mention}");

            difference = userAfter.Roles.Except(userBefore.Roles);
            if (difference.Any())
                description =
                    difference.Aggregate(description, (current, role) => current + $"\nAdded ❯ {role.Mention}");

            if (description != oldDescription) await SendLog(userAfter.Guild, "Member Updated", description);
        }

        public async Task ChannelUpdated(SocketChannel arg1, SocketChannel arg2)
        {
            if (!(arg1 is IGuildChannel channelBefore) || !(arg2 is IGuildChannel channelAfter)) return;
            if (!GetAuditLogEntry(channelAfter.Guild, out var auditLog, Discord.ActionType.ChannelUpdated)) return;
            var description =
                string.Format("Channel ❯ {1}\nResponsible User ❯ {0}\nReason ❯ {1}\nId ❯ {2}\nChanges ❯",
                    auditLog.User.Mention, auditLog.Reason ?? "none", auditLog.Id);
            string oldDescription;
            Type T;
            object before;
            object after;
            switch (arg1)
            {
                case ITextChannel textChannelBefore when arg2 is ITextChannel textChannelAfter:
                    description = string.Format(description, textChannelAfter.Mention);
                    oldDescription = description;
                    T = typeof(SocketTextChannel);
                    before = textChannelBefore;
                    after = textChannelAfter;
                    break;
                case IVoiceChannel voiceChannelBefore when arg2 is IVoiceChannel voiceChannelAfter:
                    description = string.Format(description, voiceChannelAfter.Name);
                    oldDescription = description;
                    T = typeof(SocketVoiceChannel);
                    before = voiceChannelBefore;
                    after = voiceChannelAfter;
                    break;
                default:
                    throw new NotImplementedException();
            }

            var properties = T.GetProperties();
            foreach (var property in properties)
            {
                var propertyBefore = property.GetValue(before);
                var propertyAfter = property.GetValue(after);
                if (propertyBefore.ToString() != propertyAfter.ToString())
                    description += $"\n❯ {property.Name}: {propertyBefore} ⇒ {propertyAfter}";
            }

            if (description == oldDescription) return;
            await SendLog(channelAfter.Guild, "Channel Updated", description);
        }

        public async Task ChannelDestroyed(SocketChannel arg)
        {
            if (!(arg is IGuildChannel channel)) return;
            if (!GetAuditLogEntry(channel.Guild, out var auditLog, Discord.ActionType.ChannelDeleted)) return;
            var description =
                $"Channel ❯ {channel.Name}\nResponsible User ❯ {auditLog.User.Mention}\nReason ❯ {auditLog.Reason ?? "none"}\nId ❯ {auditLog.Id}";

            await SendLog(channel.Guild, "Channel Deleted", description);
        }

        public async Task ChannelCreated(SocketChannel arg)
        {
            if (!(arg is IGuildChannel channel)) return;
            if (!GetAuditLogEntry(channel.Guild, out var auditLog, Discord.ActionType.ChannelCreated)) return;
            var description =
                $"Channel ❯ {{0}}\nResponsible User ❯ {auditLog.User.Mention}\nReason ❯ {auditLog.Reason ?? "none"}\nId ❯ {auditLog.Id}";
            switch (arg)
            {
                case ITextChannel textChannel:
                    description = string.Format(description, textChannel.Mention);
                    break;
                case IVoiceChannel voiceChannel:
                    description = string.Format(description, voiceChannel.Name);
                    break;
            }

            await SendLog(channel.Guild, "Channel Created", description);
        }

        public async Task SendLog(IGuild guild, string title, string description, DateTimeOffset? timeStamp = null,
            Server server = null)
        {
            timeStamp = timeStamp ?? DateTimeOffset.Now;
            if (server == null) _database.Execute(x => { server = server ?? x.Load<Server>($"{guild.Id}"); });

            if (!server.LogChannelId.HasValue) return;

            var channel = await guild.GetTextChannelAsync(server.LogChannelId.Value);
            if (channel == null) return;
            var currentUser = await guild.GetCurrentUserAsync();
            if (!currentUser.GetPermissions(channel).SendMessages) return;

            var embed = UtilService.NormalizeEmbed(title, description, ColorType.Normal, _random, server);
            embed.WithTimestamp(timeStamp.Value);

            await channel.SendMessageAsync(embed: embed.Build());
        }

        private bool GetAuditLogEntry(IGuild guild, out IAuditLogEntry auditLog, Discord.ActionType type)
        {
            auditLog = guild.GetAuditLogsAsync(5).GetAwaiter().GetResult().FirstOrDefault(x => x.Action == type);
            if (auditLog == null) return false;
            var timeStamp = SnowflakeUtils.FromSnowflake(auditLog.Id);
            if (timeStamp > DateTimeOffset.Now - TimeSpan.FromSeconds(5)) return false;
            return auditLog.User.Id != _client.CurrentUser.Id;
        }
    }
}