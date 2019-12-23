using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Abstractions.Models;
using Mikodev.Links.Internal.Sharing;
using Mikodev.Optional;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal
{
    internal partial class Client
    {
        public event SharingHandler<ISharingFileReceiver> NewFileReceiver;

        public event SharingHandler<ISharingDirectoryReceiver> NewDirectoryReceiver;

        private void Initial(INetwork network)
        {
            network.RegisterHandler("link.share.file", HandleFileAsync);
            network.RegisterHandler("link.share.directory", HandleDirectoryAsync);
        }

        public async Task PutFileAsync(Profile profile, string file, SharingHandler<ISharingFileSender> handler)
        {
            if (profile == null || handler == null || !(profile is ContractProfile receiver))
                throw new ArgumentNullException();
            var fileInfo = new FileInfo(file);
            if (!fileInfo.Exists)
                throw new FileNotFoundException("File not found!", file);
            var length = fileInfo.Length;
            var packet = new { name = fileInfo.Name, length };
            _ = await Network.ConnectAsync("link.share.file", packet, receiver.GetTcpEndPoint(), CancellationToken, async stream =>
            {
                var result = await stream.ReadBlockWithHeaderAsync(Configurations.TcpBufferLimits, CancellationToken);
                var data = new Token(Generator, result);
                if (data["status"].As<string>() != "wait")
                    throw new NetworkException(NetworkError.InvalidData);
                using (var sender = new FileSender(this, receiver, stream, fileInfo.FullName, length))
                {
                    await Dispatcher.InvokeAsync(() => handler.Invoke(sender));
                    await sender.LoopAsync();
                }
                return new Unit();
            });
        }

        public async Task PutDirectoryAsync(Profile profile, string directory, SharingHandler<ISharingDirectorySender> handler)
        {
            if (profile == null || handler == null || !(profile is ContractProfile receiver))
                throw new ArgumentNullException();
            var directoryInfo = new DirectoryInfo(directory);
            if (!directoryInfo.Exists)
                throw new DirectoryNotFoundException("Directory not found!");
            var packet = new { name = directoryInfo.Name };
            _ = await Network.ConnectAsync("link.share.directory", packet, receiver.GetTcpEndPoint(), CancellationToken, async stream =>
            {
                var result = await stream.ReadBlockWithHeaderAsync(Configurations.TcpBufferLimits, CancellationToken);
                var data = new Token(Generator, result);
                if (data["status"].As<string>() != "wait")
                    throw new NetworkException(NetworkError.InvalidData);
                using (var sender = new DirectorySender(this, receiver, stream, directoryInfo.FullName))
                {
                    await Dispatcher.InvokeAsync(() => handler.Invoke(sender));
                    await sender.LoopAsync();
                }
                return new Unit();
            });
        }

        private async Task HandleFileAsync(IRequest parameter)
        {
            var profile = parameter.SenderProfile;
            var handler = NewFileReceiver;
            if (profile == null || handler == null || parameter.NetworkType != NetworkType.Tcp)
                return;
            var data = parameter.Packet.Data;
            var name = data["name"].As<string>();
            var length = data["length"].As<long>();
            using (var receiver = new FileReceiver(this, profile, parameter.Stream, name, length))
            {
                await parameter.ResponseAsync(new { status = "wait" });
                await Dispatcher.InvokeAsync(() => handler.Invoke(receiver));
                await receiver.LoopAsync();
            }
        }

        private async Task HandleDirectoryAsync(IRequest parameter)
        {
            var profile = parameter.SenderProfile;
            var handler = NewDirectoryReceiver;
            if (profile == null || handler == null || parameter.NetworkType != NetworkType.Tcp)
                return;
            var data = parameter.Packet.Data;
            var name = data["name"].As<string>();
            using (var receiver = new DirectoryReceiver(this, profile, parameter.Stream, name))
            {
                await parameter.ResponseAsync(new { status = "wait" });
                await Dispatcher.InvokeAsync(() => handler.Invoke(receiver));
                await receiver.LoopAsync();
            }
        }
    }
}
