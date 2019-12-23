using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal
{
    internal sealed class Settings
    {
        internal Configurations Configurations { get; private set; }

        internal Settings(Configurations configurations)
        {
            Debug.Assert(configurations != null);
            Configurations = configurations;
        }

        public static Settings Create() => new Settings(new Configurations());

        public static async Task<Settings> LoadAsync(TextReader reader)
        {
            var buffer = new char[Configurations.SettingsMaximumCharacters];
            var length = await reader.ReadBlockAsync(buffer, 0, buffer.Length).TimeoutAfter(Configurations.SettingsIOTimeout);
            var text = new string(buffer, 0, length);
            var configurations = new Configurations();
            configurations.Load(text);
            return new Settings(configurations);
        }

        public static async Task<Settings> LoadAsync(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await LoadAsync(reader);
        }

        public async Task SaveAsync(TextWriter writer)
        {
            var item = Configurations;
            var text = item.Save();
            await writer.WriteAsync(text).TimeoutAfter(Configurations.SettingsIOTimeout);
        }

        public async Task SaveAsync(string path)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            await SaveAsync(writer);
        }
    }
}
