using Radon.Core;

namespace Radon
{
    class Program
    {
        static void Main(string[] args)
        {
            new DiscordBot().InitializeAsync().Wait();
        }
    }
}
