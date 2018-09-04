using Radon.Core;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using System;
using System.Linq;

namespace Radon.Services.External
{
    public class DatabaseService
    {
        private const string DatabaseName = "DiscordBot";
        private readonly Configuration _configuration;
        private readonly Lazy<IDocumentStore> _store;

        public DatabaseService(Configuration configuration)
        {
            _store = new Lazy<IDocumentStore>(CreateStore);
            _configuration = configuration;
            if (Store.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 5)).All(x => x != DatabaseName))
                Store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(DatabaseName)));
            Store.AggressivelyCacheFor(TimeSpan.FromDays(7));
        }

        private IDocumentStore Store => _store.Value;

        public void Execute(Action<IDocumentSession> action)
        {
            using (var session = Store.OpenSession())
            {
                action.Invoke(session);
            }
        }

        private IDocumentStore CreateStore()
        {
            var store = new DocumentStore
            {
                Urls = new[] { _configuration.DatabaseConnectionString },
                Database = DatabaseName
            }.Initialize();
            return store;
        }
    }
}
