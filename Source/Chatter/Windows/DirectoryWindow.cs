using Chatter.Internal;
using Mikodev.Links.Sharing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Chatter.Windows
{
    public sealed class DirectoryWindow : ShareWindow
    {
        private readonly DirectoryObject directoryObject;

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        private readonly List<(double progress, double speed)> values = new List<(double, double)>();

        private readonly List<(TimeSpan timeSpan, double speed)> history = new List<(TimeSpan, double)>();

        public DirectoryWindow(DirectoryObject directoryObject) : base(directoryObject)
        {
            this.directoryObject = directoryObject;
        }

        protected override void OnUpdate(string propertyName)
        {
            base.OnUpdate(propertyName);

            UpdateNotice($"{Extensions.ToUnit(directoryObject.Position)}, {directoryObject.Status}");
            if (propertyName != nameof(DirectoryObject.Speed) && (directoryObject.Status & ShareStatus.Completed) == 0)
                return;

            var count = history.Count;
            if (count > 1 && history[count - 1].timeSpan - history[count - 2].timeSpan < TimeSpan.FromMilliseconds(33))
                history.RemoveAt(count - 1);
            var limits = TimeSpan.FromSeconds(30);
            var timeSpan = stopwatch.Elapsed;
            var standard = timeSpan - limits;
            history.Add((timeSpan, directoryObject.Speed));
            var first = history.FirstOrDefault(x => x.timeSpan > standard);
            var index = history.IndexOf(first);
            if (index > 0)
                history.RemoveRange(0, index);
            values.Clear();
            var offset = first.timeSpan;
            var total = timeSpan - first.timeSpan;
            if (total > limits)
                total = limits;
            foreach (var (span, speed) in history)
                values.Add((1.0 * (span - first.timeSpan).TotalMilliseconds / total.TotalMilliseconds, speed));
            UpdateGraphics(values);
        }
    }
}
