using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Implementations;
using Mikodev.Links.Messages;
using Mikodev.Links.Messages.Implementations;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links
{
    public sealed partial class LinkClient : IDisposable
    {
        #region private
        private const int None = 0, Started = 1, Disposed = 2;

        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private int status = None;
        #endregion

        #region internal
        internal CancellationToken CancellationToken { get; }

        internal IGenerator Generator { get; } = Binary.Generator.CreateDefault();

        internal ILinkCache Cache { get; }

        internal ILinkNetwork Network { get; }

        internal LinkContracts Contracts { get; }

        internal LinkEnvironment Environment { get; }
        #endregion

        public LinkProfile Profile { get; }

        public LinkSettings Settings { get; }

        public ILinkUIContext UIContext { get; }

        public BindingList<LinkProfile> ProfileCollection { get; } = new BindingList<LinkProfile>();

        public DirectoryInfo ReceiveDirectory => new DirectoryInfo(Environment.ShareDirectory);

        public event EventHandler<MessageEventArgs> NewMessage;

        public LinkClient(LinkSettings settings, ILinkUIContext context)
        {
            if (settings is null)
                throw new ArgumentNullException(nameof(settings));
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            var environment = settings.Environment;
            CancellationToken = cancellation.Token;
            UIContext = context;

            void ProfileChanged(object sender, PropertyChangedEventArgs e)
            {
                Debug.Assert(sender == Profile);
                if (e.PropertyName == nameof(LinkProfile.Name))
                    environment.ClientName = Profile.Name;
                else if (e.PropertyName == nameof(LinkProfile.Text))
                    environment.ClientText = Profile.Text;
                else if (e.PropertyName == nameof(LinkProfile.ImageHash))
                    environment.ClientImageHash = Profile.ImageHash;
            }

            Settings = settings;
            Environment = environment;
            Network = new LinkNetwork(this);
            Contracts = new LinkContracts(this);
            Cache = new LinkCache(Environment, Network);

            var imageHash = environment.ClientImageHash;
            var imagePath = default(FileInfo);
            var exists = !string.IsNullOrEmpty(imageHash) && Cache.TryGetCache(imageHash, out imagePath);

            Profile = new LinkProfile(environment.ClientId, LinkProfileType.Client)
            {
                Name = environment.ClientName,
                Text = environment.ClientText,
                ImageHash = exists ? imageHash : string.Empty,
                ImagePath = exists ? imagePath.FullName : string.Empty,
                Address = IPAddress.Loopback,
            };
            Profile.PropertyChanged += ProfileChanged;

            Initial(Network);
            Network.RegisterHandler("link.message.text", HandleTextAsync);
            Network.RegisterHandler("link.message.image-hash", HandleImageAsync);
        }

        public async Task<Task> StartAsync()
        {
            if (Interlocked.CompareExchange(ref status, Started, None) != None)
                throw new InvalidOperationException();
            var network = (LinkNetwork)Network;
            var manager = Contracts;
            await network.InitAsync();

            var tasks = new Task[]
            {
                network.LoopAsync() ,
                manager.LoopAsync(),
            };
            return Task.WhenAll(tasks);
        }

        public void CleanProfileCollection() => Contracts.CleanProfileCollection();

        public void Dispose()
        {
            if (Interlocked.Exchange(ref status, Disposed) == Disposed)
                return;
            cancellation.Dispose();
            (Cache as IDisposable)?.Dispose();
            (Network as IDisposable).Dispose();
        }

        /// <summary>
        /// 向目标用户发送文本消息, 失败时不抛出异常
        /// </summary>
        /// <param name="profile">目标用户</param>
        /// <param name="text">文本信息内容</param>
        public async Task SendTextAsync(LinkProfile profile, string text)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            var message = new TextMessage() { Text = text, };
            var packetData = new { messageId = message.Id, text, };
            await Network.SendAsync(profile, message, "link.message.text", packetData);
        }

        /// <summary>
        /// 向目标用户发送图片消息哈希报文, 在文件 IO 出错时抛出异常, 在网络发送失败时不抛出异常
        /// </summary>
        /// <param name="profile">目标用户</param>
        /// <param name="filePath">图片路径</param>
        public async Task SendImageAsync(LinkProfile profile, FileInfo fileInfo)
        {
            var result = await Cache.SetCacheAsync(fileInfo, cancellation.Token);
            var message = new ImageMessage() { ImageHash = result.Hash, ImagePath = result.FileInfo.FullName };
            var packetData = new { messageId = message.Id, imageHash = result.Hash, };
            await Network.SendAsync(profile, message, "link.message.image-hash", packetData);
        }

        public async Task<string> SetProfileImageAsync(FileInfo fileInfo)
        {
            var result = await Cache.SetCacheAsync(fileInfo, cancellation.Token);
            var profile = Profile;
            profile.ImageHash = result.Hash;
            profile.ImagePath = result.FileInfo.FullName;
            return result.Hash;
        }

        private async Task<bool> ResponseAsync(ILinkRequest parameter, Message message)
        {
            void AppendMessage()
            {
                var profile = parameter.SenderProfile;
                var messages = profile.MessageCollection;
                if (messages.Any(r => r.Id == message.Id))
                    return;
                messages.Add(message);
                var eventArgs = new MessageEventArgs(this, profile, message);
                NewMessage?.Invoke(this, eventArgs);
                if (eventArgs.IsHandled)
                    return;
                profile.Hint++;
            }

            var success = parameter.SenderProfile != null;
            if (success)
                await UIContext.InvokeAsync(AppendMessage);
            var data = new { status = success ? "ok" : "refused" };
            await parameter.ResponseAsync(data);
            return success;
        }

        internal async Task HandleTextAsync(ILinkRequest parameter)
        {
            var data = parameter.Packet.Data;
            var message = new TextMessage(data["messageId"].As<string>()) { Text = data["text"].As<string>(), Status = MessageStatus.Success };
            _ = await ResponseAsync(parameter, message);
        }

        internal async Task HandleImageAsync(ILinkRequest parameter)
        {
            var data = parameter.Packet.Data;
            var imageHash = data["imageHash"].As<string>();
            var message = new ImageMessage(data["messageId"].As<string>()) { ImageHash = imageHash, Status = MessageStatus.Pending };

            try
            {
                if (await ResponseAsync(parameter, message) == false)
                    return;
                var fileInfo = await Cache.GetCacheAsync(imageHash, parameter.SenderProfile.GetTcpEndPoint(), cancellation.Token);
                await UIContext.InvokeAsync(() =>
                {
                    message.ImagePath = fileInfo.FullName;
                    message.Status = MessageStatus.Success;
                });
            }
            catch (Exception)
            {
                await UIContext.InvokeAsync(() => message.Status = MessageStatus.Aborted);
                throw;
            }
        }
    }
}
