using System;
using System.Collections.ObjectModel;
using System.Net;

namespace Mikodev.Links.Internal.Implementations
{
    internal sealed class NotifyContractProfile : NotifyPropertyProfile
    {
        private string imageHash;

        public DateTime LastOnlineDateTime { get; set; }

        public int TcpPort { get; set; }

        public int UdpPort { get; set; }

        public string ImageHash { get => imageHash; set => NotifyProperty(ref this.imageHash, value); }

        public string RemoteImageHash { get; set; }

        public ObservableCollection<NotifyPropertyMessage> MessageCollection { get; } = new ObservableCollection<NotifyPropertyMessage>();

        public IPEndPoint GetTcpEndPoint() => new IPEndPoint(IPAddress, TcpPort);

        public IPEndPoint GetUdpEndPoint() => new IPEndPoint(IPAddress, UdpPort);

        public NotifyContractProfile(string profileId) : base(profileId) { }

        public override string ToString() => $"{nameof(NotifyContractProfile)}(Id: {ProfileId}, Name: {Name})";
    }
}
