using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Internal.Implementations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal.Sharing
{
    internal abstract class DirectoryObject : SharingObject
    {
        protected DirectoryObject(IClient client, Stream stream, NotifyDirectorySharingViewer viewer) : base(client, stream, viewer) { }

        protected async Task PutDirectoryAsync(string path)
        {
            async Task PutAsync(DirectoryInfo directoryInfo, ImmutableList<string> relative)
            {
                var head = Generator.Encode(new { type = "directory", path = relative });
                await Stream.WriteWithHeaderAsync(head, CancellationToken);

                foreach (var file in directoryInfo.GetFiles())
                {
                    var length = file.Length;
                    var data = Generator.Encode(new { type = "file", name = file.Name, length });
                    await Stream.WriteWithHeaderAsync(data, CancellationToken);
                    await PutFileAsync(file.FullName, length);
                }

                foreach (var directory in directoryInfo.GetDirectories())
                {
                    await PutAsync(directory, relative.Add(directory.Name));
                }
            }

            await PutAsync(new DirectoryInfo(path), ImmutableList<string>.Empty);
            var tail = Generator.Encode(new { type = "end" });
            await Stream.WriteWithHeaderAsync(tail, CancellationToken);
        }

        protected async Task GetDirectoryAsync(string path)
        {
            var top = Path.GetFullPath(path);
            var current = top;

            while (true)
            {
                var data = await Stream.ReadBlockWithHeaderAsync(Settings.TcpBufferLimits, CancellationToken);
                var token = new Token(Generator, data);
                var type = token["type"].As<string>();

                switch (type)
                {
                    case "directory":
                        var relative = token["path"].As<List<string>>();
                        relative.Insert(0, top);
                        current = Path.Combine(relative.ToArray());
                        _ = Directory.CreateDirectory(current);
                        break;

                    case "file":
                        var name = token["name"].As<string>();
                        var length = token["length"].As<long>();
                        var fullName = Path.Combine(current, name);
                        await GetFileAsync(fullName, length);
                        break;

                    case "end":
                        return;

                    default:
                        throw new NetworkException(NetworkError.InvalidData);
                }
            }
        }
    }
}
