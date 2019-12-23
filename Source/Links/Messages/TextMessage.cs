using Mikodev.Links.Annotations;
using System;

namespace Mikodev.Links.Messages
{
    internal sealed class TextMessage : NotifyMessage
    {
        public const string MessagePath = "message.text";

        public TextMessage() : base(path: MessagePath) { }

        public TextMessage(string messageId) : base(messageId, path: MessagePath) { }

        public TextMessage(string messageId, DateTime dateTime, MessageReference reference) : base(messageId, path: MessagePath, dateTime, reference) { }
    }
}
