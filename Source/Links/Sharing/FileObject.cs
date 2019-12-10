using System;
using System.IO;

namespace Mikodev.Links.Sharing
{
    public abstract class FileObject : ShareObject
    {
        private double progress;

        private TimeSpan remaining;

        public long Length { get; }

        public double Progress
        {
            get => progress;
            private set => OnPropertyChange(ref progress, value);
        }

        public TimeSpan Remaining
        {
            get => remaining;
            set => OnPropertyChange(ref remaining, value);
        }

        protected FileObject(LinkClient client, LinkProfile profile, Stream stream, long length) : base(client, profile, stream)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            Length = length;
        }

        protected override void Report()
        {
            base.Report();

            Progress = Length == 0
                ? Status == ShareStatus.Success ? 1.0 : 0
                : 1.0 * Position / Length;
            Remaining = Speed < 1 || (Status & ShareStatus.Completed) != 0 ? default : TimeSpan.FromSeconds((Length - Position) / Speed);
        }
    }
}
