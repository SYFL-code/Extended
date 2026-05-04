using System;
using System.IO;
using System.Text;
using UnityEngine;


namespace Extended
{
    public static class UDebug
    {
        public static bool EnableDebug = true;   // 默认开启，方便开发阶段
        public static bool LogReset = true;    // 启动时重置日志文件

        private static readonly object _lock = new object();
        private static readonly string _logFilePath = Path.Combine(Application.persistentDataPath, "Extended.txt");
        private static bool _isInitialized = false;

        private static readonly StringBuilder _buffer = new StringBuilder();
        private static DateTime _lastFlush = DateTime.Now;

        // 是否允许输出日志
        public static bool DebugMode =>
            EnableDebug;// || (MyOptions.Instance?.DebugMode?.Value ?? false);

        public static void Log(object message) => UnityEngine.Debug.Log(message);
        public static void Log(string message) => LogInternal(message, UnityEngine.Debug.Log);
        public static void LogWarning(string message) => LogInternal(message, UnityEngine.Debug.LogWarning);
        public static void LogError(string message) => LogInternal(message, UnityEngine.Debug.LogError);
        public static void LogException(Exception message)
        {
            if (!DebugMode || message == null) return;

            UnityEngine.Debug.LogException(message);
            UDebug.LogError(message.Message);
        }

        private static void LogInternal(string message, Action<string> unityLogger)
        {
            if (!DebugMode || string.IsNullOrEmpty(message)) return;

            unityLogger(message);
            OutputLog(message);
        }

        public static void OutputLog(string content)
        {
            if (string.IsNullOrEmpty(content)) return;

            lock (_lock)
            {
                _buffer.AppendLine($"{DateTime.Now:HH:mm:ss} {content}");

                // 每秒或缓冲区积累到一定程度刷新一次
                if ((DateTime.Now - _lastFlush).TotalSeconds > 1 || _buffer.Length > 5000)
                {
                    FlushBuffer();
                }
            }
        }

        /// <summary>
        /// 立即将缓冲区中的日志写入文件。
        /// 建议在游戏退出、场景卸载等关键时机调用，避免日志丢失。
        /// </summary>
        public static void Flush()
        {
            lock (_lock)
            {
                FlushBuffer();
            }
        }

        private static void FlushBuffer()
        {
            try
            {
                InitializeLogFile();
                File.AppendAllText(_logFilePath, _buffer.ToString());
                _buffer.Clear();
                _lastFlush = DateTime.Now;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"日志写入失败: {ex.Message}");
            }
        }

        private static void InitializeLogFile()
        {
            if (_isInitialized && !LogReset) return;

            var header = LogReset
                ? $"=== 新日志 {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}{Environment.NewLine}"
                : string.Empty;

            if (LogReset || !File.Exists(_logFilePath))
            {
                File.WriteAllText(_logFilePath, header);
                LogReset = false;
            }

            _isInitialized = true;
        }
    }
}