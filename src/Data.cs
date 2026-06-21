using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using static ExtensionLib.Plugin;

namespace ExtensionLib
{
	public class Data
	{
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
            GlobalVar.playerVars.Clear();
        }

    }


	public class LibData : Data
    {
		private static JsonSerializerSettings _settings = new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,// 可读性好，方便调试
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore,// 防止意外循环引用

			ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
			{
				// 强制使用 setter
				SerializeCompilerGeneratedMembers = false
			}
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
}