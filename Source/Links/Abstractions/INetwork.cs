﻿using Mikodev.Links.Internal.Implementations;
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

        Task SendAsync(NotifyContractProfile profile, NotifyPropertyMessage message, string path, object data);

        Task BroadcastAsync(string path, object data);

        Task<T> ConnectAsync<T>(string path, object data, IPEndPoint endpoint, CancellationToken token, Func<Stream, Task<T>> func);
    }
}
