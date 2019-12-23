using Mikodev.Links.Abstractions;
using Mikodev.Links.Data.Abstractions;
using Mikodev.Links.Internal;
using System.Threading.Tasks;

namespace Mikodev.Links
{
    public static class LinkFactory
    {
        public static IClient CreateClient(ISettings settings, IDispatcher context, IStorage storage) => new Client(settings, context, storage);

        public static ISettings CreateSettings() => Settings.Create();

        public static async Task<ISettings> CreateSettingsAsync(string file) => await Settings.LoadAsync(file);
    }
}
