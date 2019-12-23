using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Annotations;
using Mikodev.Links.Data.Abstractions;
using Mikodev.Links.Implementations;
using Mikodev.Links.Internal;
using Mikodev.Links.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links
{
    internal sealed partial class LinkClient : Client, IDisposable
    {
        private const int None = 0, Started = 1, Disposed = 2;

        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private readonly LinkProfile profile;

        private int status = None;

        internal CancellationToken CancellationToken { get; }

        internal IGenerator Generator { get; } = Binary.Generator.CreateDefault();

        internal ILinkDataStore DataStore { get; }

        internal ILinkCache Cache { get; }

        internal ILinkNetwork Network { get; }

        internal ILinkUIContext UIContext { get; }

        internal LinkContracts Contracts { get; }

        internal LinkEnvironment Environment { get; }

        public override Profile Profile => profile;

        public override ILinkSettings Settings { get; }

        public override IEnumerable<Profile> Profiles => Contracts.ProfileCollection;

        public override string ReceivingDirectory => Environment.ShareDirectory;

        public override event EventHandler<MessageEventArgs> NewMessage;

        public LinkClient(ILinkSettings settings, ILinkUIContext context, ILinkDataStore store)
        {
            if (settings is null)
                throw new ArgumentNullException(nameof(settings));
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (store is null)
                throw new ArgumentNullException(nameof(store));
            var environment = ((LinkSettings)settings).Environment;
            CancellationToken = cancellation.Token;
            UIContext = context;
            DataStore = store;

            void ProfileChanged(object sender, PropertyChangedEventArgs e)
            {
                var profile = this.profile;
                Debug.Assert(sender == profile);
                if (e.PropertyName == nameof(LinkProfile.Name))
                    environment.ClientName = profile.Name;
                else if (e.PropertyName == nameof(LinkProfile.Text))
                    environment.ClientText = profile.Text;
                else if (e.PropertyName == nameof(LinkProfile.ImageHash))
                    environment.ClientImageHash = profile.ImageHash;
            }

            Settings = settings;
            Environment = environment;
            Network = new LinkNetwork(this);
            Contracts = new LinkContracts(this);
            Cache = new LinkCache(Environment, Network);

            var imageHash = environment.ClientImageHash;
            var imagePath = default(FileInfo);
            var exists = !string.IsNullOrEmpty(imageHash) && Cache.TryGetCache(imageHash, out imagePath);
            var profile = new LinkProfile(environment.ClientId, LinkProfileType.Client)
            {
                Name = environment.ClientName,
                Text = environment.ClientText,
                ImageHash = exists ? imageHash : string.Empty,
            };
            profile.SetImagePath(exists ? imagePath.FullName : string.Empty);
            profile.SetIPAddress(IPAddress.Loopback);

            this.profile = profile;
            profile.PropertyChanged += ProfileChanged;

            Initial(Network);
            Network.RegisterHandler("link.message.text", HandleTextAsync);
            Network.RegisterHandler("link.message.image-hash", HandleImageAsync);
        }

        public override async Task<Task> StartAsync()
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

        public override void CleanProfiles() => Contracts.CleanProfileCollection();

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
        public override async Task PutTextAsync(Profile profile, string text)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            var message = new TextMessage();
            message.SetObject(text);
            var packetData = new { messageId = message.MessageId, text, };
            await Network.SendAsync((LinkProfile)profile, message, "link.message.text", packetData);
        }

        /// <summary>
        /// 向目标用户发送图片消息哈希报文, 在文件 IO 出错时抛出异常, 在网络发送失败时不抛出异常
        /// </summary>
        public override async Task PutImageAsync(Profile profile, string file)
        {
            var result = await Cache.SetCacheAsync(new FileInfo(file), cancellation.Token);
            var message = new ImageMessage() { ImageHash = result.Hash };
            message.SetObject(result.FileInfo.FullName);
            var packetData = new { messageId = message.MessageId, imageHash = result.Hash, };
            await Network.SendAsync((LinkProfile)profile, message, "link.message.image-hash", packetData);
        }

        public override async Task SetProfileImageAsync(string file)
        {
            var result = await Cache.SetCacheAsync(new FileInfo(file), cancellation.Token);
            var profile = this.profile;
            profile.ImageHash = result.Hash;
            profile.SetImagePath(result.FileInfo.FullName);
        }

        private async Task<bool> ResponseAsync(ILinkRequest parameter, NotifyMessage message)
        {
            void AppendMessage()
            {
                var profile = parameter.SenderProfile;
                var messages = profile.MessageCollection;
                if (messages.Any(r => r.MessageId == message.MessageId))
                    return;
                messages.Add(message);
                var eventArgs = new MessageEventArgs(this, profile, message);
                NewMessage?.Invoke(this, eventArgs);
                if (eventArgs.IsHandled)
                    return;
                profile.UnreadCount++;
            }

            var success = parameter.SenderProfile != null;
            if (success)
            {
                await UIContext.InvokeAsync(AppendMessage);
                var model = new MessageModel
                {
                    MessageId = message.MessageId,
                    ProfileId = parameter.SenderProfile.ProfileId,
                    DateTime = message.DateTime,
                    Path = message.Path,
                    Reference = message.Reference.ToString(),
                    Data = (string)message.Object,
                };
                _ = Task.Run(() => DataStore.StoreMessagesAsync(new[] { model }));
            }
            var data = new { status = success ? "ok" : "refused" };
            await parameter.ResponseAsync(data);
            return success;
        }

        internal async Task HandleTextAsync(ILinkRequest parameter)
        {
            var data = parameter.Packet.Data;
            var message = new TextMessage(data["messageId"].As<string>());
            message.SetObject(data["text"].As<string>());
            message.SetStatus(MessageStatus.Success);
            _ = await ResponseAsync(parameter, message);
        }

        public override async Task<IEnumerable<Message>> GetMessagesAsync(Profile profile)
        {
            static MessageReference AsMessageReference(string reference)
            {
                return reference == MessageReference.Remote.ToString()
                    ? MessageReference.Remote
                    : reference == MessageReference.Local.ToString() ? MessageReference.Local : MessageReference.None;
            }

            NotifyMessage Convert(MessageModel item)
            {
                if (item.Path == TextMessage.MessagePath)
                {
                    var message = new TextMessage(item.MessageId, item.DateTime, AsMessageReference(item.Reference));
                    message.SetObject(item.Data);
                    message.SetStatus(MessageStatus.Success);
                    return message;
                }
                else if (item.Path == ImageMessage.MessagePath)
                {
                    var message = new ImageMessage(item.MessageId, item.DateTime, AsMessageReference(item.Reference));
                    var imageHash = item.Data;
                    message.ImageHash = imageHash;
                    message.SetStatus(MessageStatus.Success);
                    if (Cache.TryGetCache(imageHash, out var info))
                        message.SetObject(info.FullName);
                    return message;
                }
                return default;
            }

            void Migrate(ObservableCollection<NotifyMessage> collection, IEnumerable<MessageModel> messages)
            {
                var list = messages.Where(m => !collection.Any(x => x.MessageId == m.MessageId)).Select(Convert).Where(x => x != null).ToList();
                list.Reverse();
                list.ForEach(x => collection.Insert(0, x));
            }

            var messages = ((LinkProfile)profile).MessageCollection;
            var list = await DataStore.QueryMessagesAsync(profile.ProfileId, 30);
            Migrate(messages, list);
            return messages;
        }

        internal async Task HandleImageAsync(ILinkRequest parameter)
        {
            var data = parameter.Packet.Data;
            var imageHash = data["imageHash"].As<string>();
            var message = new ImageMessage(data["messageId"].As<string>()) { ImageHash = imageHash, };
            message.SetStatus(MessageStatus.Pending);

            try
            {
                if (await ResponseAsync(parameter, message) == false)
                    return;
                var fileInfo = await Cache.GetCacheAsync(imageHash, parameter.SenderProfile.GetTcpEndPoint(), cancellation.Token);
                await UIContext.InvokeAsync(() =>
                {
                    message.SetObject(fileInfo.FullName);
                    message.SetStatus(MessageStatus.Success);
                });
            }
            catch (Exception)
            {
                await UIContext.InvokeAsync(() => message.SetStatus(MessageStatus.Aborted));
                throw;
            }
        }
    }
}
