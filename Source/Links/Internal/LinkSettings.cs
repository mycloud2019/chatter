using Mikodev.Links.Abstractions;
using Mikodev.Links.Internal;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mikodev.Links
{
    internal sealed class LinkSettings : ISettings
    {
        internal LinkEnvironment Environment { get; private set; }

        internal LinkSettings(LinkEnvironment environment)
        {
            Debug.Assert(environment != null);
            Environment = environment;
        }

        public static LinkSettings Create() => new LinkSettings(new LinkEnvironment());

        public static async Task<LinkSettings> LoadAsync(TextReader reader)
        {
            var buffer = new char[LinkEnvironment.SettingsMaximumCharacters];
            var length = await reader.ReadBlockAsync(buffer, 0, buffer.Length).TimeoutAfter(LinkEnvironment.SettingsIOTimeout);
            var text = new string(buffer, 0, length);
            var environment = new LinkEnvironment();
            environment.Load(text);
            return new LinkSettings(environment);
        }

        public static async Task<LinkSettings> LoadAsync(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await LoadAsync(reader);
        }

        public async Task SaveAsync(TextWriter writer)
        {
            var item = Environment;
            var text = item.Save();
            await writer.WriteAsync(text).TimeoutAfter(LinkEnvironment.SettingsIOTimeout);
        }

        public async Task SaveAsync(string path)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            await SaveAsync(writer);
        }
    }
}
