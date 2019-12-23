using Mikodev.Links.Messages;
using System;
using System.Collections.ObjectModel;
using System.Net;

namespace Mikodev.Links.Internal
{
    internal sealed class LinkProfile : NotifyProfile
    {
        private string imageHash;

        public LinkProfileType ProfileType { get; }

        public DateTime LastOnlineDateTime { get; set; }

        public int TcpPort { get; set; }

        public int UdpPort { get; set; }

        public string ImageHash { get => imageHash; set => NotifyProperty(ref this.imageHash, value); }

        public string RemoteImageHash { get; set; }

        public ObservableCollection<NotifyMessage> MessageCollection { get; } = new ObservableCollection<NotifyMessage>();

        public IPEndPoint GetTcpEndPoint() => new IPEndPoint(IPAddress, TcpPort);

        public IPEndPoint GetUdpEndPoint() => new IPEndPoint(IPAddress, UdpPort);

        public LinkProfile(string profileId, LinkProfileType profileType) : base(profileId) { ProfileType = profileType; }

        public override string ToString() => $"{nameof(LinkProfile)}(Id: {ProfileId}, Name: {Name})";
    }
}
