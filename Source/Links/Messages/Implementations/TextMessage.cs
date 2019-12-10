namespace Mikodev.Links.Messages.Implementations
{
    public sealed class TextMessage : Message
    {
        public string Text { get; internal set; }

        public override string Path => "message.text";

        public TextMessage() : base() { }

        public TextMessage(string messageId) : base(messageId) { }
    }
}
