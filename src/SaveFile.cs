using On;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
		public static void SaveHooks_()
		{
			On.PlayerProgression.WipeAll -= PlayerProgression_WipeAll;
			On.PlayerProgression.WipeSaveState -= PlayerProgression_WipeSaveState;
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
		// 这里组合成 "[persistentDataPath]/ExtendedData" 目录
		private static string path => Path.Combine(Application.persistentDataPath, "ExtendedData");

		private static string GetSavePath(int saveSlot, SlugcatStats.Name slugcat)
		{
			string fileName = $"ExtendedData{saveSlot}{slugcat.value}.txt";
			return Path.Combine(path, fileName);
		}

		public static void WipeAll(int saveSlot)
		{
			// 确保目录存在
			if (Directory.Exists(path))
			{
				// 获取目录下所有文件的完整路径
				string[] files = Directory.GetFiles(path);

				// 预编译
				Regex pattern = new Regex($"^ExtendedData{saveSlot}.+\\.txt$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

				// 遍历每个文件
				for (int i = 0; i < files.Length; i++)
				{
					// 检查文件名是否以 "ExtendedData{存档槽位编号}" 开头
					// 注意：StartsWith 的参数构成为 path + 目录分隔符 + "ExtendedData" + saveSlot.ToString()
					// 例如 path 为 "/.../ExtendedData"，那么前缀就是 "/.../ExtendedData/ExtendedData1"（假设 saveSlot=1）

					string fileName = Path.GetFileName(files[i]);
					if (pattern.IsMatch(fileName))
					//if (files[i].StartsWith(path + Path.DirectorySeparatorChar.ToString() + "ExtendedData" + saveSlot.ToString()))
					{
						try
						{
							// 删除文件
							File.Delete(files[i]);
						}
						catch (Exception e)
						{
							UDebug.LogError($"Failed to delete {fileName}: {e.Message}");
						}
					}
				}
			}
		}

		public static void WipeSave(int saveSlot, global::SlugcatStats.Name slugcat)
		{
			// 确保目录存在
			if (Directory.Exists(path))
			{
				// ExtendedData[存档槽编号][角色名称].txt
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
			try
			{
				// 确保目录存在
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}

				string save = GetSavePath(saveSlot, slugcat);

				// 序列化
				string data = Data.Save();//***

				// 写入文件（覆盖已有内容）
				File.WriteAllText(save, data);

				UDebug.Log($"data={data}");
				UDebug.Log("Saving Extended");
			}
			catch (Exception ex)
			{
				UDebug.LogError($"Failed to save Extended data: {ex.Message}");
			}
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
				try
				{
					// 读取文件
					string data = File.ReadAllText(save);

					// 反序列化
					Data.Load(data);//***

					UDebug.Log($"data={data}");
				}
				catch (Exception ex)
				{
					UDebug.LogError($"Failed to load Extended save: {ex.Message}");

					Data.ClearAll();//***
				}

				UDebug.Log("Loading Extended - Exists");
			}
			// 若文件不存在
			else
			{
				//UDebug.Log($"data={data}");
				UDebug.Log("Loading Extended - Non-Existent");

				// 清空数据
				Data.ClearAll();//***
			}
		}


	}
}
