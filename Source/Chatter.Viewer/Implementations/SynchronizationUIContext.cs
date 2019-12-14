using Avalonia.Threading;
using Mikodev.Links.Abstractions;
using System;
using System.Threading.Tasks;

namespace Chatter.Viewer.Implementations
{
    internal class SynchronizationUIContext : ILinkUIContext
    {
        private readonly Dispatcher dispatcher;

        public SynchronizationUIContext(Dispatcher dispatcher) => this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        public Task InvokeAsync(Action action) => dispatcher.InvokeAsync(action);

        public void VerifyAccess() => dispatcher.VerifyAccess();
    }
}
