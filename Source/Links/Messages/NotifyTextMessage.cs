using Mikodev.Links.Abstractions;
using System;

namespace Mikodev.Links.Messages
{
    internal sealed class NotifyTextMessage : NotifyMessage
    {
        public const string MessagePath = "message.text";

        public NotifyTextMessage() : base(path: MessagePath) { }

        public NotifyTextMessage(string messageId) : base(messageId, path: MessagePath) { }

        public NotifyTextMessage(string messageId, DateTime dateTime, MessageReference reference) : base(messageId, path: MessagePath, dateTime, reference) { }
    }
}
