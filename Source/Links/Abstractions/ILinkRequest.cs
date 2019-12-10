using Mikodev.Links.Abstractions.Models;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Abstractions
{
    internal interface ILinkRequest
    {
        CancellationToken CancellationToken { get; }

        ConnectionType ConnectionType { get; }

        LinkPacket Packet { get; }

        Stream Stream { get; }

        IPAddress Address { get; }

        LinkProfile SenderProfile { get; }

        Task ResponseAsync(object data);
    }
}
