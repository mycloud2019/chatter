using Mikodev.Links.Abstractions;
using Mikodev.Links.Data.Abstractions;
using System.Threading.Tasks;

namespace Mikodev.Links
{
    public static class LinkFactory
    {
        public static IClient CreateClient(ISettings settings, IDispatcher context, IStorage storage) => new LinkClient(settings, context, storage);

        public static ISettings CreateSettings() => LinkSettings.Create();

        public static async Task<ISettings> CreateSettingsAsync(string file) => await LinkSettings.LoadAsync(file);
    }
}
