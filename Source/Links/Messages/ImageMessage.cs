using Mikodev.Links.Annotations;
using System;

namespace Mikodev.Links.Messages
{
    internal sealed class ImageMessage : NotifyMessage
    {
        public const string MessagePath = "message.image-hash";

        public string ImageHash { get; set; }

        public ImageMessage() : base(path: MessagePath) { }

        public ImageMessage(string messageId) : base(messageId, path: MessagePath) { }

        public ImageMessage(string messageId, DateTime dateTime, MessageReference reference) : base(messageId, path: MessagePath, dateTime, reference) { }
    }
}
