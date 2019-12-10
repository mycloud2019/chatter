using System;

namespace Mikodev.Links.Messages
{
    public class MessageEventArgs : EventArgs
    {
        public LinkClient Client { get; }

        public LinkProfile Profile { get; }

        public Message Message { get; }

        public bool IsHandled { get; set; } = false;

        public MessageEventArgs(LinkClient client, LinkProfile profile, Message message)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}
