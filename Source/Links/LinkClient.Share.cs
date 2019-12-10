using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Abstractions.Models;
using Mikodev.Links.Internal;
using Mikodev.Links.Sharing;
using Mikodev.Optional;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links
{
    public delegate void FileSenderHandler(FileSender sender);

    public delegate void FileReceiverHandler(FileReceiver receiver);

    public delegate void DirectorySenderHandler(DirectorySender sender);

    public delegate void DirectoryReceiverHandler(DirectoryReceiver receiver);

    public partial class LinkClient
    {
        public event FileReceiverHandler NewFileReceiver;

        public event DirectoryReceiverHandler NewDirectoryReceiver;

        private void Initial(ILinkNetwork network)
        {
            network.RegisterHandler("link.share.file", HandleFileAsync);
            network.RegisterHandler("link.share.directory", HandleDirectoryAsync);
        }

        public async Task SendFileAsync(LinkProfile profile, string filePath, FileSenderHandler handler)
        {
            if (profile == null || handler == null)
                throw new ArgumentNullException();
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                throw new FileNotFoundException("File not found!", filePath);
            var length = fileInfo.Length;
            var packet = new { name = fileInfo.Name, length };
            _ = await Network.ConnectAsync("link.share.file", packet, profile.GetTcpEndPoint(), CancellationToken, async stream =>
            {
                var result = await stream.ReadBlockWithHeaderAsync(Environment.TcpBufferLimits, CancellationToken);
                var data = new Token(Generator, result);
                if (data["status"].As<string>() != "wait")
                    throw new LinkException(LinkError.InvalidData);
                using (var sender = new FileSender(this, profile, stream, fileInfo.FullName, length))
                {
                    await UIContext.InvokeAsync(() => handler.Invoke(sender));
                    await sender.LoopAsync();
                }
                return new Unit();
            });
        }

        public async Task SendDirectoryAsync(LinkProfile profile, string directoryPath, DirectorySenderHandler handler)
        {
            if (profile == null || handler == null)
                throw new ArgumentNullException();
            var directoryInfo = new DirectoryInfo(directoryPath);
            if (!directoryInfo.Exists)
                throw new DirectoryNotFoundException("Directory not found!");
            var packet = new { name = directoryInfo.Name };
            _ = await Network.ConnectAsync("link.share.directory", packet, profile.GetTcpEndPoint(), CancellationToken, async stream =>
            {
                var result = await stream.ReadBlockWithHeaderAsync(Environment.TcpBufferLimits, CancellationToken);
                var data = new Token(Generator, result);
                if (data["status"].As<string>() != "wait")
                    throw new LinkException(LinkError.InvalidData);
                using (var sender = new DirectorySender(this, profile, stream, directoryInfo.FullName))
                {
                    await UIContext.InvokeAsync(() => handler.Invoke(sender));
                    await sender.LoopAsync();
                }
                return new Unit();
            });
        }

        private async Task HandleFileAsync(ILinkRequest parameter)
        {
            var profile = parameter.SenderProfile;
            var handler = NewFileReceiver;
            if (profile == null || handler == null || parameter.ConnectionType != ConnectionType.Tcp)
                return;
            var data = parameter.Packet.Data;
            var name = data["name"].As<string>();
            var length = data["length"].As<long>();
            using (var receiver = new FileReceiver(this, profile, parameter.Stream, name, length))
            {
                await parameter.ResponseAsync(new { status = "wait" });
                await UIContext.InvokeAsync(() => handler.Invoke(receiver));
                await receiver.LoopAsync();
            }
        }

        private async Task HandleDirectoryAsync(ILinkRequest parameter)
        {
            var profile = parameter.SenderProfile;
            var handler = NewDirectoryReceiver;
            if (profile == null || handler == null || parameter.ConnectionType != ConnectionType.Tcp)
                return;
            var data = parameter.Packet.Data;
            var name = data["name"].As<string>();
            using (var receiver = new DirectoryReceiver(this, profile, parameter.Stream, name))
            {
                await parameter.ResponseAsync(new { status = "wait" });
                await UIContext.InvokeAsync(() => handler.Invoke(receiver));
                await receiver.LoopAsync();
            }
        }
    }
}
