#region

using System;
using System.Collections.Generic;
using Discord;
using Newtonsoft.Json;
using Radon.Core;
using Radon.Services.External;

#endregion

namespace Radon.Services
{
    public class User
    {
        public string Username { get; set; }
        private string Id { get; set; }

        [JsonIgnore]
        public ulong UserId
        {
            get => ulong.Parse(Id);
            set => Id = $"{value}";
        }

        private string Biography { get; set; }

        [JsonIgnore]
        public string Bio
        {
            get => Biography ?? "None set";
            set => Biography = value;
        }
    }

    public class Server
    {
        public HashSet<ulong> Blacklist = new HashSet<ulong>();
        public BlockingType BlockingType = BlockingType.None;

        public HashSet<CommandCategory> DisabledCategories = new HashSet<CommandCategory>();

        public Dictionary<ulong, HashSet<Setting>> DisabledChannelSettings =
            new Dictionary<ulong, HashSet<Setting>>();

        public HashSet<Setting> DisabledSettings = new HashSet<Setting>();
        public HashSet<string> JoinMessages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> LeaveMessages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> LevelUpMessages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<ulong, ModLogItem> ModLog = new Dictionary<ulong, ModLogItem>();
        public HashSet<string> Prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, Tag> Tags = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<ulong, Userxp> Userxps = new Dictionary<ulong, Userxp>();
        public HashSet<ulong> Whitelist = new HashSet<ulong>();
        public string Name { get; set; }
        public string Id { get; private set; }
        private uint? _defaultColor { get; set; }

        [JsonIgnore]
        public Color DefaultColor
        {
            get => _defaultColor.HasValue ? new Color(_defaultColor.Value) : PublicVariables.DefaultColor;
            set => _defaultColor = value.RawValue;
        }

        [JsonIgnore]
        public ulong ServerId
        {
            get => ulong.Parse(Id);
            set => Id = $"{value}";
        }

        private string AnnounceChannel { get; set; }

        [JsonIgnore]
        public ulong? AnnounceChannelId
        {
            get => AnnounceChannel == null ? (ulong?)null : ulong.Parse(AnnounceChannel);
            set => AnnounceChannel = $"{value}";
        }

        private string Autorole { get; set; }

        [JsonIgnore]
        public ulong? AutoroleId
        {
            get => Autorole == null ? (ulong?)null : ulong.Parse(Autorole);
            set => Autorole = $"{value}";
        }

        private string LogChannel { get; set; }

        [JsonIgnore]
        public ulong? LogChannelId
        {
            get => LogChannel == null ? (ulong?)null : ulong.Parse(LogChannel);
            set => LogChannel = $"{value}";
        }
    }

    public class Userxp
    {
        private string Id { get; set; }

        [JsonIgnore]
        public ulong? UserId
        {
            get => Id == null ? (ulong?)null : ulong.Parse(Id);
            set => Id = $"{value}";
        }

        public ulong Level { get; set; }
        public ulong Xp { get; set; }
    }

    public class ModLogItem
    {
        private string Id { get; set; }

        [JsonIgnore]
        public ulong LogId
        {
            get => ulong.Parse(Id);
            set => Id = $"{value}";
        }

        private string ResponsibleUser { get; set; }

        [JsonIgnore]
        public ulong ResponsibleUserId
        {
            get => ulong.Parse(ResponsibleUser);
            set => ResponsibleUser = $"{value}";
        }

        private string User { get; set; }

        [JsonIgnore]
        public ulong UserId
        {
            get => ulong.Parse(User);
            set => User = $"{value}";
        }

        public string Reason { get; set; }
        public ActionType ActionType { get; set; }
    }

    public class Tag
    {
        public string Name { get; set; }
        public string Message { get; set; }
        private string Author { get; set; }

        public ulong AuthorId
        {
            get => ulong.Parse(Author);
            set => Author = $"{value}";
        }

        public DateTimeOffset TimeStamp { get; set; }
    }

    public class Warn
    {
        public string Reason { get; set; }
        private string Author { get; set; }

        public ulong AuthorId
        {
            get => ulong.Parse(Author);
            set => Author = $"{value}";
        }

        public int WarnId { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public enum ActionType
    {
        Kick,
        Ban,
        Mute,
        Clear,
        Bulk,
        Unban,
        Unmute
    }

    public enum BlockingType
    {
        Whitelist,
        Blacklist,
        None
    }
}