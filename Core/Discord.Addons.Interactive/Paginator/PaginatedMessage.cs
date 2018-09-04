#region

using System.Collections.Generic;

#endregion

namespace Discord.Addons.Interactive
{
    public class PaginatedMessage
    {
        public IEnumerable<EmbedBuilder> Pages { get; set; }

        public PaginatedAppearanceOptions Options { get; set; } = PaginatedAppearanceOptions.Default;
    }
}