using Mikodev.Links.Internal;
using Mikodev.Links.Internal.Messages;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Abstractions
{
    internal interface INetwork
    {
        void RegisterHandler(string path, Func<IRequest, Task> func);

        Task SendAsync(ContractProfile profile, NotifyMessage message, string path, object data);

        Task BroadcastAsync(string path, object data);

        Task<T> ConnectAsync<T>(string path, object data, IPEndPoint endpoint, CancellationToken token, Func<Stream, Task<T>> func);
    }
}
