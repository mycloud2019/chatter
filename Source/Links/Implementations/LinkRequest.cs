using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Abstractions.Models;
using Mikodev.Links.Internal;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Implementations
{
    internal sealed class LinkRequest : ILinkRequest
    {
        private readonly IPEndPoint endpoint;

        private readonly LinkClient client;

        private readonly LinkNetwork network;

        public ConnectionType ConnectionType { get; }

        public LinkPacket Packet { get; }

        public LinkProfile SenderProfile { get; }

        public IPAddress Address => endpoint.Address;

        public Stream Stream { get; }

        public CancellationToken CancellationToken { get; }

        private LinkRequest(ConnectionType connectionType, LinkClient client, LinkNetwork network, byte[] buffer, IPEndPoint endpoint, Stream stream, CancellationToken cancellationToken)
        {
            Debug.Assert(endpoint != null);
            Debug.Assert(connectionType != ConnectionType.Tcp || stream != null);
            this.endpoint = endpoint;
            this.client = client;
            this.network = network;

            ConnectionType = connectionType;
            Stream = stream;
            Packet = new LinkPacket(client.Generator, buffer);
            CancellationToken = cancellationToken;
            SenderProfile = client.Contracts.FindProfile(Packet.SenderId);
        }

        public async Task ResponseAsync(object data)
        {
            if (ConnectionType == ConnectionType.Tcp)
                await Stream.WriteWithHeaderAsync(client.Generator.Encode(data), CancellationToken);
            else
                await network.ResponseAsync(Packet.PacketId, data, endpoint);
        }

        public static LinkRequest CreateUdpParameter(LinkClient client, byte[] buffer, IPEndPoint endpoint, LinkNetwork network)
        {
            return new LinkRequest(ConnectionType.Udp, client, network, buffer, endpoint, null, CancellationToken.None);
        }

        public static LinkRequest CreateTcpParameter(LinkClient client, byte[] buffer, IPEndPoint endpoint, Stream stream, CancellationToken cancellationToken)
        {
            return new LinkRequest(ConnectionType.Tcp, client, null, buffer, endpoint, stream, cancellationToken);
        }
    }
}
