using Mikodev.Links.Messages;
using System;
using System.Collections.ObjectModel;
using System.Net;

namespace Mikodev.Links
{
    public sealed class LinkProfile : NotifyProfile
    {
        public LinkProfileType ProfileType { get; }

        public DateTime LastOnlineDateTime { get; set; }

        public int TcpPort { get; set; }

        public int UdpPort { get; set; }

        public string ImageHash { get; set; }

        public string RemoteImageHash { get; set; }

        public ObservableCollection<Message> MessageCollection { get; } = new ObservableCollection<Message>();

        public IPEndPoint GetTcpEndPoint() => new IPEndPoint(IPAddress, TcpPort);

        public IPEndPoint GetUdpEndPoint() => new IPEndPoint(IPAddress, UdpPort);

        public LinkProfile(string profileId, LinkProfileType profileType) : base(profileId) { ProfileType = profileType; }

        public override string ToString() => $"{nameof(LinkProfile)}(Id: {ProfileId}, Name: {Name})";
    }
}
