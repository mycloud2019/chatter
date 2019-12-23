using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Optional;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Mikodev.Optional.Extensions;

namespace Mikodev.Links.Internal.Sharing
{
    internal abstract partial class ShareObject : ISharingObject, IDisposable
    {
        private const int None = 0, Started = 1, Disposed = 2;

        private const int TickLimits = 30;

        private static readonly TimeSpan updateDelay = TimeSpan.FromMilliseconds(67);

        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        private readonly List<Tick> ticks = new List<Tick>();

        private readonly Client client;

        private readonly NotifySharingViewer viewer;

        private int interStatus = None;

        private long interPosition;

        protected Stream Stream { get; }

        protected CancellationToken CancellationToken { get; }

        internal IGenerator Generator => client.Generator;

        internal Configurations Configurations => client.Configurations;

        public SharingViewer Viewer => viewer;

        protected ShareObject(IClient client, Stream stream, NotifySharingViewer sharingViewer)
        {
            CancellationToken = cancellation.Token;
            this.client = (Client)client ?? throw new ArgumentNullException(nameof(client));
            this.viewer = sharingViewer ?? throw new ArgumentNullException(nameof(sharingViewer));
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        protected async Task SetStatus(SharingStatus status)
        {
            await client.Dispatcher.InvokeAsync(() =>
            {
                Debug.Assert((Viewer.Status & SharingStatus.Completed) == 0);
                if (status == SharingStatus.Running)
                {
                    Debug.Assert(Viewer.Status != SharingStatus.Running);
                    Debug.Assert(ticks.Count == 0);
                    ticks.Add(new Tick { TimeSpan = stopwatch.Elapsed });
                }
                viewer.SetStatus(status);
            });
        }

        internal Task LoopAsync()
        {
            if (Interlocked.CompareExchange(ref interStatus, Started, None) != None)
                throw new InvalidOperationException();
            return Task.Run(MainLoopAsync);
        }

        private async Task MainLoopAsync()
        {
            var invoke = this is IShareReceiver
                ? Task.Run(GetAsync)
                : Task.Run(PutAsync);
            do
                await client.Dispatcher.InvokeAsync(Report);
            while (await Task.WhenAny(invoke, Task.Delay(updateDelay, CancellationToken)) != invoke);

            var result = await TryAsync(invoke);
            var status = result.IsOk() ? SharingStatus.Success : SharingStatus.Aborted;
            await client.Dispatcher.InvokeAsync(() => { if ((Viewer.Status & SharingStatus.Completed) == 0) viewer.SetStatus(status); });
            await client.Dispatcher.InvokeAsync(Report);
        }

        private async Task PutAsync()
        {
            var buffer = await Stream.ReadBlockWithHeaderAsync(Configurations.TcpBufferLimits, CancellationToken);
            var data = new Token(Generator, buffer);
            var dictionary = (IReadOnlyDictionary<string, Token>)data;
            var result = dictionary.TryGetValue("status", out var item) ? item.As<string>() : null;
            await SetStatus(SharingStatus.Running);

            if (result == "ok")
                await InvokeAsync();
            else if (result == "refused")
                await SetStatus(SharingStatus.Refused);
            else
                throw new NetworkException(NetworkError.InvalidData);
        }

        private async Task GetAsync()
        {
            var accept = await ((IShareReceiver)this).WaitForAcceptAsync();
            var buffer = Generator.Encode(new { status = accept ? "ok" : "refused" });
            await Stream.WriteWithHeaderAsync(buffer, CancellationToken);

            if (accept)
            {
                var (name, path) = FindAvailableName();
                await client.Dispatcher.InvokeAsync(() => viewer.SetName(name));
                await client.Dispatcher.InvokeAsync(() => viewer.SetFullName(path));
                await SetStatus(SharingStatus.Running);
                await InvokeAsync();
            }
            else
            {
                await SetStatus(SharingStatus.Refused);
            }
        }

        private (string name, string fullName) FindAvailableName()
        {
            var container = new DirectoryInfo(Configurations.ShareDirectory);
            if (container.Exists == false)
                container.Create();
            var name = Viewer.Name;
            var tail = Path.GetExtension(name);
            var head = Path.GetFileNameWithoutExtension(name);
            for (var i = 0; i < 16; i++)
            {
                var path = Path.Combine(container.FullName, name);
                if (!File.Exists(path) && !Directory.Exists(path))
                    return (name, path);
                name = $"{head}-{DateTime.Now:yyyyMMdd-HHmmss}-{i}{tail}";
            }
            throw new IOException("File name or directory name duplicated!");
        }

        protected abstract Task InvokeAsync();

        protected virtual void Report()
        {
            var position = interPosition;
            viewer.SetPosition(position);
            if (ticks.Count == 0)
                return;

            var timeSpan = stopwatch.Elapsed;
            var tick = new Tick { TimeSpan = stopwatch.Elapsed, Position = position };
            var last = ticks.Last();
            var speed = (tick.Position - last.Position) / (tick.TimeSpan - last.TimeSpan).TotalSeconds;
            tick.Speed = speed;

            ticks.Add(tick);
            var count = ticks.Count - TickLimits;
            if (count > 0)
                ticks.RemoveRange(0, count);
            viewer.SetSpeed(ticks.Average(x => x.Speed));
        }

        protected async Task PutFileAsync(string path, long length)
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var buffer = new byte[Configurations.TcpBufferLength];

            try
            {
                if (stream.Length != length)
                    throw new IOException("File length not match! May have been modified by another application.");
                while (length > 0)
                {
                    var result = await stream.ReadAsync(buffer, 0, (int)Math.Min(length, buffer.Length), CancellationToken);
                    await Stream.WriteAsync(buffer, 0, result, CancellationToken);
                    length -= result;
                    interPosition += result;
                }
                await stream.FlushAsync();
            }
            finally
            {
                stream.Dispose();
            }
        }

        protected async Task GetFileAsync(string path, long length)
        {
            if (length < 0)
                throw new IOException("Invalid file length!");
            var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            var buffer = new byte[Configurations.TcpBufferLength];

            try
            {
                while (length > 0)
                {
                    var result = (int)Math.Min(length, buffer.Length);
                    await Stream.ReadBlockAsync(buffer, 0, result, CancellationToken);
                    await stream.WriteAsync(buffer, 0, result);
                    length -= result;
                    interPosition += result;
                }
                await stream.FlushAsync();
            }
            catch (Exception)
            {
                stream.Dispose();
                File.Delete(path);
                throw;
            }
            finally
            {
                stream.Dispose();
            }
        }

        public void Dispose()
        {
            if (Volatile.Read(ref interStatus) == Disposed)
                return;
            cancellation.Dispose();
            Stream.Dispose();
            Volatile.Write(ref interStatus, Disposed);
        }
    }
}
