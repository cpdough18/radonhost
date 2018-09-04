#region

using System.Collections.Generic;
using Radon.Core;

#endregion

namespace Radon.Services.External
{
    public class CachingService
    {
        public readonly Dictionary<ulong, ExecutionObject> ExecutionObjects;

        public CachingService()
        {
            ExecutionObjects = new Dictionary<ulong, ExecutionObject>();
        }
    }
}