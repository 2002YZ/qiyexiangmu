using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Log;

namespace FolderKeeper
{

    ////文件夹管理，by Kimi 20251020
    ///帮我用C#写一个文件夹空间管理的类 包含以下功能：
    //1.监控文件夹内的文件数量
    //2.当文件夹内的文件数量超过一定数值时，文件夹内每增加一个文件，就删除一个日期最久的文件，保持文件夹的文件数量不变
    //3.要求对硬盘的占用尽量少
    /// 

    /// <summary>
    /// 文件夹“定量保鲜”管理器（C# 7.3 兼容版）。
    /// 文件数 > quota 后，每新增 1 个文件就删除最旧的 1 个文件。
    /// </summary>
    public sealed class FolderSpaceKeeper : IDisposable
    {
        private readonly DirectoryInfo _folder;
        private readonly int _quota;
        private readonly FileSystemWatcher _watcher;
        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);
        private readonly Queue<FileInfo> _queue = new Queue<FileInfo>();



        public FolderSpaceKeeper(string folderPath, int quota)
        {
            if (quota <= 0) throw new ArgumentOutOfRangeException(nameof(quota));
            _folder = new DirectoryInfo(folderPath);
            if (!_folder.Exists) _folder.Create();
            _quota = quota;

            // 初始化队列，按 CreationTimeUtc 升序
            foreach (var fi in _folder.EnumerateFiles()
                                     .OrderBy(f => f.CreationTimeUtc)
                                     .Take(_quota))
                _queue.Enqueue(fi);

            _watcher = new FileSystemWatcher(_folder.FullName)
            {
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName,
                Filter = "*.*",
                IncludeSubdirectories = false
            };
            _watcher.Created += OnCreated;
            _watcher.EnableRaisingEvents = true;
        }

        /// <summary>当前文件数</summary>
        public int CurrentCount
        {
            get { lock (_queue) return _queue.Count; }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            // 异步但不阻塞，兼容 .NET 4.7.2
            Task.Run(async () =>
            {
                try { await HandleNewFileAsync(e.FullPath); }
                catch(Exception ex)
                { LogRecord.addLog("文件夹管理模块异常" + ex.Message); }
            });
        }



        private async Task HandleNewFileAsync(string fullPath)
        {
            FileInfo fi = null;
            // 简单重试，等待文件落盘
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    fi = new FileInfo(fullPath);
                    if (fi.Exists) 
                        break;
                }
                catch { /* ignore */ }
                await Task.Delay(100).ConfigureAwait(false);
            }
            if (fi == null || !fi.Exists) return;

            await _locker.WaitAsync().ConfigureAwait(false);
            try
            {
                lock (_queue) _queue.Enqueue(fi);

                if (_queue.Count > _quota)
                {
                    FileInfo oldest;
                    lock (_queue) oldest = _queue.Dequeue();
                    await SafeDeleteAsync(oldest).ConfigureAwait(false);
                }
            }
            finally { _locker.Release(); }
        }

        /// <summary>
        /// 先截断为 0 字节再删除，降低 SSD 写入放大。
        /// </summary>
        private static async Task SafeDeleteAsync(FileInfo file)
        {
            try
            {
                using (var fs = file.Open(FileMode.Truncate, FileAccess.Write, FileShare.None))
                { /* 截断 */ }
                file.Refresh();
                file.Delete();
            }
            catch { /* 忽略删除失败 */ }
            await Task.CompletedTask; // 保持签名一致
        }

        public void Dispose()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _locker.Dispose();
        }


        public static (string First, string Second) GetTwoMostRecentFiles(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                throw new ArgumentException("文件夹路径不能为空");

            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"目录不存在: {folderPath}");

            // 使用优先队列维护最近的两个文件
            var recentFiles = new SortedSet<FileInfo>(Comparer<FileInfo>.Create((f1, f2) => f2.LastWriteTime.CompareTo(f1.LastWriteTime)));

            // 高效遍历所有文件
            foreach (string filePath in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (recentFiles.Count < 2)
                    {
                        recentFiles.Add(fileInfo);
                    }
                    else
                    {
                        // 比较当前文件与队列中最旧的文件
                        if (fileInfo.LastWriteTime > recentFiles.Max.LastWriteTime)
                        {
                            recentFiles.Remove(recentFiles.Max);
                            recentFiles.Add(fileInfo);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // 忽略无访问权限的文件
                    continue;
                }
                catch (IOException)
                {
                    // 忽略无法访问的文件（如正在被占用的文件）
                    continue;
                }
            }

            // 按时间排序返回结果
            return recentFiles.Count >= 2
                ? (recentFiles.Max.FullName, recentFiles.Min.FullName)
                : (recentFiles.Max.FullName, null);
        }

        //遍历文件夹下所有的文件，并返回修改时间最近的文件路径
        public static string GetMostRecentlyModifiedFile(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                throw new ArgumentException("文件夹路径不能为空");

            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"目录不存在: {folderPath}");

            DateTime latestWriteTime = DateTime.MinValue;
            string latestFilePath = null;

            // 使用Directory.EnumerateFiles进行高效遍历
            foreach (string filePath in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.LastWriteTime > latestWriteTime)
                    {
                        latestWriteTime = fileInfo.LastWriteTime;
                        latestFilePath = fileInfo.FullName;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // 忽略无访问权限的文件
                    continue;
                }
                catch (IOException)
                {
                    // 忽略无法访问的文件（如正在被占用的文件）
                    continue;
                }
            }

            return latestFilePath;
        }


    }
}