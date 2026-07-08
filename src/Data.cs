using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using static ExtensionLib.Plugin;

namespace ExtensionLib
{
	public class Data
	{
		public virtual string id { get; set; } = "Default";


		public virtual string Save()
		{
			return "";
		}

		public virtual void Load(string data)
		{

		}

		public virtual void MalnourishedSave()
		{

		}

		public virtual void ClearAll()
		{
		}


		public virtual string GetFileName(int saveSlot, SlugcatStats.Name slugcat)
		{
			string fileName = $"{id}_{saveSlot}_{slugcat.value}.json";
			return fileName;
		}
		public virtual bool IsMatch(string fileName, int saveSlot)
		{
			if (string.IsNullOrEmpty(fileName))
				return false;

			// 使用字符串操作代替正则（更快）
			string starts = $"{id}_{saveSlot}_";

			return fileName.StartsWith(starts, StringComparison.OrdinalIgnoreCase)
				   && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

			//Regex pattern = new Regex($"^{id}{saveSlot}[^0-9].*\\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			//return pattern.IsMatch(fileName);
		}

	}


	public class LibData : Data
	{
		public static string ID = "ExtensionLib";

        public override string id => ID;// 唯一标识符，确保不会与其他 mod 冲突


		private static JsonSerializerSettings _settings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,// 可读性好，方便调试
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore,// 防止意外循环引用

			ContractResolver = new JsonPropertyOnlyContractResolver()
		};

		public override string Save()
		{
			// 自动序列化所有公开属性和字段
			return JsonConvert.SerializeObject(GlobalVar.playerVars, _settings);
		}

		public override void Load(string data)
		{
			ClearAll();

			if (string.IsNullOrEmpty(data)) return;

			try
			{
				var loaded = JsonConvert.DeserializeObject<Dictionary<string, PlayerVar>>(data, _settings);
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

		public override void MalnourishedSave()
		{
			try
			{
				foreach (var item in GlobalVar.playerVars)
				{
					var pv = item.Value;

					pv.Malnourished_Save();
				}
			}
			catch (JsonException ex)
			{
				Log.LogError(ex);

				ClearAll();
			}
		}

		public override void ClearAll()
		{
			GlobalVar.playerVars.Clear();
		}

		// 缓存正则实例，避免每次 IsMatch 都重新编译
		//private static readonly Regex _vanillaRegex = new Regex(@"^Vanilla(\d+)[^0-9].*\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		//private static readonly Regex _meadowRegex = new Regex(@"^RainMeadow(\d+)[^0-9].*\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public override string GetFileName(int saveSlot, SlugcatStats.Name slugcat)
		{
			bool isMeadow = MeadowCompat.IsModEnabled_RainMeadow && MeadowCompat.IsMeadowStoryMode();
			Log.LogInfo($"IsMeadow : {isMeadow}");

			string prefix = isMeadow ? "RainMeadow" : "Vanilla";
			return $"{prefix}_{saveSlot}_{slugcat.value}.json";
		}
		public override bool IsMatch(string fileName, int saveSlot)
		{
			bool isMeadow = MeadowCompat.IsModEnabled_RainMeadow && MeadowCompat.IsMeadowStoryMode();
			Log.LogInfo($"IsMeadow : {isMeadow}");

			string prefix = isMeadow ? "RainMeadow" : "Vanilla";

			string starts = $"{prefix}_{saveSlot}_";

			return fileName.StartsWith(starts, StringComparison.OrdinalIgnoreCase)
				   && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

			// 额外检查 saveSlot 是否匹配
			//var match = regex.Match(fileName);
			//if (!match.Success) return false;

			//// 提取 slot 数字并验证
			//if (int.TryParse(match.Groups[1].Value, out int slot))
			//{
			//	return slot == saveSlot;
			//}
			//return false;

			/*bool isMeadow = MeadowCompat.IsModEnabled_RainMeadow && MeadowCompat.IsMeadowStoryMode();

			Log.LogInfo($"IsMeadow : {isMeadow}");

			if (isMeadow)
			{
				Regex pattern_ = new Regex($"^RainMeadow{saveSlot}[^0-9].*\\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
				return pattern_.IsMatch(fileName);
			}

			Regex pattern = new Regex($"^Vanilla{saveSlot}[^0-9].*\\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			return pattern.IsMatch(fileName);*/
		}

	}


	public class JsonPropertyOnlyContractResolver : DefaultContractResolver
	{
		protected override List<MemberInfo> GetSerializableMembers(Type objectType)
		{
			// ⭐ 只返回标记了 [JsonProperty] 的成员
			var members = new List<MemberInfo>();

			// 获取所有公共字段和属性
			var allMembers = objectType.GetMembers(
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance
			);

			foreach (var member in allMembers)
			{
				// 检查是否有 [JsonProperty] 特性
				if (member.GetCustomAttribute<JsonPropertyAttribute>() != null)
				{
					members.Add(member);
				}
			}

			return members;
		}
	}

}