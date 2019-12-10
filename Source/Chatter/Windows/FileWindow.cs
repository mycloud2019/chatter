using Chatter.Internal;
using Mikodev.Links.Sharing;
using System;
using System.Collections.Generic;

namespace Chatter.Windows
{
    public sealed class FileWindow : ShareWindow
    {
        private readonly FileObject fileObject;

        private readonly List<(double progress, double speed)> values = new List<(double, double)>();

        public FileWindow(FileObject fileObject) : base(fileObject)
        {
            this.fileObject = fileObject;
        }

        protected override void OnUpdate(string propertyName)
        {
            base.OnUpdate(propertyName);

            UpdateNotice($@"{Extensions.ToUnit(fileObject.Position)} / {Extensions.ToUnit(fileObject.Length)}, {100.0 * fileObject.Progress:0.00}%, {fileObject.Remaining:hh\:mm\:ss}, {fileObject.Status}");
            if (propertyName != nameof(FileObject.Progress) && (fileObject.Status & ShareStatus.Completed) == 0)
                return;

            var count = values.Count;
            if (count > 1 && Math.Abs(values[count - 1].progress - values[count - 2].progress) < 0.002)
                values.RemoveAt(count - 1);
            values.Add((fileObject.Progress, fileObject.Speed));
            UpdateGraphics(values);
        }
    }
}
