using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Abstractions.Models;
using Mikodev.Links.Annotations;
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

    internal partial class LinkClient
    {
        public override event FileReceiverHandler NewFileReceiver;

        public override event DirectoryReceiverHandler NewDirectoryReceiver;

        private void Initial(ILinkNetwork network)
        {
            network.RegisterHandler("link.share.file", HandleFileAsync);
            network.RegisterHandler("link.share.directory", HandleDirectoryAsync);
        }

        public override async Task SendFileAsync(Profile profile, string file, FileSenderHandler handler)
        {
            if (profile == null || handler == null || !(profile is LinkProfile receiver))
                throw new ArgumentNullException();
            var fileInfo = new FileInfo(file);
            if (!fileInfo.Exists)
                throw new FileNotFoundException("File not found!", file);
            var length = fileInfo.Length;
            var packet = new { name = fileInfo.Name, length };
            _ = await Network.ConnectAsync("link.share.file", packet, receiver.GetTcpEndPoint(), CancellationToken, async stream =>
            {
                var result = await stream.ReadBlockWithHeaderAsync(Environment.TcpBufferLimits, CancellationToken);
                var data = new Token(Generator, result);
                if (data["status"].As<string>() != "wait")
                    throw new LinkException(LinkError.InvalidData);
                using (var sender = new FileSender(this, receiver, stream, fileInfo.FullName, length))
                {
                    await UIContext.InvokeAsync(() => handler.Invoke(sender));
                    await sender.LoopAsync();
                }
                return new Unit();
            });
        }

        public override async Task SendDirectoryAsync(Profile profile, string directory, DirectorySenderHandler handler)
        {
            if (profile == null || handler == null || !(profile is LinkProfile receiver))
                throw new ArgumentNullException();
            var directoryInfo = new DirectoryInfo(directory);
            if (!directoryInfo.Exists)
                throw new DirectoryNotFoundException("Directory not found!");
            var packet = new { name = directoryInfo.Name };
            _ = await Network.ConnectAsync("link.share.directory", packet, receiver.GetTcpEndPoint(), CancellationToken, async stream =>
            {
                var result = await stream.ReadBlockWithHeaderAsync(Environment.TcpBufferLimits, CancellationToken);
                var data = new Token(Generator, result);
                if (data["status"].As<string>() != "wait")
                    throw new LinkException(LinkError.InvalidData);
                using (var sender = new DirectorySender(this, receiver, stream, directoryInfo.FullName))
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
