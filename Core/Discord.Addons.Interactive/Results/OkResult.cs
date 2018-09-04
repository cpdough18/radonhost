#region

using Discord.Commands;

#endregion

namespace Discord.Addons.Interactive
{
    public class OkResult : RuntimeResult
    {
        public OkResult(string reason = null) : base(null, reason)
        {
        }
    }
}