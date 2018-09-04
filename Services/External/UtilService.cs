#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Radon.Core;

#endregion

namespace Radon.Services.External
{
    public static class UtilService
    {
        public static Regex CodeBlockRegex;

        public static string GetName(this CommandInfo command)
        {
            return !string.IsNullOrWhiteSpace(command.Module.Group)
                ? $"{(string.IsNullOrWhiteSpace(command.Module.Name) ? null : $"{command.Module.Name} ")}{command.Name}"
                : $"{command.Name}";
        }

        public static string GetUsage(this IEnumerable<CommandInfo> commands, ShardedCommandContext context)
        {
            var overloadUsages = new List<string>();
            foreach (var command in commands)
            {
                var usage = command.GetName();
                foreach (var parameter in command.Parameters)
                    if (parameter.IsOptional)
                        usage += $" ({parameter.Name})";
                    else
                        usage += $" <{parameter.Name}>";
                overloadUsages.Add(usage);
            }

            return string.Join("\n", overloadUsages);
        }

        public static string GetUsage(this CommandInfo command, ShardedCommandContext context)
        {
            var usage = command.GetName();
            foreach (var parameter in command.Parameters)
                if (parameter.IsOptional)
                    usage += $" ({parameter.Name})";
                else
                    usage += $" <{parameter.Name}>";
            return usage;
        }

        public static string GetUsage(this ModuleInfo module, ShardedCommandContext context)
        {
            var overloadUsages = new List<string>();
            foreach (var command in module.Commands)
            {
                var usage = command.GetName();
                foreach (var parameter in command.Parameters)
                    if (parameter.IsOptional)
                        usage += $" ({parameter.Name})";
                    else
                        usage += $" <{parameter.Name}>";
                overloadUsages.Add(usage);
            }

            return string.Join("\n", overloadUsages);
        }


        public static string ToHastebin(this string code, HttpClient http = null)
        {
            CodeBlockRegex = CodeBlockRegex ?? new Regex(PublicVariables.CodeBlockRegex,
                                 RegexOptions.Compiled | RegexOptions.Multiline);
            http = http ?? new HttpClient();
            var codes = CodeBlockRegex.Matches(code);

            var data = JObject.Parse(http
                .PostAsync("https://hastebin.com/documents",
                    new StringContent(string.Join("\n\n\n", codes.Select(x => x.Groups[2])))).GetAwaiter().GetResult()
                .Content.ReadAsStringAsync().GetAwaiter().GetResult());
            return $"https://hastebin.com/{data["key"]}";
        }

        public static EmbedBuilder NormalizeEmbed(this EmbedBuilder embed,
            ColorType colorType, Random random, Server server, bool withRequested = false,
            ShardedCommandContext context = null)
        {
            if (withRequested && context != null)
                embed.WithFooter(
                    $"Requested by {(context.User as IGuildUser)?.Nickname ?? context.User.Username}#{context.User.Discriminator}",
                    context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl());
            embed.SetColor(colorType, server, random);
            if (withRequested)
                embed.WithFooter(
                    $"Requested by {(context.User as IGuildUser)?.Nickname ?? context.User.Username}#{context.User.Discriminator}",
                    context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl());
            return embed;
        }

        public static EmbedBuilder NormalizeEmbed(string title, string description, ColorType colorType, Random random,
            Server server, bool withRequested = false,
            ShardedCommandContext context = null)
        {
            var embed = new EmbedBuilder();
            if (withRequested && context != null)
                embed.WithFooter(
                    $"Requested by {(context.User as IGuildUser)?.Nickname ?? context.User.Username}#{context.User.Discriminator}",
                    context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl());
            embed.SetColor(colorType, server, random)
                .WithTitle(title)
                .WithDescription(description);
            if (withRequested)
                embed.WithFooter(
                    $"Requested by {(context.User as IGuildUser)?.Nickname ?? context.User.Username}#{context.User.Discriminator}",
                    context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl());
            return embed;
        }

        private static EmbedBuilder SetColor(this EmbedBuilder embed, ColorType colorType, Server server,
            Random random)
        {
            random = random ?? new Random();
            embed = embed ?? new EmbedBuilder();
            switch (colorType)
            {
                case ColorType.Random:
                    embed.WithColor(new Color(random.Next(255), random.Next(255), random.Next(255)));
                    break;
                case ColorType.Normal:
                    embed.WithColor(server?.DefaultColor ?? PublicVariables.DefaultColor);
                    break;
            }

            return embed;
        }

        public static bool GetSetting(this Server server, Setting setting)
        {
            return !server.DisabledSettings.Contains(setting);
        }

        public static bool GetChannelSettings(this Server server, ulong channelId, Setting setting)
        {
            if (server.DisabledChannelSettings.TryGetValue(channelId, out var disabled))
                return !disabled.Contains(setting);

            return server.GetSetting(setting);
        }

        public static string ToMessage(this string message, IUser user, IGuild guild)
        {
            var dictionary = new Dictionary<string, string>
            {
                {"%mention%", user.Mention},
                {"%user%", (user as IGuildUser)?.Nickname ?? user.Username},
                {"%server%", guild.Name}
            };

            foreach (var pair in dictionary)
                message = message.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);

            return message;
        }

        public static Dictionary<string, string> GetColors()
        {
            const string filePath = "colors.json";
            if (!File.Exists(filePath)) File.Create(filePath).Close();

            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(filePath));
            dictionary = new Dictionary<string, string>(dictionary, StringComparer.OrdinalIgnoreCase);
            return dictionary;
        }

        public static int CalculateDifference(this string first, string second)
        {
            var n = first.Length;
            var m = second.Length;
            var d = new int[n + 1, m + 1];

            if (n == 0) return m;

            if (m == 0) return n;

            for (var i = 0; i <= n; d[i, 0] = i++) ;
            for (var j = 0; j <= m; d[0, j] = j++) ;

            for (var i = 1; i <= n; i++)
                for (var j = 1; j <= m; j++)
                {
                    var cost = second[j - 1] == first[i - 1] ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }

            return d[n, m];
        }
    }
}