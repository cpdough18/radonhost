#region

using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Radon.Services;
using Radon.Services.External;

#endregion

namespace Radon.Core
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckBotPermissionAttribute : PreconditionAttribute
    {
        public CheckBotPermissionAttribute(GuildPermission permission)
        {
            GuildPermission = permission;
            ChannelPermission = null;
        }

        public CheckBotPermissionAttribute(ChannelPermission permission)
        {
            ChannelPermission = permission;
            GuildPermission = null;
        }

        public GuildPermission? GuildPermission { get; }
        public ChannelPermission? ChannelPermission { get; }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            if (
                services.GetService<Configuration>().OwnerIds.Contains(context.User.Id))
                return PreconditionResult.FromSuccess();
            IGuildUser guildUser = null;
            if (context.Guild != null)
                guildUser = await context.Guild.GetCurrentUserAsync().ConfigureAwait(false);

            if (GuildPermission.HasValue)
            {
                if (guildUser == null)
                    return PreconditionResult.FromError(
                        $"This command can only be used in a {"server".InlineCode()} channel");
                if (!guildUser.GuildPermissions.Has(GuildPermission.Value))
                    return PreconditionResult.FromError(
                        $"I need the permission {GuildPermission.Value.Humanize(LetterCasing.Title).ToLower().InlineCode()} to do this");
            }

            if (!ChannelPermission.HasValue) return PreconditionResult.FromSuccess();
            ChannelPermissions perms;
            if (context.Channel is IGuildChannel guildChannel)
                perms = guildUser.GetPermissions(guildChannel);
            else
                perms = ChannelPermissions.All(context.Channel);

            return !perms.Has(ChannelPermission.Value)
                ? PreconditionResult.FromError(
                    $"I need the channel permission {ChannelPermission.Value.Humanize(LetterCasing.Title).ToLower().InlineCode()} to do this")
                : PreconditionResult.FromSuccess();
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckPermissionAttribute : PreconditionAttribute
    {
        public CheckPermissionAttribute(GuildPermission permission)
        {
            GuildPermission = permission;
            ChannelPermission = null;
        }

        public CheckPermissionAttribute(ChannelPermission permission)
        {
            ChannelPermission = permission;
            GuildPermission = null;
        }

        public GuildPermission? GuildPermission { get; }
        public ChannelPermission? ChannelPermission { get; }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (services.GetService<Configuration>().OwnerIds.Contains(context.User.Id))
                return Task.FromResult(PreconditionResult.FromSuccess());
            var guildUser = context.User as IGuildUser;

            if (GuildPermission.HasValue)
            {
                if (guildUser == null)
                    return Task.FromResult(
                        PreconditionResult.FromError(
                            $"This command is only available in {"server".InlineCode()} channels"));
                if (!guildUser.GuildPermissions.Has(GuildPermission.Value))
                    return Task.FromResult(
                        PreconditionResult.FromError(
                            $"You need the permission {GuildPermission.Value.Humanize(LetterCasing.Title).ToLower().InlineCode()} to do this"));
            }

            if (!ChannelPermission.HasValue) return Task.FromResult(PreconditionResult.FromSuccess());
            ChannelPermissions perms;
            if (context.Channel is IGuildChannel guildChannel)
                perms = guildUser.GetPermissions(guildChannel);
            else
                perms = ChannelPermissions.All(context.Channel);

            if (!perms.Has(ChannelPermission.Value))
                return Task.FromResult(PreconditionResult.FromError(
                    $"You need the channel permission {ChannelPermission.Value.Humanize(LetterCasing.Title).ToLower().InlineCode()} to do this"));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckServerAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            return Task.FromResult(context.Guild != null
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError(
                    $"This command is only available in {"server".InlineCode()} channels"));
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckNsfwAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (context.Channel is ITextChannel text && text.IsNsfw)
                return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(
                PreconditionResult.FromError($"This command is only aviable in {"nsfw".InlineCode()} channels"));
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckBotOwnerAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            switch (context.Client.TokenType)
            {
                case TokenType.Bot:
                    if (!services.GetService<Configuration>().OwnerIds.Contains(context.User.Id))
                        return Task.FromResult(PreconditionResult.FromError("You're not my dad!"));
                    return Task.FromResult(PreconditionResult.FromSuccess());
                default:
                    return Task.FromResult(PreconditionResult.FromError("An internal error has occurred"));
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckServerOwnerAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (services.GetService<Configuration>().OwnerIds.Contains(context.User.Id))
                return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(context.Guild == null
                ? PreconditionResult.FromError($"This command is only available in {"server".InlineCode()} channels")
                : ((IGuildUser)context.User).Guild.OwnerId == context.User.Id
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError(
                        $"You need to be the {"owner".InlineCode()} of this server to use this command"));
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class CheckBotHierarchyAttribute : ParameterPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter,
            object value, IServiceProvider services)
        {
            var currentUser = (context as SocketCommandContext)?.Guild.CurrentUser;
            switch (value)
            {
                case SocketGuildUser user when currentUser != null:
                    return Task.FromResult(user.Hierarchy <= currentUser.Hierarchy
                        ? PreconditionResult.FromSuccess()
                        : PreconditionResult.FromError("I don't have the correct permission to do this"));
                case SocketRole role when currentUser != null:
                    return Task.FromResult(role.Position <= currentUser.Hierarchy
                        ? PreconditionResult.FromSuccess()
                        : PreconditionResult.FromError("I don't have the correct permission to do this"));
            }

            throw new NotImplementedException(nameof(value));
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class CheckUserHierarchyAttribute : ParameterPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter,
            object value, IServiceProvider services)
        {
            if (services.GetService<Configuration>().OwnerIds.Contains(context.User.Id))
                return Task.FromResult(PreconditionResult.FromSuccess());
            var user = (context as SocketCommandContext)?.Guild.CurrentUser;
            switch (value)
            {
                case SocketGuildUser target when user != null:
                    return Task.FromResult(target.Hierarchy <= user.Hierarchy
                        ? PreconditionResult.FromSuccess()
                        : PreconditionResult.FromError("You don't have the correct permission to do this"));
                case SocketRole role when user != null:
                    return Task.FromResult(role.Position <= user.Hierarchy
                        ? PreconditionResult.FromSuccess()
                        : PreconditionResult.FromError("I don't have the correct permission to do this"));
            }

            throw new NotImplementedException(nameof(value));
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CheckState : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (services.GetService<Configuration>().OwnerIds.Contains(context.User.Id))
                return Task.FromResult(PreconditionResult.FromSuccess());
            var caching = services.GetService<CachingService>();
            if (caching.ExecutionObjects.TryGetValue(context.Message.Id, out var executionObj))
            {
                var category = command.Attributes.OfType<CommandCategoryAttribute>().FirstOrDefault()?.Category ??
                               command.Module.Attributes.OfType<CommandCategoryAttribute>().FirstOrDefault()?.Category;
                if (category.HasValue && executionObj.Server.DisabledCategories.Contains(category.Value))
                    return Task.FromResult(PreconditionResult.FromError("This command category is disabled"));
            }

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }

    public class CannotDisableAttribute : Attribute
    {
    }
}