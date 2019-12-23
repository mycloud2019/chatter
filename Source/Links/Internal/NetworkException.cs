using System;
using System.Runtime.Serialization;

namespace Mikodev.Links.Internal
{
    [Serializable]
    internal class NetworkException : Exception
    {
        public NetworkError ErrorCode { get; }

        public NetworkException(NetworkError error) : this(error, GetMessage(error)) { }

        public NetworkException(NetworkError error, string message) : base(message) => ErrorCode = error;

        protected NetworkException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ErrorCode = (NetworkError)info.GetInt32(nameof(ErrorCode));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ErrorCode), (int)ErrorCode);
            base.GetObjectData(info, context);
        }

        private static string GetMessage(NetworkError error) => error switch
        {
            NetworkError.InvalidData => "Invalid data!",
            NetworkError.InvalidHost => "Invalid host!",
            NetworkError.UdpPacketTooLarge => "Udp packet too large!",
            _ => "Undefined error!",
        };
    }
}
