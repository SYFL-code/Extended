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

        public virtual string GetFileName(int saveSlot, SlugcatStats.Name slugcat)
        {
            string fileName = $"{id}{saveSlot}{slugcat.value}.json";
            return fileName;
        }
        public virtual bool IsMatch(string fileName, int saveSlot)
        {
            // 预编译
            Regex pattern = new Regex($"^{id}{saveSlot}[^0-9].*\\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return pattern.IsMatch(fileName);
        }


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

	}


	public class LibData : Data
	{
		public override string id { get; set; } = "ExtensionLib";// 唯一标识符，确保不会与其他 mod 冲突


		private static JsonSerializerSettings _settings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,// 可读性好，方便调试
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore,// 防止意外循环引用

            ContractResolver = new JsonPropertyOnlyContractResolver()
        };

        public override string GetFileName(int saveSlot, SlugcatStats.Name slugcat)
        {
            /*Log.LogInfo($"MeadowInLobby : {MeadowCompat.MeadowInLobby}");
            if (MeadowCompat.MeadowInLobby)
			{
                string fileName = $"RainMeadow{saveSlot}{slugcat.value}.json";
                return fileName;
            }*/
            string fileName = $"Vanilla{saveSlot}{slugcat.value}.json";
            return fileName;
        }
        public override bool IsMatch(string fileName, int saveSlot)
        {
            /*Log.LogInfo($"MeadowInLobby : {MeadowCompat.MeadowInLobby}");
            if (MeadowCompat.MeadowInLobby)
            {
                Regex pattern_ = new Regex($"^RainMeadow{saveSlot}[^0-9].*\\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                return pattern_.IsMatch(fileName);
            }*/
            // 预编译
            Regex pattern = new Regex($"^Vanilla{saveSlot}[^0-9].*\\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return pattern.IsMatch(fileName);
        }

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