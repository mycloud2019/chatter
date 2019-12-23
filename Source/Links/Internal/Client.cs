using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Data.Abstractions;
using Mikodev.Links.Implementations;
using Mikodev.Links.Internal.Messages;
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

namespace Mikodev.Links.Internal
{
    internal sealed partial class Client : IClient, IDisposable
    {
        private const int None = 0, Started = 1, Disposed = 2;

        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private readonly ContractProfile profile;

        private readonly Settings settings;

        private int status = None;

        internal CancellationToken CancellationToken { get; }

        internal IGenerator Generator { get; } = Binary.Generator.CreateDefault();

        internal IStorage Storage { get; }

        internal ICache Cache { get; }

        internal INetwork Network { get; }

        internal IDispatcher Dispatcher { get; }

        internal Contracts Contracts { get; }

        internal Configurations Configurations { get; }

        public Profile Profile => profile;

        public IEnumerable<Profile> Profiles => Contracts.ProfileCollection;

        public string ReceivingDirectory => Configurations.ShareDirectory;

        public event EventHandler<MessageEventArgs> NewMessage;

        public Client(Settings settings, IDispatcher dispatcher, IStorage storage)
        {
            if (settings is null)
                throw new ArgumentNullException(nameof(settings));
            if (dispatcher is null)
                throw new ArgumentNullException(nameof(dispatcher));
            if (storage is null)
                throw new ArgumentNullException(nameof(storage));
            var environment = ((Settings)settings).Configurations;
            CancellationToken = cancellation.Token;
            Dispatcher = dispatcher;
            Storage = storage;

            void ProfileChanged(object sender, PropertyChangedEventArgs e)
            {
                var profile = this.profile;
                Debug.Assert(sender == profile);
                if (e.PropertyName == nameof(ContractProfile.Name))
                    environment.ClientName = profile.Name;
                else if (e.PropertyName == nameof(ContractProfile.Text))
                    environment.ClientText = profile.Text;
                else if (e.PropertyName == nameof(ContractProfile.ImageHash))
                    environment.ClientImageHash = profile.ImageHash;
            }

            this.settings = settings;
            Configurations = environment;
            Network = new Network(this);
            Contracts = new Contracts(this);
            Cache = new Cache(Configurations, Network);

            var imageHash = environment.ClientImageHash;
            var imagePath = default(FileInfo);
            var exists = !string.IsNullOrEmpty(imageHash) && Cache.TryGetCache(imageHash, out imagePath);
            var profile = new ContractProfile(environment.ClientId, ContractProfileType.Client)
            {
                Name = environment.ClientName,
                Text = environment.ClientText,
                UdpPort = environment.UdpEndPoint.Port,
                TcpPort = environment.TcpEndPoint.Port,
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

        public async Task StartAsync()
        {
            if (Interlocked.CompareExchange(ref status, Started, None) != None)
                throw new InvalidOperationException();
            await Task.Yield();
            var network = (Network)Network;
            var manager = Contracts;
            network.Initialize();
            _ = Task.Run(() => network.LoopAsync());
            _ = Task.Run(() => manager.LoopAsync());
        }

        public void CleanProfiles() => Contracts.CleanProfileCollection();

        public Task WriteSettingsAsync(string file) => settings.SaveAsync(file);

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
        public async Task PutTextAsync(Profile profile, string text)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            var message = new NotifyTextMessage();
            message.SetObject(text);
            var packetData = new { messageId = message.MessageId, text, };
            await Dispatcher.InvokeAsync(() => AppendMessage((ContractProfile)profile, message));
            await Network.SendAsync((ContractProfile)profile, message, "link.message.text", packetData);
        }

        /// <summary>
        /// 向目标用户发送图片消息哈希报文, 在文件 IO 出错时抛出异常, 在网络发送失败时不抛出异常
        /// </summary>
        public async Task PutImageAsync(Profile profile, string file)
        {
            var result = await Cache.SetCacheAsync(new FileInfo(file), cancellation.Token);
            var message = new NotifyImageMessage() { ImageHash = result.Hash };
            message.SetObject(result.FileInfo.FullName);
            var packetData = new { messageId = message.MessageId, imageHash = result.Hash, };
            await Dispatcher.InvokeAsync(() => AppendMessage((ContractProfile)profile, message));
            await Network.SendAsync((ContractProfile)profile, message, "link.message.image-hash", packetData);
        }

        public async Task SetProfileImageAsync(string file)
        {
            var result = await Cache.SetCacheAsync(new FileInfo(file), cancellation.Token);
            var profile = this.profile;
            profile.ImageHash = result.Hash;
            profile.SetImagePath(result.FileInfo.FullName);
        }

        private async Task AppendMessage(ContractProfile profile, NotifyMessage message)
        {
            var messages = profile.MessageCollection;
            if (messages.Any(r => r.MessageId == message.MessageId))
                return;
            messages.Add(message);
            var model = new MessageEntry
            {
                MessageId = message.MessageId,
                ProfileId = profile.ProfileId,
                DateTime = message.DateTime,
                Path = message.Path,
                Reference = message.Reference.ToString(),
                Object = message is NotifyTextMessage text ? (string)text.Object : ((NotifyImageMessage)message).ImageHash,
            };
            await Storage.StoreMessagesAsync(new[] { model });
            if (message.Reference != MessageReference.Remote)
                return;
            var eventArgs = new MessageEventArgs(profile, message);
            NewMessage?.Invoke(this, eventArgs);
            if (eventArgs.IsHandled)
                return;
            profile.UnreadCount++;
        }

        private async Task<bool> ResponseAsync(IRequest parameter, NotifyMessage message)
        {
            var success = parameter.SenderProfile != null;
            if (success)
                await Dispatcher.InvokeAsync(() => AppendMessage(parameter.SenderProfile, message));
            var data = new { status = success ? "ok" : "refused" };
            await parameter.ResponseAsync(data);
            return success;
        }

        internal async Task HandleTextAsync(IRequest parameter)
        {
            var data = parameter.Packet.Data;
            var message = new NotifyTextMessage(data["messageId"].As<string>());
            message.SetObject(data["text"].As<string>());
            message.SetStatus(MessageStatus.Success);
            _ = await ResponseAsync(parameter, message);
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(Profile profile)
        {
            static MessageReference AsMessageReference(string reference)
            {
                return reference == MessageReference.Remote.ToString()
                    ? MessageReference.Remote
                    : reference == MessageReference.Local.ToString() ? MessageReference.Local : MessageReference.None;
            }

            NotifyMessage Convert(MessageEntry item)
            {
                if (item.Path == NotifyTextMessage.MessagePath)
                {
                    var message = new NotifyTextMessage(item.MessageId, item.DateTime, AsMessageReference(item.Reference));
                    message.SetObject(item.Object);
                    message.SetStatus(MessageStatus.Success);
                    return message;
                }
                else if (item.Path == NotifyImageMessage.MessagePath)
                {
                    var message = new NotifyImageMessage(item.MessageId, item.DateTime, AsMessageReference(item.Reference));
                    var imageHash = item.Object;
                    message.ImageHash = imageHash;
                    message.SetStatus(MessageStatus.Success);
                    if (Cache.TryGetCache(imageHash, out var info))
                        message.SetObject(info.FullName);
                    return message;
                }
                return default;
            }

            void Migrate(ObservableCollection<NotifyMessage> collection, IEnumerable<MessageEntry> messages)
            {
                var list = messages.Where(m => !collection.Any(x => x.MessageId == m.MessageId)).Select(Convert).Where(x => x != null).ToList();
                list.Reverse();
                list.ForEach(x => collection.Insert(0, x));
            }

            var messages = ((ContractProfile)profile).MessageCollection;
            var list = await Storage.QueryMessagesAsync(profile.ProfileId, 30);
            Migrate(messages, list);
            return messages;
        }

        internal async Task HandleImageAsync(IRequest parameter)
        {
            var data = parameter.Packet.Data;
            var imageHash = data["imageHash"].As<string>();
            var message = new NotifyImageMessage(data["messageId"].As<string>()) { ImageHash = imageHash, };
            message.SetStatus(MessageStatus.Pending);

            try
            {
                if (await ResponseAsync(parameter, message) == false)
                    return;
                var fileInfo = await Cache.GetCacheAsync(imageHash, parameter.SenderProfile.GetTcpEndPoint(), cancellation.Token);
                await Dispatcher.InvokeAsync(() =>
                {
                    message.SetObject(fileInfo.FullName);
                    message.SetStatus(MessageStatus.Success);
                });
            }
            catch (Exception)
            {
                await Dispatcher.InvokeAsync(() => message.SetStatus(MessageStatus.Aborted));
                throw;
            }
        }
    }
}
