using Mikodev.Links.Messages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;

namespace Mikodev.Links
{
    public sealed class LinkProfile : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private string name;

        private string text;

        private DateTime lastOnlineDateTime;

        private LinkProfileStatus profileStatus = LinkProfileStatus.Online;

        private IPAddress address;

        private int tcpPort;

        private int udpPort;

        private int hint;

        private string imagePath;

        private string imageHash;

        #region notify property change

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChange<T>(ref T location, T value, [CallerMemberName] string callerName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(callerName));
            if (EqualityComparer<T>.Default.Equals(location, value))
                return;
            location = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(callerName));
        }

        #endregion

        public string Id { get; }

        public LinkProfileType ProfileType { get; }

        public string Name
        {
            get => name;
            set => OnPropertyChange(ref name, value);
        }

        public string Text
        {
            get => text;
            set => OnPropertyChange(ref text, value);
        }

        public LinkProfileStatus ProfileStatus
        {
            get => profileStatus;
            internal set => OnPropertyChange(ref profileStatus, value);
        }

        public DateTime LastOnlineDateTime
        {
            get => lastOnlineDateTime;
            internal set => OnPropertyChange(ref lastOnlineDateTime, value);
        }

        public IPAddress Address
        {
            get => address;
            internal set => OnPropertyChange(ref address, value);
        }

        public int TcpPort
        {
            get => tcpPort;
            internal set => OnPropertyChange(ref tcpPort, value);
        }

        public int UdpPort
        {
            get => udpPort;
            internal set => OnPropertyChange(ref udpPort, value);
        }

        public string ImagePath
        {
            get => imagePath;
            internal set => OnPropertyChange(ref imagePath, value);
        }

        public string ImageHash
        {
            get => imageHash;
            internal set => OnPropertyChange(ref imageHash, value);
        }

        public int Hint
        {
            get => hint;
            set => OnPropertyChange(ref hint, value);
        }

        public string RemoteImageHash { get; internal set; }

        public BindingList<Message> MessageCollection { get; } = new BindingList<Message>();

        public IPEndPoint GetTcpEndPoint() => new IPEndPoint(address, tcpPort);

        public IPEndPoint GetUdpEndPoint() => new IPEndPoint(address, udpPort);

        public LinkProfile(string clientId, LinkProfileType profileType)
        {
            Id = clientId;
            ProfileType = profileType;
        }

        public override string ToString() => $"{nameof(LinkProfile)}(Id: {Id}, Name: {Name})";
    }
}
