using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Abstractions.Models;
using Mikodev.Links.Internal;
using Mikodev.Links.Internal.Implementations;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Implementations
{
    internal sealed class Request : IRequest
    {
        private readonly IPEndPoint endpoint;

        private readonly Client client;

        private readonly Network network;

        public NetworkType NetworkType { get; }

        public Packet Packet { get; }

        public NotifyContractProfile SenderProfile { get; }

        public IPAddress IPAddress => endpoint.Address;

        public Stream Stream { get; }

        public CancellationToken CancellationToken { get; }

        private Request(NetworkType networkType, Client client, Network network, byte[] buffer, IPEndPoint endpoint, Stream stream, CancellationToken cancellationToken)
        {
            Debug.Assert(endpoint != null);
            Debug.Assert(networkType != NetworkType.Tcp || stream != null);
            this.endpoint = endpoint;
            this.client = client;
            this.network = network;

            NetworkType = networkType;
            Stream = stream;
            Packet = new Packet(client.Generator, buffer);
            CancellationToken = cancellationToken;
            SenderProfile = client.Contracts.FindProfile(Packet.SenderId);
        }

        public async Task ResponseAsync(object data)
        {
            if (NetworkType == NetworkType.Tcp)
                await Stream.WriteWithHeaderAsync(client.Generator.Encode(data), CancellationToken);
            else
                await network.ResponseAsync(Packet.PacketId, data, endpoint);
        }

        public static Request CreateUdpParameter(Client client, byte[] buffer, IPEndPoint endpoint, Network network)
        {
            return new Request(NetworkType.Udp, client, network, buffer, endpoint, null, CancellationToken.None);
        }

        public static Request CreateTcpParameter(Client client, byte[] buffer, IPEndPoint endpoint, Stream stream, CancellationToken cancellationToken)
        {
            return new Request(NetworkType.Tcp, client, null, buffer, endpoint, stream, cancellationToken);
        }
    }
}
