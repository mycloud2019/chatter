using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Internal;
using Mikodev.Links.Messages;
using Mikodev.Tasks.Abstractions;
using Mikodev.Tasks.TaskCompletionManagement;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Implementations
{
    internal sealed class LinkNetwork : ILinkNetwork, IDisposable
    {
        private UdpClient udpClient;

        private TcpListener tcpListener;

        private readonly CancellationToken cancellationToken;

        private readonly LinkEnvironment environment;

        private readonly LinkClient client;

        private readonly IGenerator generator;

        private readonly ITaskCompletionManager<string, LinkPacket> completionManager = new TaskCompletionManager<string, LinkPacket>();

        private readonly ConcurrentDictionary<string, Func<ILinkRequest, Task>> funcs = new ConcurrentDictionary<string, Func<ILinkRequest, Task>>();

        internal LinkNetwork(LinkClient client)
        {
            this.client = client;
            generator = client.Generator;
            environment = client.Environment;
            cancellationToken = client.CancellationToken;
            RegisterHandler("link.async-result", HandleRequestAsync);
            Debug.Assert(generator != null);
            Debug.Assert(environment != null);
        }

        internal async Task InitAsync()
        {
            var profile = (LinkProfile)client.Profile;
            var udpEndPoint = environment.UdpEndPoint;
            var tcpEndPoint = environment.TcpEndPoint;

            udpClient = new UdpClient(udpEndPoint) { EnableBroadcast = true };
            tcpListener = new TcpListener(tcpEndPoint);
            tcpListener.Start();
            await client.Dispatcher.InvokeAsync(() =>
            {
                profile.UdpPort = udpEndPoint.Port;
                profile.TcpPort = tcpEndPoint.Port;
            });
        }

        internal Task LoopAsync()
        {
            var tasks = new Task[]
            {
                Task.Run(TcpLoopAsync),
                Task.Run(UdpLoopAsync),
            };
            return Task.WhenAll(tasks);
        }

        private async Task UdpLoopAsync()
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await udpClient.ReceiveAsync();
                _ = Task.Run(() => HandleConnectionAsync(LinkRequest.CreateUdpParameter(client, result.Buffer, result.RemoteEndPoint, this)));
            }
        }

        private async Task TcpLoopAsync()
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await tcpListener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(result));
            }
        }

        private async Task HandleClientAsync(TcpClient result)
        {
            var cancel = new CancellationTokenSource();
            var stream = default(Stream);

            try
            {
                stream = result.GetStream();
                var endpoint = (IPEndPoint)result.Client.RemoteEndPoint;
                var buffer = await stream.ReadBlockWithHeaderAsync(environment.TcpBufferLimits, cancel.Token).TimeoutAfter(environment.TcpTimeout);
                var parameter = LinkRequest.CreateTcpParameter(client, buffer, endpoint, stream, cancel.Token);
                await HandleConnectionAsync(parameter);
            }
            finally
            {
                cancel.Cancel();
                cancel.Dispose();
                stream?.Dispose();
                result?.Dispose();
            }
        }

        private Task HandleConnectionAsync(LinkRequest parameter)
        {
            return funcs.TryGetValue(parameter.Packet.Path, out var functor)
                ? functor.Invoke(parameter)
                : Task.FromResult(-1);
        }

        private byte[] CreatePacket(string path, object data)
        {
            return generator.Encode(new
            {
                senderId = environment.ClientId,
                path,
                data,
            });
        }

        internal async Task<TcpClient> CreateClientAsync(string path, object data, IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            var packet = CreatePacket(path, data);
            var client = new TcpClient();
            var stream = default(Stream);
            try
            {
                await client.ConnectAsync(endpoint.Address, endpoint.Port).TimeoutAfter(environment.TcpConnectTimeout);
                stream = client.GetStream();
                await stream.WriteWithHeaderAsync(packet, cancellationToken).TimeoutAfter(environment.TcpTimeout);
                return client;
            }
            catch (Exception)
            {
                stream?.Dispose();
                client?.Dispose();
                throw;
            }
        }

        private List<Uri> GetBroadcastUris()
        {
            static bool AddressFilter(Uri uri) =>
                uri.HostNameType == UriHostNameType.IPv4
                    ? IPAddress.TryParse(uri.Host, out var address) && IPAddress.Broadcast.Equals(address)
                    : false;

            var uris = environment.BroadcastUris.ToList();

            while (true)
            {
                var uri = uris.FirstOrDefault(AddressFilter);
                if (uri == null)
                    break;

                var port = uri.Port;
                _ = uris.Remove(uri);
                var addresses = Extensions.GetBroadcastAddresses();
                var targetUris = addresses.Select(x => new Uri($"udp://{x}:{port}")).ToList();
                uris.AddRange(targetUris);

                Debug.Assert(!uris.Any(x => ReferenceEquals(x, uri)));
                Debug.Assert(targetUris.All(x => x.Port == uri.Port));
            }

            Debug.Assert(uris != null);
            Debug.Assert(uris.Count > 0);
            return uris;
        }

        private async Task SendToAsync(Uri uri, string path, object data)
        {
            Debug.Assert(uri != null);
            Debug.Assert(uri.Scheme == "udp");

            try
            {
                var packet = CreatePacket(path, data);
                var hostType = uri.HostNameType;
                var address = hostType == UriHostNameType.IPv4 || hostType == UriHostNameType.IPv6
                    ? IPAddress.Parse(uri.Host)
                    : (await Dns.GetHostEntryAsync(uri.Host)).AddressList.FirstOrDefault(x => x.AddressFamily == udpClient.Client.AddressFamily);
                if (address == null)
                    throw new LinkException(LinkError.InvalidHost, $"Invalid host: '{uri.Host}'");
                _ = await udpClient.SendAsync(packet, packet.Length, new IPEndPoint(address, uri.Port));
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
        }

        public async Task BroadcastAsync(string path, object data)
        {
            var uris = GetBroadcastUris();
            var list = uris.Select(x => SendToAsync(x, path, data)).ToList();
            await Task.WhenAll(list);
        }

        private async Task<LinkPacket> RequestAsync(string path, object data, IPEndPoint endpoint, TimeSpan limits)
        {
            var task = completionManager.CreateNew(limits, _ => $"{Guid.NewGuid():N}", out var packetId, default);
            var packet = new
            {
                packetId,
                senderId = environment.ClientId,
                path,
                data,
            };
            var buffer = generator.Encode(packet);
            if (buffer.Length > environment.UdpLengthLimits)
                throw new LinkException(LinkError.UdpPacketTooLarge);
            _ = await udpClient.SendAsync(buffer, buffer.Length, endpoint);
            return await task;
        }

        internal Task ResponseAsync(string packetId, object data, IPEndPoint endpoint)
        {
            var packet = new
            {
                packetId,
                senderId = environment.ClientId,
                path = "link.async-result",
                data,
            };
            var buffer = generator.Encode(packet);
            return udpClient.SendAsync(buffer, buffer.Length, endpoint);
        }

        public async Task SendAsync(LinkProfile profile, NotifyMessage message, string path, object packetData)
        {
            message.SetStatus(MessageStatus.Pending);

            bool Handled(Token token)
            {
                var status = token["status"].As<string>();
                var flag = status == "ok"
                    ? MessageStatus.Success
                    : status == "refused" ? MessageStatus.Refused : default;
                if (flag == default)
                    return false;
                message.SetStatus(flag);
                return true;
            }

            for (var i = 0; i < 2; i++)
            {
                try
                {
                    var result = await RequestAsync(path, packetData, profile.GetUdpEndPoint(), environment.UdpTimeout);
                    if (Handled(result.Data))
                        return;
                }
                catch (LinkException ex) when (ex.ErrorCode == LinkError.UdpPacketTooLarge)
                {
                    break;
                }
                catch (TimeoutException)
                {
                    continue;
                }
            }

            var tcp = default(TcpClient);
            var stream = default(Stream);
            var cancel = new CancellationTokenSource();

            try
            {
                tcp = await CreateClientAsync(path, packetData, profile.GetTcpEndPoint(), cancel.Token);
                stream = tcp.GetStream();
                var buffer = await stream.ReadBlockWithHeaderAsync(environment.TcpBufferLimits, cancel.Token).TimeoutAfter(environment.TcpTimeout);
                var token = new Token(generator, buffer);
                if (Handled(token))
                    return;
            }
            catch (Exception)
            {
                // ignore
            }
            finally
            {
                cancel.Cancel();
                cancel.Dispose();
                stream?.Dispose();
                tcp?.Dispose();
            }

            message.SetStatus(MessageStatus.Aborted);
        }

        public void Dispose()
        {
            udpClient?.Dispose();
            tcpListener?.Stop();
        }

        private Task HandleRequestAsync(ILinkRequest request)
        {
            var packet = request.Packet;
            _ = completionManager.SetResult(packet.PacketId, packet);
            return Task.FromResult(0);
        }

        public void RegisterHandler(string path, Func<ILinkRequest, Task> func)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            if (funcs.TryAdd(path, func))
                return;
            throw new ArgumentException("Duplicate path detected!");
        }

        public async Task<T> ConnectAsync<T>(string path, object data, IPEndPoint endpoint, CancellationToken token, Func<Stream, Task<T>> func)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            if (endpoint is null)
                throw new ArgumentNullException(nameof(endpoint));
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            using (var client = await CreateClientAsync(path, data, endpoint, token))
                return await func.Invoke(client.GetStream());
        }
    }
}
