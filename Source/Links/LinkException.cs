using System;
using System.Runtime.Serialization;

namespace Mikodev.Links
{
    [Serializable]
    public class LinkException : Exception
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

        private static string GetMessage(LinkError error)
        {
            switch (error)
            {
                case LinkError.InvalidData:
                    return "Invalid data!";

                case LinkError.InvalidHost:
                    return "Invalid host!";

                case LinkError.UdpPacketTooLarge:
                    return "Udp packet too large!";

                default:
                    return "Undefined error!";
            }
        }
    }
}
