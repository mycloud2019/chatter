using Mikodev.Links.Abstractions;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Chatter.Implementations
{
    public class SynchronizationUIContext : ILinkUIContext
    {
        private readonly Dispatcher dispatcher;

        private readonly TaskScheduler scheduler;

        public SynchronizationUIContext(TaskScheduler scheduler, Dispatcher dispatcher)
        {
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public Task InvokeAsync(Action action) => Task.Factory.StartNew(action, default, TaskCreationOptions.None, scheduler);

        public void VerifyAccess() => dispatcher.VerifyAccess();
    }
}
