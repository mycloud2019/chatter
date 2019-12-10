using Mikodev.Links.Abstractions;
using Mikodev.Links.Abstractions.Models;
using Mikodev.Links.Internal;
using Mikodev.Tasks.Abstractions;
using Mikodev.Tasks.TaskCompletionManagement;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Implementations
{
    internal sealed class LinkCache : ILinkCache, IDisposable
    {
        private const int BufferLength = 4096;

        private const int BufferLimits = 16 * 1024 * 1024;

        private readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(15);

        private readonly ILinkNetwork network;

        private readonly ITaskCompletionManager<string, FileInfo> completionManager = new TaskCompletionManager<string, FileInfo>();

        private readonly string cachepath;

        private readonly string extension = ".png";

        internal LinkCache(LinkEnvironment environment, ILinkNetwork network)
        {
            Debug.Assert(environment != null);
            Debug.Assert(network != null);
            this.network = network;
            cachepath = Path.GetFullPath(environment.CacheDirectory);
            network.RegisterHandler("link.get-cache", HandleCacheAsync);
        }

        private async Task<byte[]> CacheToFileAsync(Stream stream, string filename, CancellationToken token)
        {
            var directoryPath = Path.GetDirectoryName(filename);
            if (Directory.Exists(directoryPath) == false)
                _ = Directory.CreateDirectory(directoryPath);
            using (var filestream = new FileStream(filename, FileMode.CreateNew))
            using (var md5 = MD5.Create())
            using (var cryptoStream = new CryptoStream(filestream, md5, CryptoStreamMode.Write))
            {
                var length = 0L;
                var buffer = new byte[BufferLength];
                while (true)
                {
                    var result = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (result < 1)
                        break;
                    length += result;
                    if (length > BufferLimits)
                        throw new InvalidOperationException();
                    await cryptoStream.WriteAsync(buffer, 0, result, token);
                }
                cryptoStream.FlushFinalBlock();
                return md5.Hash;
            }
        }

        private async Task<HashInfo> CacheStreamAsync(Stream stream, CancellationToken token)
        {
            var hash = default(string);
            var fullpath = default(string);
            var filename = Path.Combine(cachepath, $"cache@{Guid.NewGuid():N}");
            try
            {
                var buffer = await CacheToFileAsync(stream, filename, token);
                hash = BitConverter.ToString(buffer).Replace("-", string.Empty).ToLowerInvariant();
                fullpath = GetHashPath(hash);
                File.Move(filename, fullpath);
            }
            catch
            {
                if (File.Exists(filename))
                    File.Delete(filename);
                if (fullpath == null || File.Exists(fullpath) == false)
                    throw;
            }
            return new HashInfo(hash, new FileInfo(fullpath));
        }

        private string GetHashPath(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentException();
            return Path.Combine(cachepath, hash + extension);
        }

        /// <summary>
        /// 向目标请求指定 Hash 的缓存, 返回本地缓存路径 (若 Hash 不匹配, 抛出异常)
        /// </summary>
        private async Task<FileInfo> RequestAsync(string hash, IPEndPoint endpoint, CancellationToken token)
        {
            Debug.Assert(!string.IsNullOrEmpty(hash));
            Debug.Assert(endpoint != null);

            var data = new { hash };
            return await network.ConnectAsync("link.get-cache", data, endpoint, token, async stream =>
            {
                var header = new byte[sizeof(int)];
                await stream.ReadBlockAsync(header, token);
                var length = BitConverter.ToInt32(header, 0);
                if (length < 0)
                    throw new InvalidOperationException();
                var result = await CacheStreamAsync(stream, token);
                if (result.Hash != hash)
                    throw new InvalidOperationException();
                return result.FileInfo;
            });
        }

        public async Task<HashInfo> SetCacheAsync(FileInfo fileInfo, CancellationToken token)
        {
            using (var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                return await CacheStreamAsync(stream, token);
        }

        public bool TryGetCache(string hash, out FileInfo fileInfo)
        {
            var path = GetHashPath(hash);
            var flag = File.Exists(path);
            fileInfo = flag ? new FileInfo(path) : null;
            return flag;
        }

        public async Task<FileInfo> GetCacheAsync(string hash, IPEndPoint endpoint, CancellationToken token)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentException("File hash can not be null or empty!", nameof(hash));
            if (endpoint is null)
                throw new ArgumentNullException(nameof(endpoint));
            if (TryGetCache(hash, out var fullpath))
                return fullpath;
            var task = completionManager.Create(requestTimeout, hash, out var created, token);
            if (created)
                _ = Task.Run(() => RequestAsync(hash, endpoint, token)).ContinueWith(x => completionManager.SetResult(hash, x.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
            return await task;
        }

        public void Dispose()
        {
            (completionManager as IDisposable)?.Dispose();
        }

        internal async Task HandleCacheAsync(ILinkRequest parameter)
        {
            var data = parameter.Packet.Data;
            var hash = data["hash"].As<string>();
            var path = GetHashPath(hash);
            var stream = parameter.Stream;
            using (var filestream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var length = filestream.Length;
                if (length > BufferLimits)
                    throw new InvalidOperationException();
                var header = BitConverter.GetBytes((int)length);
                await stream.WriteAsync(header, 0, header.Length, parameter.CancellationToken);
                await filestream.CopyToAsync(stream, BufferLength, parameter.CancellationToken);
            }
        }
    }
}
