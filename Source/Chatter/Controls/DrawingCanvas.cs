using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace Chatter.Controls
{
    public sealed class DrawingCanvas : Canvas
    {
        private readonly List<Visual> visuals = new List<Visual>();

        protected override int VisualChildrenCount => visuals.Count;

        protected override Visual GetVisualChild(int index) => visuals[index];

        public void AddVisual(Visual visual)
        {
            if (visual == null)
                throw new ArgumentNullException(nameof(visual));
            Dispatcher.VerifyAccess();

            if (visuals.Contains(visual))
                throw new ArgumentException();
            AddVisualChild(visual);
            AddLogicalChild(visual);
            visuals.Add(visual);
        }

        public bool RemoveVisual(Visual visual)
        {
            if (visual == null)
                throw new ArgumentNullException(nameof(visual));
            Dispatcher.VerifyAccess();

            if (!visuals.Remove(visual))
                return false;
            RemoveVisualChild(visual);
            RemoveLogicalChild(visual);
            return true;
        }

        public void ClearVisuals()
        {
            Dispatcher.VerifyAccess();

            visuals.ForEach(RemoveVisualChild);
            visuals.ForEach(RemoveLogicalChild);
            visuals.Clear();
        }
    }
}
