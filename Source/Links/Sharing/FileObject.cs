using Mikodev.Links.Abstractions;
using System;
using System.IO;

namespace Mikodev.Links.Sharing
{
    internal abstract class FileObject : ShareObject
    {
        protected FileObject(IClient client, Stream stream, NotifySharingViewer viewer) : base(client, stream, viewer) { }

        protected override void Report()
        {
            base.Report();
            var viewer = (NotifySharingViewer)Viewer;
            viewer.SetProgress(viewer.Length == 0
                ? viewer.Status == SharingStatus.Success ? 1.0 : 0
                : 1.0 * viewer.Position / viewer.Length);
            viewer.SetRemaining(viewer.Speed < 1 || (viewer.Status & SharingStatus.Completed) != 0
                ? default
                : TimeSpan.FromSeconds((viewer.Length - viewer.Position) / viewer.Speed));
        }
    }
}
