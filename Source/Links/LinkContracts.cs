using Mikodev.Links.Abstractions;
using Mikodev.Links.Annotations;
using Mikodev.Links.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Mikodev.Optional.Extensions;

namespace Mikodev.Links
{
    internal sealed class LinkContracts
    {
        private readonly LinkClient client;

        private readonly LinkEnvironment environment;

        private readonly object locker = new object();

        private readonly Dictionary<string, LinkProfile> profiles = new Dictionary<string, LinkProfile>();

        public ObservableCollection<LinkProfile> ProfileCollection { get; } = new ObservableCollection<LinkProfile>();

        internal LinkContracts(LinkClient client)
        {
            this.client = client;
            environment = client.Environment;
            var network = client.Network;
            Debug.Assert(environment != null);
            Debug.Assert(network != null);
            network.RegisterHandler("link.broadcast", HandleBroadcastAsync);
        }

        internal LinkProfile FindProfile(string id) => Lock(locker, () => profiles.TryGetValue(id, out var profile) ? profile : null);

        internal void CleanProfileCollection()
        {
            client.UIContext.VerifyAccess();

            var collection = ProfileCollection;

            static bool NotOnline(LinkProfile profile) => profile.OnlineStatus != ProfileOnlineStatus.Online;

            lock (locker)
            {
                var alpha = collection.Where(NotOnline).ToList();
                var bravo = profiles.Values.Where(NotOnline).ToList();
                alpha.ForEach(x => collection.Remove(x));
                bravo.ForEach(x => profiles.Remove(x.ProfileId));
            }
        }

        internal Task LoopAsync()
        {
            var tasks = new Task[]
            {
                Task.Run(UpdateLoopAsync),
                Task.Run(BroadcastLoopAsync),
            };
            return Task.WhenAll(tasks);
        }

        private async Task UpdateLoopAsync()
        {
            int UpdateStatus(LinkProfile profile)
            {
                var span = DateTime.Now - profile.LastOnlineDateTime;
                if (span < TimeSpan.Zero || span > environment.ProfileOnlineTimeout)
                    profile.SetOnlineStatus(ProfileOnlineStatus.Offline);
                var imageHash = profile.RemoteImageHash;
                if (!string.IsNullOrEmpty(imageHash) && imageHash != profile.ImageHash)
                    _ = Task.Run(() => UpdateImageAsync(profile));
                return 1;
            }

            var token = client.CancellationToken;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                await client.UIContext.InvokeAsync(() => Lock(locker, () => profiles.Values.Sum(UpdateStatus)));
                await Task.Delay(environment.BackgroundTaskDelay);
            }
        }

        private async Task UpdateImageAsync(LinkProfile profile)
        {
            var cache = client.Cache;
            var imageHash = profile.RemoteImageHash;

            var fileInfo = await cache.GetCacheAsync(imageHash, profile.GetTcpEndPoint(), client.CancellationToken);
            profile.ImageHash = imageHash;
            await client.UIContext.InvokeAsync(() => profile.SetImagePath(fileInfo.FullName));
        }

        private async Task BroadcastLoopAsync()
        {
            var profile = (LinkProfile)client.Profile;
            var network = client.Network;
            var token = client.CancellationToken;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                var data = new
                {
                    name = profile.Name,
                    text = profile.Text,
                    udpPort = profile.UdpPort,
                    tcpPort = profile.TcpPort,
                    imageHash = profile.ImageHash,
                };
                await network.BroadcastAsync("link.broadcast", data);
                await Task.Delay(environment.BroadcastTaskDelay, token);
            }
        }

        internal async Task HandleBroadcastAsync(ILinkRequest parameter)
        {
            var packet = parameter.Packet;
            var data = packet.Data;
            var senderId = packet.SenderId;
            // if senderId == environment.ClientId then ...
            var address = parameter.Address;
            var tcpPort = data["tcpPort"].As<int>();
            var udpPort = data["udpPort"].As<int>();
            var name = data["name"].As<string>();
            var text = data["text"].As<string>();
            var imageHash = data["imageHash"].As<string>();

            var instance = default(LinkProfile);
            var profile = Lock(locker, () => profiles.GetOrAdd(senderId, key => instance = new LinkProfile(key, LinkProfileType.Client)));
            await client.UIContext.InvokeAsync(() =>
            {
                profile.Name = name;
                profile.Text = text;
                profile.SetIPAddress(address);
                profile.TcpPort = tcpPort;
                profile.UdpPort = udpPort;
                profile.LastOnlineDateTime = DateTime.Now;
                profile.SetOnlineStatus(ProfileOnlineStatus.Online);
                profile.RemoteImageHash = imageHash;

                if (instance != null)
                    ProfileCollection.Add(profile);
            });
        }
    }
}
