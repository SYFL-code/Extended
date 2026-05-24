using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using On;
using UnityEngine;

namespace Extended
{
	public class SaveFile
	{
		public static void SaveHooks()
		{
			On.PlayerProgression.WipeAll += PlayerProgression_WipeAll;
			On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;
		}

		// 删除存档槽位中的角色存档
		private static void PlayerProgression_WipeSaveState(On.PlayerProgression.orig_WipeSaveState orig, global::PlayerProgression self, global::SlugcatStats.Name saveStateNumber)
		{
			orig(self, saveStateNumber);
			WipeSave(self.rainWorld.options.saveSlot, saveStateNumber);
		}

		// 删除存档槽位
		private static void PlayerProgression_WipeAll(On.PlayerProgression.orig_WipeAll orig, global::PlayerProgression self)
		{
			orig(self);
			WipeAll(self.rainWorld.options.saveSlot);
		}


		// 构建目标目录路径：Application.persistentDataPath 是持久化数据目录（各平台不同）
		// 这里组合成 "[persistentDataPath]/Extended_lyn" 目录
		private static string path => Path.Combine(Application.persistentDataPath, "Extended_lyn");

		private static string GetSavePath(int saveSlot, SlugcatStats.Name slugcat)
		{
			string fileName = $"Extended_lyn{saveSlot}{slugcat.value}.txt";
			return Path.Combine(path, fileName);
		}

		public static void WipeAll(int saveSlot)
		{
			// 确保目录存在
			if (Directory.Exists(path))
			{
				// 获取目录下所有文件的完整路径
				string[] files = Directory.GetFiles(path);

				// 遍历每个文件
				for (int i = 0; i < files.Length; i++)
				{
					// 检查文件名是否以 "Extended_lyn{存档槽位编号}" 开头
					// 注意：StartsWith 的参数构成为 path + 目录分隔符 + "Extended_lyn" + saveSlot.ToString()
					// 例如 path 为 "/.../Extended_lyn"，那么前缀就是 "/.../Extended_lyn/Extended_lyn1"（假设 saveSlot=1）
					if (files[i].StartsWith(path + Path.DirectorySeparatorChar.ToString() + "Extended_lyn" + saveSlot.ToString()))
					{
						// 删除文件
						File.Delete(files[i]);
					}
				}
			}
		}

		public static void WipeSave(int saveSlot, global::SlugcatStats.Name slugcat)
		{
			// 确保目录存在
			if (Directory.Exists(path))
			{
				// Extended_lyn[存档槽编号][角色名称].txt
				string save = GetSavePath(saveSlot, slugcat);

				// 确保文件存在
				if (File.Exists(save))
				{
					// 删除文件
					File.Delete(save);
				}
			}
		}

		public static void Save(int saveSlot, global::SlugcatStats.Name slugcat)
		{

			// 确保目录存在
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			string save = GetSavePath(saveSlot, slugcat);

			// 序列化
			string data = ExtendedData.SaveString();

			// 写入文件（覆盖已有内容）
			File.WriteAllText(save, data);

			UDebug.Log($"data={data}");
			UDebug.Log("Saving Extended");
		}

		public static void Load(int saveSlot, global::SlugcatStats.Name slugcat)
		{
			// 确保目录存在
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			string save = GetSavePath(saveSlot, slugcat);

			// 如果文件存在
			if (File.Exists(save))
			{
				// 读取文件
				string data = File.ReadAllText(save);
				ExtendedData.LoadString(data);

				UDebug.Log($"data={data}");
				UDebug.Log("Loading Extended - Exists");
			}
			// 若文件不存在
			else
			{
				ExtendedData.StoredDatas.Clear();

				//UDebug.Log($"data={data}");
				UDebug.Log("Loading Extended - Non-Existent");
			}
		}


	}
}
