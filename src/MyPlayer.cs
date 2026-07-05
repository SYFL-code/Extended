using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ExtensionLib.Helper;
using static ExtensionLib.PlayerVar;

namespace ExtensionLib
{
	public class MyPlayer
	{

		public static void Hook()
		{
			On.Player.ctor += Player_ctor;
			On.Player.Update += Player_Update;
			On.Player.Destroy += Player_Destroy;

			On.Player.SwallowObject += Player_SwallowObject;
			On.Player.Regurgitate += Player_Regurgitate;

			On.Player.StomachGlowLightColor += Player_StomachGlowLightColor;

			IL.Player.AddFood += Player_AddFood;
			IL.Player.GrabUpdate += Player_GrabUpdate;
		}

		public static void Hook_()
		{
			On.Player.ctor -= Player_ctor;
			On.Player.Update -= Player_Update;
			On.Player.Destroy -= Player_Destroy;

			On.Player.SwallowObject -= Player_SwallowObject;
			On.Player.Regurgitate -= Player_Regurgitate;

			On.Player.StomachGlowLightColor -= Player_StomachGlowLightColor;

			IL.Player.AddFood -= Player_AddFood;
			IL.Player.GrabUpdate -= Player_GrabUpdate;
		}


		private static void Player_AddFood(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			/*
				// add = Math.Min(add, MaxFoodInStomach - this.playerState.foodInStomach);
				IL_0014: br IL_0150

				IL_0019: ldarg.1
				IL_001a: ldarg.0
				IL_001b: call instance int32 Player::get_MaxFoodInStomach()
				IL_0020: ldarg.0
				IL_0021: call instance class PlayerState Player::get_playerState()
				IL_0026: ldfld int32 PlayerState::foodInStomach
				IL_002b: sub
				IL_002c: call int32 [mscorlib]System.Math::Min(int32, int32)
				IL_0031: starg.s 'add'
			*/

			bool logged = false;

			if (c.TryGotoNext(MoveType.After,
				(i) => i.Match(OpCodes.Br),
				(i) => i.MatchLdarg(1),
				(i) => i.MatchLdarg(0),
				(i) => i.MatchCall<Player>("get_MaxFoodInStomach")
			))
			{
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Func<int, Player, int>>((origMaxFood, self) =>
				{
					if (!logged || Input.GetKey("c"))
					{
						logged = true;

						Log.LogInfo($"Player orig maxFoodInStomach {origMaxFood}");
					}

					return origMaxFood - 2;
				});
			}
		}

		private static void Player_GrabUpdate(ILContext il)
		{
			//throw new Exception("[Player_GrabUpdate] Patch entered");

			Logs.Write("[Player_GrabUpdate] Patch entered");

			ILCursor c = new ILCursor(il);

			Dictionary<int, bool> logs = new();
			for (int i = 0; i < 15; i++)
			{
				logs.Add(i, false);
			}
			int logIndex = 0;

			logIndex = 1;
			// ============================================================
			// 问题1: IL_0451 附近
			// 原版: if (objectInStomach == null || CanPutSpearToBack || CanPutSlugToBack)
			// 改为: if (stomachList.Count < maxCapacity || CanPutSpearToBack || CanPutSlugToBack)
			// 用途: 判断是否能拿取新物品（胃有空位 或 能放背上）
			// ============================================================
			/*if (c.TryGotoNext(MoveType.Before,
				i => i.MatchLdarg(0),
				i => i.MatchLdfld<Player>("objectInStomach"),
				i => i.MatchBrfalse(out _),
				i => i.MatchLdarg(0),
				i => i.MatchCall<Player>("get_CanPutSpearToBack")
				))
			{
				LogsWrite(c, logs, logIndex);

				c.Remove();
				c.Remove();
				//c.Remove();

				// 插入: 检查胃是否未满
				//原逻辑 objectInStomach == null → brfalse 跳转(满足条件)

				c.Emit(OpCodes.Ldarg, 0);
				c.EmitDelegate<Func<Player, bool>>(player =>
				{
					Log.LogInfo($"[{logIndex}]");

					player.GetPlayerVar(out var pv);
					var stomachData = pv.stomachData;
					return stomachData.IsFull;
				});
				//c.Emit(OpCodes.Brfalse,  跳转到 IL_046c 的标签 );
			}
			else
			{
				Logs.Write($"[Player_GrabUpdate] [logIndex:{logIndex}] not found");
			}*/


			logIndex = 0;
			if (c.TryGotoNext(MoveType.Before,
				i => i.MatchLdarg(0),
				i => i.MatchLdfld<Player>("objectInStomach"),//objectInStomach spearOnBack
				i => i.MatchBrtrue(out _),
				i => i.MatchLdarg(0),
				i => i.MatchCall<Player>("get_isGourmand"),
				i => i.MatchBrtrue(out _)
				))
			{
				LogsWrite(c, logs, logIndex);

				c.Emit(OpCodes.Ldarg, 0);

				c.EmitDelegate<Action<Player>>(player =>
				{
					if (!logs[logIndex] || Input.GetKey("c"))
					{
						logs[logIndex] = true;

						Log.LogInfo($"[{logIndex}] {logIndex}");

						Logs.Write($"[Player_GrabUpdate]-[{logIndex}] {logIndex}");
					}
				});
			}
			else
			{
				Logs.Write($"[Player_GrabUpdate] [logIndex:{logIndex}] not found");
			}

			Logs.Write("[Player_GrabUpdate] Patch entered_");

		}

		public static void LogsWrite(ILCursor c, Dictionary<int, bool> logs, int logIndex)
		{
			Logs.Write($"[Player_GrabUpdate]-[{logIndex}] found");

			/*c.Emit(OpCodes.Ldarg, 0);

			c.EmitDelegate<Action<Player>>(player =>
			{
				if (!logs[logIndex] || Input.GetKey("c"))
				{
					logs[logIndex] = true;

					Log.LogInfo($"[{logIndex}] {logIndex}");

					Logs.Write($"[Player_GrabUpdate]-[{logIndex}] {logIndex}");
				}
			});*/
		}

		public static int BeSwallowed(Player player, bool needCanBeSwallowed = true)
		{
			int grasp = -1;
			for (int i = 0; i < player.grasps.Length; i++)
			{
				if (player.grasps[i] != null)
				{
					if (!needCanBeSwallowed || player.CanBeSwallowed(player.grasps[i].grabbed))
					{
						grasp = i;
					}
				}
			}

			Log.LogInfo($"BeSwallowed result : {grasp}");
			return grasp;

			/*int num13 = 0;
			while (num13 < 2)
			{
				if (base.grasps[num13] != null && this.CanBeSwallowed(base.grasps[num13].grabbed))
				{
					base.bodyChunks[0].pos += Custom.DirVec(base.grasps[num13].grabbed.firstChunk.pos, base.bodyChunks[0].pos) * 2f;
					this.SwallowObject(num13);
					if (this.spearOnBack != null)
					{
						this.spearOnBack.interactionLocked = true;
					}
					if ((ModManager.MSC || ModManager.CoopAvailable) && this.slugOnBack != null)
					{
						this.slugOnBack.interactionLocked = true;
					}
					this.swallowAndRegurgitateCounter = 0;
					if (base.graphicsModule != null)
					{
						(base.graphicsModule as PlayerGraphics).swallowing = 20;
						break;
					}
					break;
				}
				else
				{
					num13++;
				}
			}*/
		}


		private static void Player_ctor(On.Player.orig_ctor orig, Player player, AbstractCreature abs, World world)
		{
			orig(player, abs, world);


			Log.LogDebug("Player ctor");

			player.GetPlayerVar(out var pv);
			var stomachData = pv.stomachData;

			pv.coord = player.coord;

			if (stomachData._serializedHistory.Count != 0)
			{
				foreach (var s in stomachData._serializedHistory)
				{
					Log.LogDebug($"Trying to spawn {s}");

					var Object = Helper.ObjectFromString(s, player.room.world, player.coord, player.coord);

					if (Object != null)
					{
						Log.LogDebug($"Successfully spawn {s}");
						stomachData.historyInStomach.Add(Object);
					}
				}
				stomachData._serializedHistory.Clear();
			}

			/*(bool, List<int>) result = IncludeById(pv.objectsInStomach, player.objectInStomach);

			Log.LogInfo($"IncludeById result : {result}");

			if (!result.Item1 && player.objectInStomach != null)
			{
				pv.objectsInStomach.Add(player.objectInStomach);
			}
			else
			{
				player.objectInStomach = pv.objectsInStomach[pv.objectsInStomach.Count - 1];
			}*/
		}

		private static void Player_Update(On.Player.orig_Update orig, Player player, bool eu)
		{
			// 防御层：补位逻辑
			player.GetPlayerVar(out var pv);
			var stomachData = pv.stomachData;

			if (stomachData.Current == null && stomachData.HistoryCount > 0)
			{
				stomachData.Current = stomachData.Pop();
			}


			//体温/低温系统（Hypothermia System）
			if (player.room != null && player.room.blizzard)
			{
				/*if (ModManager.MSC && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
				{
					player.Hypothermia -= Mathf.Lerp(RainWorldGame.DefaultHeatSourceWarmth, 0f, player.HypothermiaExposure);
				}*/

				int lanternCount = stomachData.historyInStomach.Count(obj => obj.type == AbstractPhysicalObject.AbstractObjectType.Lantern);

				if (lanternCount > 0)
				{
					lanternCount = Math.Min(lanternCount, 2);

					float warmthMultiplier = 0.6f + (lanternCount - 1) * 0.3f;
					player.Hypothermia -= Mathf.Lerp(RainWorldGame.DefaultHeatSourceWarmth * warmthMultiplier, 0f, player.HypothermiaExposure);

					//player.Hypothermia -= Mathf.Lerp(RainWorldGame.DefaultHeatSourceWarmth, 0f, player.HypothermiaExposure);
				}

				/*if (player.room.game.cameras[0].ghostMode >= 1f)
				{
					player.HypothermiaGain = 0f;
					player.Hypothermia = Mathf.Lerp(player.Hypothermia, 0f, player.room.game.cameras[0].ghostMode / 100f);
				}
				if (player.Hypothermia < 0f)
				{
					player.Hypothermia = 0f;
				}*/
			}

			orig(player, eu);

			pv.coord = player.coord;

			/*if (pv.swallowedObjectsTemp.Count != 0)
			{
				foreach (var s in pv.swallowedObjectsTemp)
				{
					var Object = Helper.ObjectFromString(s, player.room.world, player.coord, player.coord);
					
					if (Object != null)
					{
						pv.objectsInStomach.Add(Object);
					}
				}
				pv.swallowedObjectsTemp.Clear();
			}*/

			if (Input.GetKeyDown("n"))
			{
				int grasp = BeSwallowed(player, false);

				if (grasp >= 0)
				{
					player.SwallowObject(grasp);
				}

				/*if (player.objectInStomach != null)
				{
					pv.objectsInStomach.Add(player.objectInStomach);
					player.objectInStomach = null;
				}*/
			}

			if (Input.GetKeyDown("m"))
			{
				player.Regurgitate();

				/*int grasp = -1;
				for (int i = 0; i < player.grasps.Length; i++)
				{
					if (player.grasps[i]?.grabbed.abstractPhysicalObject == null)
					{
						grasp = i;
					}
				}
				if (grasp >= 0)
				{
					AbstractPhysicalObject? obj = PopCurrent(player);

					if (obj != null)
					{
						if (player.grasps[grasp] == null)
						{
							player.grasps[grasp] = new Player.Grasp(player, grasp);
						}

						player.objectInStomach = obj;
					}
				}*/

				/*if (pv.objectsInStomach.Count > 0 && player.objectInStomach == null)
				{
					player.objectInStomach = pv.objectsInStomach[pv.objectsInStomach.Count - 1];
					pv.objectsInStomach.RemoveAt(pv.objectsInStomach.Count - 1);
				}*/
			}
		}

		private static void Player_Destroy(On.Player.orig_Destroy orig, Player player)
		{
			player.GetPlayerVar(out var pv);

			pv.coord = player.coord;

			orig(player);
		}

		// 吞咽
		private static void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player player, int grasp)
		{
			Log.LogInfo("===Before");

			player.GetPlayerVar(out var pv);
			var stomachData = pv.stomachData;

			stomachData.Swallow(null);

			orig(player, grasp);

			if (player.objectInStomach != null)
			{
				Log.LogInfo($"吞咽成功！胃部物品数量: {stomachData.TotalCount}");
				for (int i = 0; i < stomachData.TotalCount; i++)
				{
					Log.LogInfo(stomachData.GetAllContents()[i].ToString());
				}
			}
			else
			{
				Log.LogInfo("原版吞咽函数没有处理物品");
			}

			Log.LogInfo("===After");
		}

		// 反刍
		private static void Player_Regurgitate(On.Player.orig_Regurgitate orig, Player player)
		{
			Log.LogInfo("===Before");

			orig(player);

			player.GetPlayerVar(out var pv);
			var stomachData = pv.stomachData;

			stomachData.PopCurrent();

			if (player.objectInStomach == null)
			{
				Log.LogInfo($"胃部物品数量: {stomachData.TotalCount}");
				for (int i = 0; i < stomachData.TotalCount; i++)
				{
					Log.LogInfo(stomachData.GetAllContents()[i].ToString());
				}
			}
			else
			{
				//Log.LogInfo("原版反刍函数没有处理物品");
			}

			//bool hasItems = pv.objectsInStomach.Count > 0;
			//AbstractPhysicalObject? lastItem = hasItems ? pv.objectsInStomach[pv.objectsInStomach.Count - 1] : null;

			// 从历史取出补位
			/*AbstractPhysicalObject? lastItem;

			if (stomachData.HistoryCount > 0)
			{
				lastItem = stomachData.Pop();
			}
			else
			{
				lastItem = null;
			}

			if (hasItems)
			{
				// 设置要吐出的物品
				player.objectInStomach = lastItem;
			}

			orig(player);

			if (hasItems && player.objectInStomach == null)
			{
				// 吐出成功，从列表移除
				pv.objectsInStomach.RemoveAt(pv.objectsInStomach.Count - 1);

				// 同步头指针到新的最后一个
				if (pv.objectsInStomach.Count > 0)
				{
					player.objectInStomach = pv.objectsInStomach[pv.objectsInStomach.Count - 1];
				}
			}*/

			Log.LogInfo("===After");
		}

		private static Color? Player_StomachGlowLightColor(On.Player.orig_StomachGlowLightColor orig, Player player)
		{
			Color? color = orig(player);

			var content = player.objectInStomach;

			player.GetPlayerVar(out var pv);
			var stomachData = pv.stomachData;

			Color mixed = Color.black;
			int count = 0;

			if (!stomachData.IsEmpty)
			{
				foreach (var obj in stomachData.GetAllContents())
				{
					player.objectInStomach = obj;
					Color? c = orig(player);

					if (c.HasValue)
					{
						mixed += c.Value;
						count++;
					}
				}
			}

			player.objectInStomach = content;

			if (count > 0)
			{
				return new Color?(mixed / count); // 平均混合
			}
			return color;
		}

	}
}
