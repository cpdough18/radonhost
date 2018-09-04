#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Radon.Core;
using Radon.Services;
using Radon.Services.External;

#endregion

namespace Radon.Modules
{
    [CheckBotOwner]
    [CommandCategory(CommandCategory.BotOwner)]
    public class BotOwnerModule : CommandBase
    {
        private readonly IEnumerable<string> _dependencies = new[]
        {
            "Discord", "Discord.Net", "Discord.Commands", "Discord.WebSocket", "System", "System.Linq",
            "System.Collections.Generic", "System.Text", "System.Threading.Tasks"
        };

        [Command("eval")]
        [Summary("Evaluates some code and returns the result")]
        public async Task EvaluateAsync([Remainder] string code)
        {
            if (code.Contains("\\`")) 
                code = code.Replace("\\","`");
            if (Regex.IsMatch(code, PublicVariables.CodeBlockRegex, RegexOptions.Compiled | RegexOptions.Multiline))
                code =
                    $"{Regex.Match(code, PublicVariables.CodeBlockRegex, RegexOptions.Compiled | RegexOptions.Multiline).Groups[2]}";
            var assemblys = Assembly.GetEntryAssembly().GetReferencedAssemblies().Select(Assembly.Load).ToList();
            assemblys.Add(Assembly.GetEntryAssembly());
            var scriptOptions = ScriptOptions.Default
                .WithReferences(assemblys.Select(x => MetadataReference.CreateFromFile(x.Location)))
                .WithImports(Assembly.GetEntryAssembly().GetTypes().Select(x => x.Namespace).Distinct());
            var embed = NormalizeEmbed("Evaluate Code", "Debugging...");
            var message = await ReplyAsync(embed: embed.Build());
            try
            {
                var result = await CSharpScript.EvaluateAsync(
                    $"{string.Join("\n", _dependencies.Select(x => $"using {x};"))}\n{code}", scriptOptions,
                    new EvaluateObject
                    {
                        Client = Context.Client,
                        Context = Context,
                        Database = Database,
                        Random = Random,
                        Server = Server
                    }, typeof(EvaluateObject));
                embed.WithTitle("Completed")
                    .WithDescription($"Result: {result ?? "none"}");
                await message.ModifyAsync(x => x.Embed = embed.Build());
            }
            catch (Exception e)
            {
                embed.WithTitle("Failure")
                    .WithDescription($"Reason: {e.Message ?? e.InnerException.Message}");
                await message.ModifyAsync(x => x.Embed = embed.Build());
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}