namespace Mikodev.Links.Messages.Implementations
{
    public sealed class ImageMessage : Message
    {
        private string imagePath;

        public string ImageHash { get; internal set; }

        public string ImagePath
        {
            get => imagePath;
            internal set => OnPropertyChange(ref imagePath, value);
        }

        public override string Path => "message.image-hash";

        public ImageMessage() : base() { }

        public ImageMessage(string messageId) : base(messageId) { }
    }
}
