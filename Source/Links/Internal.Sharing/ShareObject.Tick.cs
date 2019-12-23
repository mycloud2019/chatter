using System;

namespace Mikodev.Links.Internal.Sharing
{
    internal abstract partial class ShareObject
    {
        private struct Tick
        {
            public TimeSpan TimeSpan;

            public long Position;

            public double Speed;
        }
    }
}
