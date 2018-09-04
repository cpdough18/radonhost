using Discord;
using Discord.Rest;
using System.Collections.Generic;

namespace Radon.Services.External
{
    public class PublicVariables
    {

        // Group 1: emoji name, Group 2: emoji id
        public const string EmojiRegex = @"^<a?:(\w+):(\d+)>$/";

        // Group 1: channel id
        public const string ChannelRegex = @"/^<#(\d+)>$/";

        // Group 1: user id
        public const string UserMentionRegex = @"/^<@!?(\d+)>$/";

        // Group 1: @everyone or @here, Group 2: everyone or here 
        public const string MassMentionRegex = @"/^(@(?:everyone|here))$/";

        // Group 1: role id
        public const string RoleMentionRegex = @"/^<@&(\d+)>$/";

        // Group 1: inner text
        public const string StrikeRegex = @"/~~(.+?)~~/";
        public const string UnderlineRegex = @"/__(.+?)__/";
        public const string BoldRegex = @"/\*\*(.+?)\*\*/";
        public const string Italics1Regex = @"/_(.+?)_/";
        public const string Italics2Regex = @"/\*(.+?)\*/";

        // Group 1: language, Group 2: content
        public const string CodeRegex = "/`(.+?)`/";
        public const string CodeBlockRegex = @"\`\`\`(?:(\S+?)[\n ])?\n*(?s:(.+?))\n*\`\`\`";

        public const string InviteRegex =
            @"(http|https)?(:)?(\/\/)?(discordapp|discord)..(gg|io|me|com)\/(\w+:{0,1}\w*@)?(\S+)(:[0-9]+)?(\/|\/([\w#!:.?+=&%@!-/]))?";

        public static readonly Color DefaultColor = Color.Blue;

        public static readonly Dictionary<string, string> Colors = UtilService.GetColors();

        public static RestApplication Application;
    }
}
