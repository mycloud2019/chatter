using Mikodev.Binary;
using Mikodev.Links.Annotations;
using Mikodev.Links.Internal;
using Mikodev.Optional;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static Mikodev.Optional.Extensions;

namespace Mikodev.Links.Sharing
{
    public abstract partial class ShareObject : IDisposable, INotifyPropertyChanged, INotifyPropertyChanging
    {
        #region const & static readonly
        private const int None = 0, Started = 1, Disposed = 2;

        private const int TickLimits = 30;

        private static readonly TimeSpan updateDelay = TimeSpan.FromMilliseconds(67);
        #endregion

        #region private
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        private readonly List<Tick> ticks = new List<Tick>();

        private readonly LinkClient client;

        private int interStatus = None;

        private string name;

        private string fullName;

        private long interPosition;

        private long position;

        private double speed;

        private ShareStatus shareStatus = ShareStatus.Pending;
        #endregion

        public Profile Profile { get; }

        public string Name
        {
            get => name;
            protected set => OnPropertyChange(ref name, value);
        }

        public string FullName
        {
            get => fullName;
            protected set => OnPropertyChange(ref fullName, value);
        }

        public ShareStatus Status
        {
            get => shareStatus;
            private set => OnPropertyChange(ref shareStatus, value);
        }

        public long Position
        {
            get => position;
            private set => OnPropertyChange(ref position, value);
        }

        public double Speed
        {
            get => speed;
            private set => OnPropertyChange(ref speed, value);
        }

        #region notify property change

        public event PropertyChangedEventHandler PropertyChanged;

        public event PropertyChangingEventHandler PropertyChanging;

        protected void OnPropertyChange<T>(ref T location, T value, [CallerMemberName] string callerName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(callerName));
            if (EqualityComparer<T>.Default.Equals(location, value))
                return;
            location = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(callerName));
        }

        #endregion

        protected Stream Stream { get; }

        protected CancellationToken CancellationToken { get; }

        internal IGenerator Generator => client.Generator;

        internal LinkEnvironment Environment => client.Environment;

        protected ShareObject(Client client, Profile profile, Stream stream)
        {
            CancellationToken = cancellation.Token;
            this.client = (LinkClient)client ?? throw new ArgumentNullException(nameof(client));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        protected async Task SetStatus(ShareStatus status)
        {
            await client.UIContext.InvokeAsync(() =>
            {
                Debug.Assert((Status & ShareStatus.Completed) == 0);
                if (status == ShareStatus.Running)
                {
                    Debug.Assert(Status != ShareStatus.Running);
                    Debug.Assert(ticks.Count == 0);
                    ticks.Add(new Tick { TimeSpan = stopwatch.Elapsed });
                }
                Status = status;
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
                ? Task.Run(ReceiveAsync)
                : Task.Run(SendAsync);

            do
            {
                await client.UIContext.InvokeAsync(Report);
            }
            while (await Task.WhenAny(invoke, Task.Delay(updateDelay, CancellationToken)) != invoke);

            var result = await TryAsync(invoke);
            var status = result.IsOk() ? ShareStatus.Success : ShareStatus.Aborted;
            await client.UIContext.InvokeAsync(() => { if ((Status & ShareStatus.Completed) == 0) Status = status; });
            await client.UIContext.InvokeAsync(Report);
        }

        private async Task SendAsync()
        {
            var buffer = await Stream.ReadBlockWithHeaderAsync(Environment.TcpBufferLimits, CancellationToken);
            var data = new Token(Generator, buffer);
            var dictionary = (IReadOnlyDictionary<string, Token>)data;
            var result = dictionary.TryGetValue("status", out var item) ? item.As<string>() : null;
            await SetStatus(ShareStatus.Running);

            if (result == "ok")
                await InvokeAsync();
            else if (result == "refused")
                await SetStatus(ShareStatus.Refused);
            else
                throw new LinkException(LinkError.InvalidData);
        }

        private async Task ReceiveAsync()
        {
            var accept = await ((IShareReceiver)this).AcceptAsync();
            var buffer = Generator.Encode(new { status = accept ? "ok" : "refused" });
            await Stream.WriteWithHeaderAsync(buffer, CancellationToken);

            if (accept)
            {
                var (name, path) = FindAvailableName();
                await client.UIContext.InvokeAsync(() => Name = name);
                await client.UIContext.InvokeAsync(() => FullName = path);
                await SetStatus(ShareStatus.Running);
                await InvokeAsync();
            }
            else
            {
                await SetStatus(ShareStatus.Refused);
            }
        }

        private (string name, string fullName) FindAvailableName()
        {
            var container = new DirectoryInfo(Environment.ShareDirectory);
            if (container.Exists == false)
                container.Create();
            var name = Name;
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
            Position = position;
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
            Speed = ticks.Average(x => x.Speed);
        }

        protected async Task PutFileAsync(string path, long length)
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var buffer = new byte[Environment.TcpBufferLength];

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

        protected async Task ReceiveFileAsync(string path, long length)
        {
            if (length < 0)
                throw new IOException("Invalid file length!");
            var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            var buffer = new byte[Environment.TcpBufferLength];

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
