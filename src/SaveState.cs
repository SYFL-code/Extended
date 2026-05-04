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
	public class SaveState
	{
		public static void SaveHooks()
		{
			On.PlayerProgression.WipeAll += PlayerProgression_WipeAll;
			On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState1;
		}

		private static void PlayerProgression_WipeSaveState1(On.PlayerProgression.orig_WipeSaveState orig, global::PlayerProgression self, global::SlugcatStats.Name saveStateNumber)
		{
			orig(self, saveStateNumber);
			WipeSave(self.rainWorld.options.saveSlot, saveStateNumber);
		}

		private static void PlayerProgression_WipeAll(On.PlayerProgression.orig_WipeAll orig, global::PlayerProgression self)
		{
			orig(self);
			WipeAll(self.rainWorld.options.saveSlot);
		}

		public static void WipeAll(int saveSlot)
		{
            // 构建目标目录路径：Application.persistentDataPath 是持久化数据目录（各平台不同）
            // 这里组合成 "[persistentDataPath]/Extended.lyn" 目录
            string path = Application.persistentDataPath + Path.DirectorySeparatorChar.ToString() + "Extended.lyn";

			// 检查目录是否存在
			if (Directory.Exists(path))
			{
				// 获取目录下所有文件的完整路径
				string[] files = Directory.GetFiles(path);

				// 遍历每个文件
				for (int i = 0; i < files.Length; i++)
				{
                    // 检查文件名是否以 "Extended.lyn{存档槽位编号}" 开头
                    // 注意：StartsWith 的参数构成为 path + 目录分隔符 + "Extended.lyn" + saveSlot.ToString()
                    // 例如 path 为 "/.../Extended.lyn"，那么前缀就是 "/.../Extended.lyn/Extended.lyn1"（假设 saveSlot=1）
                    if (files[i].StartsWith(path + Path.DirectorySeparatorChar.ToString() + "Extended.lyn" + saveSlot.ToString()))
					{
						// 如果匹配，则删除该文件
						File.Delete(files[i]);
					}
				}
			}
		}

		public static void WipeSave(int saveSlot, global::SlugcatStats.Name slugcat)
		{
			string path = Application.persistentDataPath + Path.DirectorySeparatorChar.ToString() + "Extended.lyn";

			if (Directory.Exists(path))
			{
                // 拼接出具体的文件名，格式为：
                // [path]/Extended.lyn[存档槽编号][角色编号或名称].txt
                string save = string.Concat(new string[]
				{
				path,
				Path.DirectorySeparatorChar.ToString(),
                "Extended.lyn",
				saveSlot.ToString(),
				slugcat.value.ToString(),
				".txt"
				});

				// 检查该文件是否存在
				if (File.Exists(save))
				{
					// 存在则删除
					File.Delete(save);
				}
			}
		}

		public static void Save(int saveSlot, global::SlugcatStats.Name slugcat)
		{
            // 1. 构建 Extended.lyn 目录路径
            string path = Application.persistentDataPath + Path.DirectorySeparatorChar.ToString() + "Extended.lyn";

			// 2. 若目录不存在则创建（确保写入时不报错）
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			// 3. 只有当静态存储对象不为空时才保存
			if (ExtendedData.StoredDatas.Count > 0)
            {
                // 4. 拼接出完整文件路径，文件名格式：
                //    Extended.lyn[存档槽编号][角色数字值].txt
                string save = string.Concat(new string[]
				{
				path,
				Path.DirectorySeparatorChar.ToString(),
                "Extended.lyn",
				saveSlot.ToString(),
				slugcat.value.ToString(),
				".txt"
				});

				// 5. 将当前数据序列化为字符串
				//string data = Zname.GenerateRandomText();//
				string data = ExtendedData.SaveString();

				// 6. 将字符串写入文件（覆盖已有内容）
				File.WriteAllText(save, data);

                // 7. 调试日志
                UDebug.Log($"data={data}");
                UDebug.Log("Saving Extended");
			}
		}

		public static void Load(int saveSlot, global::SlugcatStats.Name slugcat)
		{
			// 1. 同样的目录路径
			string path = Application.persistentDataPath + Path.DirectorySeparatorChar.ToString() + "Extended.lyn";

			// 2. 确保目录存在（与 Save 对称）
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			// 3. 拼接与 Save 完全相同的文件名
			string save = string.Concat(new string[]
			{
			path,
			Path.DirectorySeparatorChar.ToString(),
            "Extended.lyn",
			saveSlot.ToString(),
			slugcat.value.ToString(),
			".txt"
			});

			// 4. 如果文件存在，则读取并反序列化
			if (File.Exists(save))
			{
				string data = File.ReadAllText(save);
				ExtendedData.LoadString(data);//

				UDebug.Log($"data={data}");
				UDebug.Log("Loading Extended - Exists");
			}
			// 5. 若文件不存在，则初始化默认数据
			else
			{
				//string data = Zname.GenerateRandomText();//
                ExtendedData.StoredDatas = new();
                //ExtendedData.storedObjects = new List<ExtendedData.StoredObject>();

                //UDebug.Log($"data={data}");
				UDebug.Log("Loading Extended - Non-Existent");
			}
		}


	}
}
