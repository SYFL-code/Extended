using BepInEx.Logging;
using Kittehface.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionLib
{
	public static class Log
	{
		//public static ManualLogSource? log { get; private set; }
		private static ManualLogSource? log;

		private static bool EnableLog { get; set; } = true;
		private static LogLevel CurrentLevel { get; set; } = LogLevel.Debug;

		public static void SetLog(ManualLogSource logger) => log = logger;

		//public static void LogInfo(object obj) => Log_(LogLevel.Info, obj);
		public static void LogInfo(object obj, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") =>
			Log_(LogLevel.Info, obj, caller, filePath);
		public static void LogWarning(object obj, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") =>
			Log_(LogLevel.Warning, obj, caller, filePath);
		public static void LogError(object obj, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") =>
			Log_(LogLevel.Error, obj, caller, filePath);
		public static void LogFatal(object obj, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") =>
			Log_(LogLevel.Fatal, obj, caller, filePath);
		public static void LogDebug(object obj, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") =>
			Log_(LogLevel.Debug, obj, caller, filePath);
		public static void LogMessage(object obj, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "") =>
			Log_(LogLevel.Message, obj, caller, filePath);

		public static object MessageLog(object obj, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "")
		{
			if (obj is string s)
			{
				string className = Path.GetFileNameWithoutExtension(filePath);
				obj = $"[{className}.{caller}] {s}";
			}
			return obj;
		}
		public static string MessageLog(this string obj, [CallerMemberName] string caller = "", [CallerFilePath] string filePath = "")
		{
			if (obj is string s)
			{
				string className = Path.GetFileNameWithoutExtension(filePath);
				obj = $"[{className}.{caller}] {s}";
			}
			return obj;
		}

		private static void Log_(LogLevel level, object obj, string caller = "", string filePath = "")
		{
			if (log == null) return;

			if (!ShouldLog(level)) return;

			if (obj is string s)
			{
				string className = Path.GetFileNameWithoutExtension(filePath);
				obj = $"[{Plugin.version}]-[{className}.{caller}] {s}";
			}

			switch (level)
			{
				case LogLevel.Fatal:
					log.LogFatal(obj);
					break;
				case LogLevel.Error:
					log.LogError(obj);
					break;
				case LogLevel.Warning:
					log.LogWarning(obj);
					break;
				case LogLevel.Info:
					log.LogInfo(obj);
					break;
				case LogLevel.Debug:
					log.LogDebug(obj);
					break;
				case LogLevel.Message:
					log.LogMessage(obj);
					break;
				default:
					log.LogInfo(obj);
					break;
			}

		}

		private enum LogLevel
		{
			None = 0,
			Message = 0,
			Fatal = 1,
			Error = 2,
			Warning = 3,
			Info = 4,
			Debug = 5
		}

		private static bool ShouldLog(LogLevel Level)
		{
			if (!EnableLog) return false;

			return CurrentLevel >= Level;
		}

		#region LogLevel
		// Fatal 致命
		// Error 错误
		// Warning 警告
		// Message 消息
		// Info 信息
		// Debug 调试

		/*// 摘要:
		//     The level, or severity of a log entry.
		[Flags]
		public enum LogLevel_
		{
			//
			// 摘要:
			//     No level selected.
			None = 0,
			//
			// 摘要:
			//     A fatal error has occurred, which cannot be recovered from.
			Fatal = 1,
			//
			// 摘要:
			//     An error has occured, but can be recovered from.
			Error = 2,
			//
			// 摘要:
			//     A warning has been produced, but does not necessarily mean that something wrong
			//     has happened.
			Warning = 4,
			//
			// 摘要:
			//     An important message that should be displayed to the user.
			Message = 8,
			//
			// 摘要:
			//     A message of low importance.
			Info = 0x10,
			//
			// 摘要:
			//     A message that would likely only interest a developer.
			Debug = 0x20,
			//
			// 摘要:
			//     All log levels.
			All = 0x3F
		}*/
		#endregion
	}




	public static class Logs
	{
		private static readonly string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

		public static void Write(string msg, bool other = true)
		{
			if (!other)
			{
                File.AppendAllText(path, $"{msg}\n");
				return;
            }

			File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{Plugin.version}] {msg}\n");
		}
	}


}
