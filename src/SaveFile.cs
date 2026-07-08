using CoralBrain;
using On;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static ExtensionLib.Plugin;

namespace ExtensionLib
{
	public class SaveFile
	{
		public static void SaveHooks()
		{
			LibData libData = new LibData();
			SaveFile.AddData(libData);

			On.StoryGameSession.AddPlayer += StoryGameSession_AddPlayer;
			On.SaveState.SessionEnded += SaveState_SessionEnded;

			On.PlayerProgression.WipeAll += PlayerProgression_WipeAll;
			On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;
		}
		public static void SaveHooks_()
		{
			Datum.Clear();

			On.StoryGameSession.AddPlayer -= StoryGameSession_AddPlayer;
			On.SaveState.SessionEnded -= SaveState_SessionEnded;

			On.PlayerProgression.WipeAll -= PlayerProgression_WipeAll;
			On.PlayerProgression.WipeSaveState -= PlayerProgression_WipeSaveState;
		}

		// 存档结束时保存或加载数据
		private static void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, global::SaveState saveState, global::RainWorldGame game, bool survived, bool newMalnourished)
		{
			orig(saveState, game, survived, newMalnourished);

			int saveSlot = game.rainWorld.options.saveSlot;
			global::SlugcatStats.Name slugcat = saveState.saveStateNumber;

			Log.LogInfo($"survived : {survived} , malnourished : {newMalnourished}");

			if (survived && !newMalnourished)
			{
				Log.LogInfo($"Save");
				SaveFile.Save(saveSlot, slugcat);
			}
			else if (survived && newMalnourished)
			{
				Log.LogInfo($"newMalnourished");
				SaveFile.MalnourishedSave(saveSlot, slugcat);
			}
			else
			{
				if (!newMalnourished) // 未挨饿
				{
					Log.LogInfo($"Load");
					SaveFile.Load(saveSlot, slugcat);
				}
			}
		}

		// 添加角色时加载数据
		private static void StoryGameSession_AddPlayer(On.StoryGameSession.orig_AddPlayer orig, global::StoryGameSession storyGame, global::AbstractCreature player)
		{
			orig(storyGame, player);

			Log.LogInfo($"malnourished : {storyGame.saveState.malnourished}");

			if (!storyGame.saveState.malnourished)
			{
				Log.LogInfo($"Load");
				SaveFile.Load(storyGame.game.rainWorld.options.saveSlot, storyGame.saveStateNumber);
			}
		}

		// 删除存档槽位中的角色存档
		private static void PlayerProgression_WipeSaveState(On.PlayerProgression.orig_WipeSaveState orig, global::PlayerProgression progression, global::SlugcatStats.Name saveStateNumber)
		{
			orig(progression, saveStateNumber);
			WipeSave(progression.rainWorld.options.saveSlot, saveStateNumber);
		}

		// 删除存档槽位
		private static void PlayerProgression_WipeAll(On.PlayerProgression.orig_WipeAll orig, global::PlayerProgression progression)
		{
			orig(progression);
			WipeAll(progression.rainWorld.options.saveSlot);
		}

		#region Data
		private static Dictionary<string, Data> Datum = new();

		public static void AddData(Data data)
		{
			AddData(data.id, data);
		}
		public static void AddData(string id, Data data)
		{
			if (Datum.ContainsKey(id))
			{
				Log.LogError($"Datum with id '{id}' already exists. Overwriting.");
				Datum[id] = data;
				return;
			}
			Datum.Add(id, data);
		}
		#endregion

		public static FileDict steamMapping = new FileDict(Path.Combine(path(LibData.ID), "steamMapping.json"));


		// 构建目标目录路径：Application.persistentDataPath 是持久化数据目录（各平台不同）
		// 这里组合成 "[persistentDataPath]/ExtensionData/[id]" 目录
		private static string path(string id) => Path.Combine(pathMain, id);
		private static string pathMain => Path.Combine(Application.persistentDataPath, "ExtensionData");//"ExtendedData""ExtensionData"

		private static string GetSavePath(string id, int saveSlot, SlugcatStats.Name slugcat)
		{
			if (Datum.ContainsKey(id))
			{
				Data data_ = Datum[id];

				string fileName = data_.GetFileName(saveSlot, slugcat);
				return Path.Combine(path(id), fileName);
			}
			Log.LogError($"Datum with id '{id}' does not exist.");
			return "";
		}

		#region Wipe
		public static void WipeAll(int saveSlot)// saveSlot 是存档槽位编号，范围是 0-2  /  -1、-2、-3
		{
			Log.LogInfo($"saveSlot : {saveSlot}");

			try
			{
				foreach (var item in Datum)
				{
					Data data_ = item.Value;
					string id = item.Key;

					// 确保目录存在
					if (Directory.Exists(path(id)))
					{
						// 获取目录下所有文件的完整路径
						string[] files = Directory.GetFiles(path(id));

						// 预编译
						//Regex pattern = new Regex($"^{id}{saveSlot}[^0-9].*\\.json$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

						// 遍历每个文件
						for (int i = 0; i < files.Length; i++)
						{
							// 检查文件名是否以 "[id]{存档槽位编号}" 开头
							// 注意：StartsWith 的参数构成为 path + 目录分隔符 + [id] + saveSlot.ToString()
							// 例如 path 为 "/.../[id]"，那么前缀就是 "/.../[id]/[id]1"（假设 saveSlot=1）

							string fileName = Path.GetFileName(files[i]);

							if (data_.IsMatch(fileName, saveSlot))
							//if (files[i].StartsWith(path + Path.DirectorySeparatorChar.ToString() + [id] + saveSlot.ToString()))
							{
								try
								{
									// 删除文件
									File.Delete(files[i]);
								}
								catch (Exception e)
								{
									Log.LogInfo($"Failed to delete {fileName}: {e.Message}");
								}
							}
						}



					}
				}
			}
			catch (Exception ex)
			{
				Log.LogError($"Failed : {ex.Message}");
			}
		}

		public static void WipeSave(int saveSlot, global::SlugcatStats.Name slugcat)
		{
			Log.LogInfo($"saveSlot : {saveSlot} , slugcat : {slugcat}");

			try
			{
				foreach (var item in Datum)
				{
					Data data_ = item.Value;
					string id = item.Key;

					// 确保目录存在
					if (Directory.Exists(path(id)))
					{
						// [id][存档槽编号][角色名称].json
						// 例如[id]2White.json
						string save = GetSavePath(id, saveSlot, slugcat);

						// 确保文件存在
						if (File.Exists(save))
						{
							// 删除文件
							File.Delete(save);
						}
					}
				}

			}
			catch (Exception ex)
			{
				Log.LogError($"Failed : {ex.Message}");
			}
		}
		#endregion

		#region Save Load
		public static void Save(int saveSlot, global::SlugcatStats.Name slugcat)
		{
			Log.LogInfo($"saveSlot : {saveSlot} , slugcat : {slugcat}");

			try
			{
				foreach (var item in Datum)
				{
					Data data_ = item.Value;
					string id = item.Key;

					// 确保目录存在
					if (!Directory.Exists(path(id)))
					{
						Directory.CreateDirectory(path(id));
					}

					string save = GetSavePath(id, saveSlot, slugcat);

					// 序列化
					string data = data_.Save();//***

					// 写入文件（覆盖已有内容）
					File.WriteAllText(save, data);

					Log.LogInfo($"id={id}");
					Log.LogInfo($"data={data}");
				}

				Log.LogInfo("Saving Extended");
			}
			catch (Exception ex)
			{
				Log.LogError($"Failed to save data: {ex.Message}");
			}
		}

		public static void Load(int saveSlot, global::SlugcatStats.Name slugcat)
		{
			Log.LogInfo($"saveSlot : {saveSlot} , slugcat : {slugcat}");

			foreach (var item in Datum)
			{
				Data data_ = item.Value;
				string id = item.Key;

				// 确保目录存在
				if (!Directory.Exists(path(id)))
				{
					Directory.CreateDirectory(path(id));
				}

				string save = GetSavePath(id, saveSlot, slugcat);

				// 如果文件存在
				if (File.Exists(save))
				{
					try
					{
						// 读取文件
						string data = File.ReadAllText(save);

						Log.LogInfo($"id={id}");
						Log.LogInfo($"data={data}");

						// 反序列化
						data_.Load(data);//***
					}
					catch (Exception ex)
					{
						Log.LogError($"Failed to load id:{id} data: {ex.Message}");

						data_.ClearAll();//***
					}


				}
				// 若文件不存在
				else
				{
					//Log.Log($"data={data}");
					Log.LogInfo($"Loading id:{id} data - Non-Existent");

					// 清空数据
					data_.ClearAll();//***
				}
			}

			Log.LogInfo("Loading - Exists");
		}

		public static void MalnourishedSave(int saveSlot, global::SlugcatStats.Name slugcat)
		{
			foreach (var item in Datum)
			{
				Data data_ = item.Value;
				string id = item.Key;

				data_.MalnourishedSave();
			}
		}
		#endregion

	}
}
