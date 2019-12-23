using Mikodev.Links.Abstractions;
using Mikodev.Links.Data.Abstractions;
using System.Threading.Tasks;

namespace Mikodev.Links
{
    public static class LinkFactory
    {
        public static Client CreateClient(ILinkSettings settings, ILinkUIContext context, ILinkDataStore store) => new LinkClient(settings, context, store);

        public static ILinkSettings CreateSettings() => LinkSettings.Create();

        public static async Task<ILinkSettings> CreateSettingsAsync(string file) => await LinkSettings.LoadAsync(file);
    }
}
