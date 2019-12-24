using Chatter.Internal;
using Mikodev.Links.Abstractions;
using System;
using System.Collections.Generic;

namespace Chatter.Windows
{
    public sealed class FileWindow : SharingWindow
    {
        private readonly SharingViewer viewer;

        private readonly List<(double progress, double speed)> values = new List<(double, double)>();

        public FileWindow(ISharingFileObject fileObject) : base(fileObject)
        {
            this.viewer = fileObject.Viewer;
        }

        protected override void OnUpdate(string propertyName)
        {
            base.OnUpdate(propertyName);
            UpdateNotice($@"{Extensions.ToUnit(viewer.Position)} / {Extensions.ToUnit(viewer.Length)}, {100.0 * viewer.Progress:0.00}%, {viewer.Remaining:hh\:mm\:ss}, {viewer.Status}");
            if (propertyName != nameof(SharingViewer.Progress) && (viewer.Status & SharingStatus.Completed) == 0)
                return;
            var count = values.Count;
            if (count > 1 && Math.Abs(values[count - 1].progress - values[count - 2].progress) < 0.002)
                values.RemoveAt(count - 1);
            values.Add((viewer.Progress, viewer.Speed));
            UpdateGraphics(values);
        }
    }
}
