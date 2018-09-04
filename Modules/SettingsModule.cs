#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Radon.Core;
using Radon.Services;
using Radon.Services.External;

#endregion

namespace Radon.Modules
{
    [CommandCategory(CommandCategory.Settings)]
    [CheckServer]
    public class SettingsModule : CommandBase
    {
        [Group("blocking")]
        [Summary("Lets you set the blocking type")]
        public class BlockingModule : CommandBase
        {
            [Command("")]
            [Priority(-1)]
            [Summary("Shows the current blocking type")]
            public async Task BlockingAsync()
            {
                await ReplyEmbedAsync("Blocking Type",
                    $"The blocking type is {$"{Server.BlockingType}".ToLower().InlineCode()}");
            }

            [Command("whitelist")]
            [Alias("wl")]
            [Summary("Shows the current blocking type to whitelist")]
            public async Task WhitelistAsync()
            {
                Server.BlockingType = BlockingType.Whitelist;

                await ReplyEmbedAsync("Blocking Type",
                    $"Set the blocking type to {$"{BlockingType.Whitelist}".ToLower().InlineCode()}");
            }

            [Command("blacklist")]
            [Alias("bl")]
            [Summary("Shows the current blocking type to blacklist")]
            public async Task BlacklistAsync()
            {
                Server.BlockingType = BlockingType.Blacklist;

                await ReplyEmbedAsync("Blocking Type",
                    $"Set the blocking type to {$"{BlockingType.Blacklist}".ToLower().InlineCode()}");
            }

            [Command("none")]
            [Alias("none")]
            [Summary("Shows the current blocking type to none")]
            public async Task NoneAsync()
            {
                Server.BlockingType = BlockingType.Whitelist;

                await ReplyEmbedAsync("Blocking Type",
                    $"Set the blocking type to {$"{BlockingType.None}".ToLower().InlineCode()}");
            }
        }

        [Group("blacklist")]
        [Alias("bl")]
        [CommandCategory(CommandCategory.Settings)]
        [Summary("Lets you edit the blacklist")]
        [CheckServer]
        public class BlacklistModule : CommandBase
        {
            [Command("")]
            [Summary("Displays all channels in the blacklist")]
            [Priority(-1)]
            public async Task BlacklistAsync()
            {
                await ReplyEmbedAsync("Blacklist",
                    Server.Blacklist.Any()
                        ? $"The blacklist contains {string.Join(", ", Server.Blacklist.Select(x => Context.Guild.GetTextChannel(x).Mention))}"
                        : "The blacklist is empty");
            }

            [Command("add")]
            [Alias("a")]
            [Summary("Adds a channel to the blacklist")]
            [CheckPermission(GuildPermission.ManageChannels)]
            public async Task AddBlacklistAsync(params ITextChannel[] channels)
            {
                var channelCount = channels.Sum(channel => (!Server.Blacklist.Add(channel.Id) ? 0 : 1));

                await ReplyEmbedAsync("Blacklist",
                    $"Added {channelCount} {(channelCount == 1 ? "channel" : "channels")} to the blacklist");
            }

            [Command("remove")]
            [Alias("r")]
            [Summary("Removes a channel from the blacklist")]
            [CheckPermission(GuildPermission.ManageChannels)]
            public async Task RemoveBlacklistAsync(params ITextChannel[] channels)
            {
                var channelIds = channels.Select(x => x.Id);
                var channelCount = Server.Blacklist.RemoveWhere(x => channelIds.Contains(x));

                await ReplyEmbedAsync("Blacklist",
                    $"Removed {channelCount} {(channelCount == 1 ? "channel" : "channels")} from the blacklist");
            }
        }

        [Group("whitelist")]
        [Alias("wl")]
        [CommandCategory(CommandCategory.Settings)]
        [Summary("Lets you edit the whitelist")]
        [CheckServer]
        public class WhitelistModule : CommandBase
        {
            [Command("")]
            [Priority(-1)]
            public async Task WhitelistAsync()
            {
                await ReplyEmbedAsync("Whitelist",
                    Server.Blacklist.Any()
                        ? $"The white contains {string.Join(", ", Server.Whitelist.Select(x => Context.Guild.GetTextChannel(x).Mention))}"
                        : "The whitelist is empty");
            }

            [Command("add")]
            [Alias("a")]
            [CheckPermission(GuildPermission.ManageChannels)]
            [Summary("Adds a channel from the whitelist")]
            public async Task AddWhitelistAsync(params ITextChannel[] channels)
            {
                var channelCount = channels.Sum(channel => (!Server.Whitelist.Add(channel.Id) ? 0 : 1));

                await ReplyEmbedAsync("Whitelist",
                    $"Added {channelCount} {(channelCount == 1 ? "channel" : "channels")} to the whitelist");
            }

            [Command("remove")]
            [Alias("r")]
            [CheckPermission(GuildPermission.ManageChannels)]
            [Summary("Removes a channel from the whitelist")]
            public async Task RemoveWhitelistAsync(params ITextChannel[] channels)
            {
                var channelIds = channels.Select(x => x.Id);
                var channelCount = Server.Whitelist.RemoveWhere(x => channelIds.Contains(x));

                await ReplyEmbedAsync("Whitelist",
                    $"Removed {channelCount} {(channelCount == 1 ? "channel" : "channels")} from the whitelist");
            }
        }

        [Group("category")]
        [Alias("categorys")]
        [CommandCategory(CommandCategory.Settings)]
        [CheckServer]
        [Summary("Lets you disable or enable categorys")]
        public class CategoryModule : CommandBase
        {
            [Command("")]
            [Priority(-1)]
            [Summary("Shows all enabled/disabled categorys")]
            public async Task Categorys()
            {
                var categorys = Enum.GetValues(typeof(CommandCategory)).Cast<CommandCategory>();
                await ReplyEmbedAsync("Categorys",
                    $"Enabled: {string.Join(", ", categorys.Except(Server.DisabledCategories).Select(x => $"{x}".InlineCode()))}" +
                    $"\nDisabled: {string.Join(", ", Server.DisabledCategories.Select(x => $"{x}".InlineCode()))}");
            }

            [CheckPermission(GuildPermission.ManageGuild)]
            [Command("enable")]
            [Summary("Enables a category")]
            public async Task EnableAsync([Remainder] string category)
            {
                if (Enum.TryParse(typeof(CommandCategory), category, true, out var specificObj))
                {
                    var specificCategory = (CommandCategory)specificObj;
                    if (Server.DisabledCategories.Contains(specificCategory))
                    {
                        Server.DisabledCategories.Remove(specificCategory);
                        await ReplyEmbedAsync("Category Enabled",
                            $"Enabled the category {$"{specificCategory}".ToLower().InlineCode()}");
                    }
                    else
                    {
                        await ReplyEmbedAsync("Already Enabled",
                            $"{$"{specificCategory}".InlineCode()} is already enabled");
                    }
                }
                else
                {
                    await ReplyEmbedAsync("Unknown Category",
                        $"Aviable categorys: {string.Join(", ", Enum.GetValues(typeof(CommandCategory)).Cast<CommandCategory>().Select(x => $"{x}".ToLower().InlineCode()))}");
                }
            }

            [CheckPermission(GuildPermission.ManageGuild)]
            [Command("disable")]
            [Summary("Disables a category")]
            public async Task DisableAsync([Remainder] string category)
            {
                if (Enum.TryParse(typeof(CommandCategory), category, true, out var specificObj))
                {
                    var specificCategory = (CommandCategory)specificObj;
                    if (typeof(CommandCategory).GetMember($"{specificCategory}")[0]
                        .GetCustomAttributes(typeof(CannotDisableAttribute), true).Any())
                    {
                        await ReplyEmbedAsync("Cannot Disable",
                            $"You cannot disable the category {$"{specificCategory}".ToLower()}");
                        return;
                    }

                    if (Server.DisabledCategories.Contains(specificCategory))
                    {
                        await ReplyEmbedAsync("Already Disabled",
                            $"{$"{specificCategory}".InlineCode()} is already disabled");
                    }
                    else
                    {
                        Server.DisabledCategories.Add(specificCategory);
                        await ReplyEmbedAsync("Category Disabled",
                            $"Disabled the category {$"{specificCategory}".ToLower().InlineCode()}");
                    }
                }
                else
                {
                    await ReplyEmbedAsync("Unknown Category",
                        $"Aviable categorys: {string.Join(", ", Enum.GetValues(typeof(CommandCategory)).Cast<CommandCategory>().Select(x => $"{x}".ToLower().InlineCode()))}");
                }
            }
        }

        [Group("prefix")]
        [CommandCategory(CommandCategory.Settings)]
        [CheckServer]
        [Summary("Lets you add/remove custom prefixes")]
        public class PrefixModule : CommandBase
        {
            [Command("")]
            [Priority(-1)]
            public async Task PrefixAsync()
            {
                await ReplyEmbedAsync("Custom Prefixes",
                    Server.Prefixes.Any()
                        ? $"The custom prefixes are {string.Join(", ", Server.Prefixes.Select(x => x.InlineCode()))}"
                        : "There are no custom prefixes for this server");
            }

            [Command("add")]
            [Alias("a")]
            [CheckPermission(GuildPermission.ManageGuild)]
            [Summary("Adds a prefix")]
            public async Task AddPrefixAsync([Remainder] string prefix)
            {
                if (Server.Prefixes.Add(prefix))
                    await ReplyEmbedAsync("Prefix Added", $"Added the prefix {prefix.InlineCode()}");
                else
                    await ReplyEmbedAsync("Already Existing", $"The prefix {prefix.InlineCode()} already exists");
            }

            [Command("remove")]
            [Alias("r")]
            [CheckPermission(GuildPermission.ManageGuild)]
            [Summary("Removes a prefix")]
            public async Task RemovePrefixAsync([Remainder] string prefix)
            {
                if (Server.Prefixes.Remove(prefix))
                    await ReplyEmbedAsync("Prefix Removed", $"Removed the prefix {prefix.InlineCode()}");
                else
                    await ReplyEmbedAsync("Unknown Prefix", $"The prefix {prefix.InlineCode()} doesn't exist");
            }

            [Command("clear")]
            [Alias("c")]
            [CheckPermission(GuildPermission.ManageGuild)]
            [Summary("Removes all prefixes")]
            public async Task ClearPrefixesAsync()
            {
                Server.Prefixes.Clear();
                await ReplyEmbedAsync("Prefixes Cleared", "Removed all custom prefixes");
            }
        }

        [Group("joinmessage")]
        [CommandCategory(CommandCategory.Settings)]
        [CheckServer]
        [Summary("Lets you set custom joinmessages")]
        public class JoinMessageModule : CommandBase
        {
            [Command("")]
            [Priority(-1)]
            [Summary("Shows all joinmessages")]
            public async Task JoinMessageAsync()
            {
                if (!Server.JoinMessages.Any())
                {
                    await ReplyEmbedAsync("Joinmessages", "There are no custom messages");
                    return;
                }

                var messages = Server.JoinMessages.Select((t, i) => Server.JoinMessages.ToList()[i])
                    .Select((item, i) => $"{i + 1}. {item.InlineCode()}").ToList();

                await ReplyEmbedAsync("Joinmessages", $"The joinmessages are\n{string.Join("\n", messages)}");
            }

            [Command("add")]
            [Alias("a")]
            [Summary("Adds a joinmessage")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task AddJoinMessageAsync([Remainder] string message)
            {
                if (Server.JoinMessages.Add(message))
                    await ReplyEmbedAsync("Joinmessage Added", $"Added the joinmessage {message.InlineCode()}");
                else
                    await ReplyEmbedAsync("Joinmessage Exists", "This joinmessage already exists");
            }

            [Command("remove")]
            [Alias("r")]
            [Summary("Removes a joinmessage")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task AddJoinMessageAsync(int id)
            {
                if (!Server.JoinMessages.Any())
                {
                    await ReplyEmbedAsync("No Joinmessages", "There are no custom joinmessages");
                    return;
                }

                if (Server.JoinMessages.Count < id || id < 1)
                {
                    if (Server.JoinMessages.Count == 1)
                        await ReplyEmbedAsync("Message Not Found",
                            $"You can only select the number {"1".InlineCode()}");
                    else
                        await ReplyEmbedAsync("Message Not Found",
                            $"You have to select a number between {"1".InlineCode()} and {$"{Server.JoinMessages.Count}".InlineCode()}");
                }
                else
                {
                    var message = Server.JoinMessages.ToList()[id - 1];
                    Server.JoinMessages.Remove(message);
                    await ReplyEmbedAsync("Joinmessage Removed", $"Removed the joinmessage {message.InlineCode()}");
                }
            }

            [Command("clear")]
            [Alias("c")]
            [Summary("Removes all joinmessages")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task ClearJoinMessageAsync()
            {
                Server.JoinMessages.Clear();
                await ReplyEmbedAsync("Joinmessages Cleared", "Removed all custom joinmessages");
            }
        }

        [Group("leavemessage")]
        [CommandCategory(CommandCategory.Settings)]
        [CheckServer]
        [Summary("Lets you set custom leavemessages")]
        public class LeaveMessageModule : CommandBase
        {
            [Command("")]
            [Summary("Shows all leavemessages")]
            [Priority(-1)]
            public async Task LeaveMessageAsync()
            {
                if (!Server.LeaveMessages.Any())
                {
                    await ReplyEmbedAsync("Leavemessages", "There are no custom messages");
                    return;
                }

                var messages = Server.LeaveMessages.Select((t, i) => Server.LeaveMessages.ToList()[i])
                    .Select((item, i) => $"{i + 1}. {item.InlineCode()}").ToList();

                await ReplyEmbedAsync("Leavemessages", $"The leavemessages are\n{string.Join("\n", messages)}");
            }

            [Command("add")]
            [Alias("a")]
            [Summary("Adds a leavemessage")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task AddLeaveMessageAsync([Remainder] string message)
            {
                if (Server.LeaveMessages.Add(message))
                    await ReplyEmbedAsync("Leavemessage Added", $"Added the leavemessage {message.InlineCode()}");
                else
                    await ReplyEmbedAsync("Leavemessage Exists", "This leavemessage already exists");
            }

            [Command("remove")]
            [Alias("r")]
            [Summary("Removes a leavemessage")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task AddLeaveMessageAsync(int id)
            {
                if (!Server.LeaveMessages.Any())
                {
                    await ReplyEmbedAsync("No Leavemessages", "There are no custom leavemessages");
                    return;
                }

                if (Server.LeaveMessages.Count < id || id < 1)
                {
                    if (Server.LeaveMessages.Count == 1)
                        await ReplyEmbedAsync("Message Not Found",
                            $"You can only select the number {"1".InlineCode()}");
                    else
                        await ReplyEmbedAsync("Message Not Found",
                            $"You have to select a number between {"1".InlineCode()} and {$"{Server.LeaveMessages.Count}".InlineCode()}");
                }
                else
                {
                    var message = Server.LeaveMessages.ToList()[id - 1];
                    Server.LeaveMessages.Remove(message);
                    await ReplyEmbedAsync("Leavemessage Removed", $"Removed the leavemessage {message.InlineCode()}");
                }
            }

            [Command("clear")]
            [Alias("c")]
            [Summary("Removes all leavemessages")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task ClearLeaveMessageAsync()
            {
                Server.LeaveMessages.Clear();
                await ReplyEmbedAsync("Leavemessages Cleared", "Removed all custom leavemessages");
            }
        }

        [Group("color")]
        [CommandCategory(CommandCategory.Settings)]
        [CheckServer]
        [Summary("Lets you set custom message color")]
        public class ColorModule : CommandBase
        {
            [Command("")]
            [Summary("Shows you the current color")]
            [Priority(-1)]
            public async Task ColorAsync()
            {
                await ReplyEmbedAsync("Color",
                    $"The custom color is {Server.DefaultColor}");
            }

            [Command("")]
            [Summary("Lets you set a custom color")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task SetColorAsync([Remainder] string color)
            {
                if (PublicVariables.Colors.TryGetValue(color, out var hex))
                {
                    var specificColor = new Color(uint.Parse(hex, NumberStyles.HexNumber));
                    Server.DefaultColor = specificColor;
                    await ReplyEmbedAsync("Color Set", $"Set the custom color to {$"{color}".ToLower().InlineCode()}");
                }
                else
                {
                    await ReplyEmbedAsync("Color Not Found",
                        $"Aviable colors: {string.Join(", ", PublicVariables.Colors.Select(x => $"{x.Key}".InlineCode()))}");
                }
            }

            [Command("")]
            [Summary("Lets you set a custom color")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task SetColorAsync(int r, int g, int b)
            {
                if (new[] { r, g, b }.Any(x => x > 255 || x < 0))
                {
                    await ReplyEmbedAsync("Wrong Format",
                        $"Every value needs to be between {"0".InlineCode()} and {"255".InlineCode()}");
                    return;
                }

                Server.DefaultColor = new Color(r, g, b);

                await ReplyEmbedAsync("Color Set",
                    $"Set the custom color to RGB({$"{r}".InlineCode()}, {$"{g}".InlineCode()}, {$"{b}".InlineCode()})");
            }
        }

        [Group("logchannel")]
        [Alias("lc")]
        [CommandCategory(CommandCategory.Settings)]
        [CheckServer]
        [Summary("Lets you set the logchannel")]
        public class LogChannelModule : CommandBase
        {
            [Command("")]
            [Priority(-1)]
            public async Task LogChannelAsync()
            {
                var channel = Context.Guild.GetTextChannel(Server.LogChannelId.GetValueOrDefault());
                await ReplyEmbedAsync("Logchannel",
                    $"The logchannel is {(channel == null ? "not set" : $"{channel.Mention}")}");
            }

            [Command("")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task SetLogChannelAsync(ITextChannel channel)
            {
                Server.LogChannelId = channel.Id;
                await ReplyEmbedAsync("Logchannel Set", $"Set the logchannel to {channel.Mention}");
            }

            [Command("remove")]
            [Alias("r", "delete", "d")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task RemoveLogChannelAsync()
            {
                Server.LogChannelId = null;
                await ReplyEmbedAsync("Logchannel Removed", "Removed the logchannel");
            }
        }

        [Group("announcechannel")]
        [Alias("ac")]
        [CommandCategory(CommandCategory.Settings)]
        [CheckServer]
        [Summary("Lets you set the announcechannel")]
        public class AnnounceChannelModule : CommandBase
        {
            [Command("")]
            [Priority(-1)]
            public async Task AnnouceChannelAsync()
            {
                var channel = Context.Guild.GetTextChannel(Server.AnnounceChannelId.GetValueOrDefault());
                await ReplyEmbedAsync("Announcechannel",
                    $"The announcechannel is {(channel == null ? "not set" : $"{channel.Mention}")}");
            }

            [Command("")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task SetAnnounceChannelAsync(ITextChannel channel)
            {
                Server.AnnounceChannelId = channel.Id;
                await ReplyEmbedAsync("Announcechannel Set", $"Set the announcechannel to {channel.Mention}");
            }

            [Command("remove")]
            [Alias("r", "delete", "d")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task RemoveAnnounceChannelAsync()
            {
                Server.AnnounceChannelId = null;
                await ReplyEmbedAsync("Announcechannel Removed", "Removed the announcechannel");
            }
        }

        [Group("autorole")]
        [Alias("ar")]
        [CommandCategory(CommandCategory.Settings)]
        [CheckServer]
        [Summary("Lets you set the autorole (every joining user gets this role")]
        public class AutoRoleModule : CommandBase
        {
            [Command("")]
            [Priority(-1)]
            public async Task AutoRoleAsync()
            {
                var role = Context.Guild.GetRole(Server.AutoroleId.GetValueOrDefault());
                await ReplyEmbedAsync("Autorole", $"The autorole is {(role == null ? "not set" : $"{role.Mention}")}");
            }

            [Command("")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task AutoRoleAsync(string role)
            {
                var specificRole = Context.Guild.Roles.FirstOrDefault(x =>
                    string.Equals(x.Name, role, StringComparison.OrdinalIgnoreCase));
                if (specificRole == null)
                {
                    await ReplyEmbedAsync("Role Not Found", $"Couldn't find a role with the name {role.InlineCode()}");
                }
                else
                {
                    if (specificRole.Position >= Context.Guild.CurrentUser.Hierarchy)
                    {
                        await ReplyEmbedAsync("Missing Permissions",
                            "I have not enough permissions to assign this role");
                    }
                    else if (specificRole.Position >= (Context.User as SocketGuildUser).Hierarchy)
                    {
                        await ReplyEmbedAsync("Missing Permissions",
                            "You have not enough permissions to assign this role");
                    }
                    else
                    {
                        Server.AutoroleId = specificRole.Id;
                        await ReplyEmbedAsync("Autorole Set", $"Set the autorole to {specificRole.Mention}");
                    }
                }
            }

            [Command("remove")]
            [Alias("r", "delete", "d")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task RemoveAutoRoleAsync()
            {
                Server.AutoroleId = null;
                await ReplyEmbedAsync("Autorole Removed", "Removed the autorole");
            }
        }

        [Group("settings")]
        [Alias("setting", "s")]
        [CommandCategory(CommandCategory.Settings)]
        [CheckServer]
        [Summary("Lets you edit the settings")]
        public class ServerSettingsModule : CommandBase
        {
            [Command("")]
            [Priority(-1)]
            public async Task SettingsAsync()
            {
                var settings = Enum.GetValues(typeof(Setting)).Cast<Setting>();
                await ReplyEmbedAsync("Settings",
                    $"Enabled: {string.Join(", ", settings.Except(Server.DisabledSettings).Select(x => $"{x}".ToLower().InlineCode()))}\nDisabled: {string.Join(", ", Server.DisabledSettings.Select(x => $"{x}".ToLower().InlineCode()))}");
            }

            [Command("enable")]
            [Alias("e")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task EnableSettingAsync([Remainder] string setting)
            {
                if (Enum.TryParse(typeof(Setting), setting, true, out var specificObject))
                {
                    var specificSetting = (Setting)specificObject;
                    if (Server.DisabledSettings.Remove(specificSetting))
                        await ReplyEmbedAsync("Setting Enabled",
                            $"Enabled the setting {$"{specificSetting}".ToLower().InlineCode()}");
                    else
                        await ReplyEmbedAsync("Already Enabled",
                            $"The setting {$"{specificSetting}".ToLower().InlineCode()} is already enabled");
                }
                else
                {
                    await ReplyEmbedAsync("Setting Not Found",
                        $"Aviable settings: {string.Join(", ", Enum.GetValues(typeof(Setting)).Cast<Setting>().Select(x => $"{x}".ToLower().InlineCode()))}");
                }
            }

            [Command("disable")]
            [Alias("d")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task DisableSettingAsync([Remainder] string setting)
            {
                if (Enum.TryParse(typeof(Setting), setting, true, out var specificObject))
                {
                    var specificSetting = (Setting)specificObject;
                    if (Server.DisabledSettings.Add(specificSetting))
                        await ReplyEmbedAsync("Setting Disabled",
                            $"Disabled the setting {$"{specificSetting}".ToLower().InlineCode()}");
                    else
                        await ReplyEmbedAsync("Already Disabled",
                            $"The setting {$"{specificSetting}".ToLower().InlineCode()} is already disabled");
                }
                else
                {
                    await ReplyEmbedAsync("Setting Not Found",
                        $"Aviable settings: {string.Join(", ", Enum.GetValues(typeof(Setting)).Cast<Setting>().Select(x => $"{x}".ToLower().InlineCode()))}");
                }
            }
        }

        [Group("channelsettings")]
        [Alias("channelsetting", "cs")]
        [CommandCategory(CommandCategory.Settings)]
        [CheckServer]
        [Summary("Lets you edit the settings of the current channel")]
        public class ChannelSettingsModule : CommandBase
        {
            [Command("")]
            [Priority(-1)]
            public async Task SettingsAsync()
            {
                if (!Server.DisabledChannelSettings.ContainsKey(Context.Channel.Id))
                    Server.DisabledChannelSettings.Add(Context.Channel.Id, new HashSet<Setting>());
                var channelSettings = Server.DisabledChannelSettings[Context.Channel.Id];
                var settings = Enum.GetValues(typeof(Setting)).Cast<Setting>();
                await ReplyEmbedAsync("Settings",
                    $"Enabled: {string.Join(", ", settings.Except(channelSettings).Select(x => $"{x}".ToLower().InlineCode()))}\nDisabled: {string.Join(", ", channelSettings.Select(x => $"{x}".ToLower().InlineCode()))}");
            }

            [Command("enable")]
            [Alias("e")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task EnableSettingAsync([Remainder] string setting)
            {
                if (Enum.TryParse(typeof(Setting), setting, true, out var specificObject))
                {
                    var specificSetting = (Setting)specificObject;

                    var channelSettings = Server.DisabledChannelSettings[Context.Channel.Id];
                    if (channelSettings.Remove(specificSetting))
                    {
                        Server.DisabledChannelSettings[Context.Channel.Id] = channelSettings;
                        await ReplyEmbedAsync("Setting Enabled",
                            $"Enabled the setting {$"{specificSetting}".ToLower().InlineCode()}");
                    }
                    else
                    {
                        await ReplyEmbedAsync("Already Enabled",
                            $"The setting {$"{specificSetting}".ToLower().InlineCode()} is already enabled");
                    }
                }
                else
                {
                    await ReplyEmbedAsync("Setting Not Found",
                        $"Aviable settings: {string.Join(", ", Enum.GetValues(typeof(Setting)).Cast<Setting>().Select(x => $"{x}".ToLower().InlineCode()))}");
                }
            }

            [Command("disable")]
            [Alias("d")]
            [CheckPermission(GuildPermission.ManageGuild)]
            public async Task DisableSettingAsync([Remainder] string setting)
            {
                if (Enum.TryParse(typeof(Setting), setting, true, out var specificObject))
                {
                    var specificSetting = (Setting)specificObject;

                    var channelSettings = Server.DisabledChannelSettings[Context.Channel.Id];
                    if (channelSettings.Add(specificSetting))
                    {
                        Server.DisabledChannelSettings[Context.Channel.Id] = channelSettings;
                        await ReplyEmbedAsync("Setting Disabled",
                            $"Disabled the setting {$"{specificSetting}".ToLower().InlineCode()}");
                    }
                    else
                    {
                        await ReplyEmbedAsync("Already Disabled",
                            $"The setting {$"{specificSetting}".ToLower().InlineCode()} is already disabled");
                    }
                }
                else
                {
                    await ReplyEmbedAsync("Setting Not Found",
                        $"Aviable settings: {string.Join(", ", Enum.GetValues(typeof(Setting)).Cast<Setting>().Select(x => $"{x}".ToLower().InlineCode()))}");
                }
            }
        }
    }
}