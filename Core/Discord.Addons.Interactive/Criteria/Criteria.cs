#region

using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

#endregion

namespace Discord.Addons.Interactive
{
    public class Criteria<T> : ICriterion<T>
    {
        private readonly List<ICriterion<T>> _critiera = new List<ICriterion<T>>();

        public async Task<bool> JudgeAsync(SocketCommandContext sourceContext, T parameter)
        {
            foreach (ICriterion<T> criterion in _critiera)
            {
                bool result = await criterion.JudgeAsync(sourceContext, parameter).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
            }

            return true;
        }

        public Criteria<T> AddCriterion(ICriterion<T> criterion)
        {
            _critiera.Add(criterion);
            return this;
        }
    }
}