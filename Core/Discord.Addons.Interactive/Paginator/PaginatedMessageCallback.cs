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

        private PaginatedAppearanceOptions options => _pager.Options;
        public SocketCommandContext Context { get; }

        public RunMode RunMode => RunMode.Sync;
        public ICriterion<SocketReaction> Criterion { get; }

        public TimeSpan? Timeout => options.Timeout;

        public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
        {
            var emote = reaction.Emote;

            if (emote.Equals(options.First))
            {
                page = 1;
            }
            else if (emote.Equals(options.Next))
            {
                if (page >= pages)
                    return false;
                ++page;
            }
            else if (emote.Equals(options.Back))
            {
                if (page <= 1)
                    return false;
                --page;
            }
            else if (emote.Equals(options.Last))
            {
                page = pages;
            }
            else if (emote.Equals(options.Stop))
            {
                await Message.DeleteAsync().ConfigureAwait(false);
                return true;
            }

            await RenderAsync().ConfigureAwait(false);
            return false;
        }

        public async Task DisplayAsync()
        {
            var embed = BuildEmbed();
            var message = await Context.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            Message = message;
            Interactive.AddReactionCallback(message, this);
            // Reactions take a while to add, don't wait for them
            _ = Task.Run(async () =>
            {
                await message.AddReactionAsync(options.First);
                await message.AddReactionAsync(options.Back);
                await message.AddReactionAsync(options.Stop);
                await message.AddReactionAsync(options.Next);
                await message.AddReactionAsync(options.Last);
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
                .WithFooter(string.Format(options.FooterFormat, page, pages))
                .Build();
        }

        private async Task RenderAsync()
        {
            var embed = BuildEmbed();
            await Message.ModifyAsync(m => m.Embed = embed).ConfigureAwait(false);
        }
    }
}