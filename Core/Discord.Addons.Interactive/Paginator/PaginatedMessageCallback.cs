#region

using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

#endregion

namespace Discord.Addons.Interactive
{
    public class PaginatedMessageCallback : IReactionCallback
    {
        private readonly PaginatedMessage _pager;
        private readonly int pages;
        private int page = 1;


        public PaginatedMessageCallback(InteractiveService interactive,
            SocketCommandContext sourceContext,
            PaginatedMessage pager,
            ICriterion<SocketReaction> criterion = null)
        {
            Interactive = interactive;
            Context = sourceContext;
            Criterion = criterion ?? new EmptyCriterion<SocketReaction>();
            _pager = pager;
            pages = _pager.Pages.Count();
        }

        public InteractiveService Interactive { get; }
        public IUserMessage Message { get; private set; }

        private PaginatedAppearanceOptions Options => _pager.Options;
        public SocketCommandContext Context { get; }

        public RunMode RunMode => RunMode.Sync;
        public ICriterion<SocketReaction> Criterion { get; }

        public TimeSpan? Timeout => Options.Timeout;

        public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
        {
            IEmote emote = reaction.Emote;

            if (emote.Equals(Options.First))
            {
                page = 1;
            }
            else if (emote.Equals(Options.Next))
            {
                if (page >= pages)
                    return false;
                ++page;
            }
            else if (emote.Equals(Options.Back))
            {
                if (page <= 1)
                    return false;
                --page;
            }
            else if (emote.Equals(Options.Last))
            {
                page = pages;
            }
            else if (emote.Equals(Options.Stop))
            {
                await Message.DeleteAsync().ConfigureAwait(false);
                return true;
            }

            await RenderAsync().ConfigureAwait(false);
            return false;
        }

        public async Task DisplayAsync()
        {
            Embed embed = BuildEmbed();
            Rest.RestUserMessage message = await Context.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            Message = message;
            Interactive.AddReactionCallback(message, this);
            // Reactions take a while to add, don't wait for them
            _ = Task.Run(async () =>
            {
                await message.AddReactionAsync(Options.First);
                await message.AddReactionAsync(Options.Back);
                await message.AddReactionAsync(Options.Stop);
                await message.AddReactionAsync(Options.Next);
                await message.AddReactionAsync(Options.Last);
            });
            if (Timeout != null)
                _ = Task.Delay(Timeout.Value).ContinueWith(_ =>
                {
                    Interactive.RemoveReactionCallback(message);
                    Message.DeleteAsync();
                });
        }

        private Embed BuildEmbed()
        {
            return _pager.Pages.ToArray()[page - 1]
                .WithFooter(string.Format(Options.FooterFormat, page, pages))
                .Build();
        }

        private async Task RenderAsync()
        {
            Embed embed = BuildEmbed();
            await Message.ModifyAsync(m => m.Embed = embed).ConfigureAwait(false);
        }
    }
}