using Avalonia;
using Mikodev.Optional;
using System;
using System.Threading.Tasks;

namespace Chatter.Viewer.Internal
{
    public static class ResultExtensions
    {
        public static async Task<Result<T, Exception>> NoticeOnErrorAsync<T>(this Task<Result<T, Exception>> task)
        {
            var result = await task;
            if (result.IsError())
                await Notice.ShowDialog(Application.Current.MainWindow, result.UnwrapError().Message, "Error");
            return result;
        }
    }
}
