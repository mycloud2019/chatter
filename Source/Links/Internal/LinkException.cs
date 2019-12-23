using System;
using System.Runtime.Serialization;

namespace Mikodev.Links
{
    [Serializable]
    internal class LinkException : Exception
    {
        public LinkError ErrorCode { get; }

        public LinkException(LinkError error) : this(error, GetMessage(error)) { }

        public LinkException(LinkError error, string message) : base(message) => ErrorCode = error;

        protected LinkException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ErrorCode = (LinkError)info.GetInt32(nameof(ErrorCode));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ErrorCode), (int)ErrorCode);
            base.GetObjectData(info, context);
        }

        private static string GetMessage(LinkError error) => error switch
        {
            LinkError.InvalidData => "Invalid data!",
            LinkError.InvalidHost => "Invalid host!",
            LinkError.UdpPacketTooLarge => "Udp packet too large!",
            _ => "Undefined error!",
        };
    }
}
