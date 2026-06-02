using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using static Extended.Plugin;

namespace Extended
{
	public static class Data
	{
		private static JsonSerializerSettings _settings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,// 可读性好，方便调试
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore// 防止意外循环引用
        };

		public static string Save()
		{
			// 自动序列化所有公开属性和字段
			return JsonConvert.SerializeObject(GlobalVar.playerVars, _settings);
		}

		public static void Load(string data)
		{
			ClearAll();

			if (string.IsNullOrEmpty(data)) return;

			try
			{
				var loaded = JsonConvert.DeserializeObject<Dictionary<int, PlayerVar>>(data, _settings);
				if (loaded != null)
				{
					foreach (var kvp in loaded)
					{
						GlobalVar.playerVars[kvp.Key] = kvp.Value;// playerRef = null
					}
				}
			}
			catch (JsonException ex)
			{
				Log.LogError(ex);

				ClearAll();
			}
		}

		public static void ClearAll()
		{
			GlobalVar.playerVars.Clear();
		}

	}
}