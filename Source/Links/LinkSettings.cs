using Mikodev.Links.Internal;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links
{
    public sealed class LinkSettings
    {
        internal LinkEnvironment Environment { get; private set; }

        internal LinkSettings(LinkEnvironment environment)
        {
            Debug.Assert(environment != null);
            Environment = environment;
        }

        public static LinkSettings Create() => new LinkSettings(new LinkEnvironment());

        public static async Task<LinkSettings> CreateAsync(TextReader reader)
        {
            var buffer = new char[LinkEnvironment.SettingsMaximumCharacters];
            var length = await reader.ReadBlockAsync(buffer, 0, buffer.Length).TimeoutAfter(LinkEnvironment.SettingsIOTimeout);
            var text = new string(buffer, 0, length);
            var environment = new LinkEnvironment();
            environment.Load(text);
            return new LinkSettings(environment);
        }

        public async Task SaveAsync(TextWriter writer)
        {
            var item = Environment;
            var text = item.Save();
            await writer.WriteAsync(text).TimeoutAfter(LinkEnvironment.SettingsIOTimeout);
        }
    }
}
