using System.Linq;
using Discord;
using Radon.Services.External;

namespace Radon.Services
{
    public class ServerService
    {
        private readonly DatabaseService _database;

        public ServerService(DatabaseService database)
        {
            _database = database;
        }

        public ModLogItem AddLogItem(IGuild guild, ActionType actionType, string reason, ulong responsible, ulong target)
        {
            Server server;
            var logItem = new ModLogItem
            {
                ActionType = actionType,
                Reason = reason,
                ResponsibleUserId = responsible,
                UserId = target
            };
            _database.Execute(x =>
            {
                server = x.Load<Server>($"{guild.Id}");
                logItem.LogId = server.ModLog.Any() ? server.ModLog.Keys.Max() + 1 : 0;
                server.ModLog[logItem.LogId] = logItem;
                x.Store(server, $"{guild.Id}");
                x.SaveChanges();
            });
            return logItem;
        }
        public ModLogItem AddLogItem(Server server, ActionType actionType, string reason, ulong responsible,
            ulong target)
        {
            var logItem = new ModLogItem
            {
                ActionType = actionType,
                Reason = reason,
                ResponsibleUserId = responsible,
                UserId = target
            };
            _database.Execute(x =>
            {
                server = x.Load<Server>($"{server.Id}");
                logItem.LogId = server.ModLog.Any() ? server.ModLog.Keys.Max() : 0;
                server.ModLog[logItem.LogId] = logItem;
                x.Store(server, $"{server.Id}");
                x.SaveChanges();
            });
            return logItem;
        }

    }
}
