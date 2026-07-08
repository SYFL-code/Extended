using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using MoreSlugcats;
using Steamworks;
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
			//On.Player.ctor += Player_ctor;
			On.Player.Update += Player_Update;
			On.Player.Destroy += Player_Destroy;

			On.Player.SwallowObject += Player_SwallowObject;
			On.Player.Regurgitate += Player_Regurgitate;

			On.Player.StomachGlowLightColor += Player_StomachGlowLightColor;

			On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
			On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
			On.PlayerGraphics.Update += PlayerGraphics_Update;
			On.SlugcatHand.Update += SlugcatHand_Update;

			//IL.Player.AddFood += Player_AddFood;
			IL.Player.GrabUpdate += Player_GrabUpdate;
		}

		public static void Hook_()
		{
			//On.Player.ctor -= Player_ctor;
			On.Player.Update -= Player_Update;
			On.Player.Destroy -= Player_Destroy;

			On.Player.SwallowObject -= Player_SwallowObject;
			On.Player.Regurgitate -= Player_Regurgitate;

			On.Player.StomachGlowLightColor -= Player_StomachGlowLightColor;

			On.PlayerGraphics.InitiateSprites -= PlayerGraphics_InitiateSprites;
			On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
			On.PlayerGraphics.Update -= PlayerGraphics_Update;
			On.SlugcatHand.Update -= SlugcatHand_Update;

			//IL.Player.AddFood -= Player_AddFood;
			IL.Player.GrabUpdate -= Player_GrabUpdate;
		}


		private static void Player_GrabUpdate(ILContext il)
		{
			try
			{
				//Logs.Write(il.ToString(), false);

				ILCursor c = new ILCursor(il);

				// replace all mentions of 'isGourmand' with the equivalent of '(isGourmand || CanRegurgitate(player))'
				// 将所有对 'isGourmand' 的引用替换为等价于 '(isGourmand || CanRegurgitate(player))' 的逻辑

				// match 'isGourmand', brfalse edition
				// 匹配 'isGourmand'，brfalse 版本
				while (c.TryGotoNext(MoveType.After,
					i => i.MatchLdarg(0),
					i => i.MatchLdfld<Player>("objectInStomach"),
					i => i.Match(OpCodes.Brfalse_S) || i.Match(OpCodes.Brfalse) // 匹配 brfalse（假时跳转）
				))
				{
					// 1

					Logs.Write($"[logIndex:brfalse edition]");

					// this is the condition we should skip to if our check succeeds, replicating the behavior if the vanilla che
					// 如果我们的检查通过，就跳转到这个标签，复制原版检查通过时的行为
					// 如果我们的检查成功，这是我们应该跳过的条件，复制普通che的行为
					ILLabel skip = c.MarkLabel();

					ILLabel? proceedCond = c.Prev.Operand as ILLabel;//跳转指令.Operand  跳转处
					//c.Prev 光标位置前的指令，或如果光标位于指令列表开头，则为null

					//向前移动
					c.GotoPrev(MoveType.Before,
						i => i.MatchLdarg(0),
						i => i.Match(OpCodes.Ldfld)
					);

					c.Index += 1;

					c.Remove();
					c.Remove();

					// insert the condition
					// 插入条件判断
					//c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<Func<Player, bool>>(player =>
					{
						player.GetPlayerVar(out var pv);
						var stomachData = pv.stomachData;

						//Log.LogInfo($"[logIndex:brfalse edition] IsEmpty: {stomachData.IsEmpty}");

						return !stomachData.IsFull;

						//return InHand(player);
					});

					// 如果条件为真，跳进去
					c.Emit(OpCodes.Brtrue, proceedCond);

					// move forwards to avoid an infloop
					// 向后移动，避免无限循环
					c.GotoNext(MoveType.After,
						i => i.Match(OpCodes.Brfalse_S) || i.Match(OpCodes.Brfalse)
					);
				}



				c.Index = 0;
				// match isGourmand', brtrue edition
				// 匹配 'isGourmand'，brtrue 版本
				while (c.TryGotoNext(MoveType.After,
					i => i.MatchLdarg(0),
					i => i.MatchLdfld<Player>("objectInStomach"),
					i => i.Match(OpCodes.Brtrue_S) || i.Match(OpCodes.Brtrue) // 匹配 brtrue（真时跳转）
				))
				{
					Logs.Write($"[logIndex:brtrue edition]");

					// a lot easier here, since you can just insert another cond
					// 这里简单多了，因为你可以直接插入另一个条件
					ILLabel? proceedCond = c.Prev.Operand as ILLabel;//跳转指令.Operand  跳转处
					//c.Prev 光标位置前的指令，或如果光标位于指令列表开头，则为null

					//ILLabel? nextCond = c.Next.Operand as ILLabel;
					//var markCursor = new ILCursor(il);
					//markCursor.Index = c.Index + 1;  // 指向 c.Next
					//var labelAtNext = markCursor.MarkLabel();  // 在 c.Next 前面打标签


					int originalIndex = c.Index;

					//向前移动
					c.GotoPrev(MoveType.Before,
						i => i.MatchLdarg(0),
						i => i.Match(OpCodes.Ldfld)
					);
					int originalIndexBefore = c.Index;

					// 2
					c.Index = originalIndex;
					if (c.Next.MatchLdarg(0))
					{
						c.Index += 1;
						if (c.Next.MatchCallOrCallvirt<Player>("get_isGourmand"))
						{
							// IL_064e: brfalse.s IL_066b

							c.Index += 1;
							if (c.Next.Match(OpCodes.Brfalse_S) || c.Next.Match(OpCodes.Brfalse))
							{

								Logs.Write($"[logIndex:brtrue edition] 2 IsEmpty");

								c.Index = originalIndexBefore;

								c.Remove();
								c.Remove();
								c.Remove();

								c.Emit(OpCodes.Ldarg_0);
								c.EmitDelegate<Func<Player, bool>>(player =>
								{
									player.GetPlayerVar(out var pv);
									var stomachData = pv.stomachData;

									//Log.LogInfo($"[logIndex:brtrue edition] 2 IsEmpty: {stomachData.IsEmpty}");
									return !stomachData.IsEmpty;
								});

								c.Emit(OpCodes.Brtrue, proceedCond);

								// move forwards to avoid an infloop
								// 向后移动，避免无限循环
								c.GotoNext(MoveType.After,
									i => i.Match(OpCodes.Brtrue_S) || i.Match(OpCodes.Brtrue)
								);

								continue;
							}

						}
					}


					// 3
					c.Index = originalIndex;
					if (c.Next.MatchLdarg(0))
					{
						c.Index += 1;
						if (c.Next.MatchCallOrCallvirt<Player>("get_isGourmand"))
						{
							// IL_18dd: brtrue.s IL_18fe

							c.Index += 1;
							if (c.Next.Match(OpCodes.Brtrue_S) || c.Next.Match(OpCodes.Brtrue))
							{
								Logs.Write($"[logIndex:brtrue edition] Regurgitate IsEmpty");

								c.Index = originalIndexBefore;

								c.Remove();
								c.Remove();
								c.Remove();

								c.Emit(OpCodes.Ldarg_0);
								// insert the condition
								// 插入条件判断
								c.EmitDelegate<Func<Player, bool>>(player =>
								{
									/*if (!logged)
									{
										foreach (var instruct in il.Instrs)
										{
											Debug.Log(instruct.ToString());
										}
										logged = true;
									}*/

									player.GetPlayerVar(out var pv);
									var stomachData = pv.stomachData;

									Log.LogInfo($"[logIndex:brtrue edition] Regurgitate IsEmpty: {stomachData.IsEmpty}");
									Log.LogInfo($"[logIndex:brtrue edition] Regurgitate IsFull: {stomachData.IsFull}");

									int inHand = InHand(player, true);

									Log.LogInfo($"[logIndex:brtrue edition] Regurgitate inHand: {inHand}");
									if (inHand != -1 && !stomachData.IsFull)
									{
										return false;
									}

									return !stomachData.IsEmpty;
								});

								// if it's true, proceed as usual
								// 如果条件为真，照常执行
								c.Emit(OpCodes.Brtrue, proceedCond);

								//c.Emit(OpCodes.Br_S, nextCond);
								//c.Emit(OpCodes.Br_S, labelAtNext);

								// move forwards to avoid an infloop
								// 向后移动，避免无限循环
								c.GotoNext(MoveType.After,
									i => i.Match(OpCodes.Brtrue_S) || i.Match(OpCodes.Brtrue)
								);

								continue;
							}

						}
					}

					// 5
					c.Index = originalIndex;
					if (c.Next.MatchLdarg(0))
					{
						Logs.Write($"[logIndex:brtrue edition] SwallowObject 1");
						c.Index += 1;
						if (c.Next.MatchLdfld<Player>("swallowAndRegurgitateCounter"))
						{
							Logs.Write($"[logIndex:brtrue edition] SwallowObject 2");
							c.Index += 1;
							if (c.Next.MatchLdcI4(90))
							{
								Logs.Write($"[logIndex:brtrue edition] SwallowObject 3");
								c.Index += 1;
								if (c.Next.MatchBle(out _))
								{
									Logs.Write($"[logIndex:brtrue edition] SwallowObject IsFull");

									c.Index = originalIndexBefore;
									c.Index += 1;

									c.Remove();
									c.Remove();

									//c.Emit(OpCodes.Ldarg_0);
									// insert the condition
									// 插入条件判断
									c.EmitDelegate<Func<Player, bool>>(player =>
									{
										player.GetPlayerVar(out var pv);
										var stomachData = pv.stomachData;

										Log.LogInfo($"[logIndex:brtrue edition] SwallowObject IsFull: {stomachData.IsFull}");
										return stomachData.IsFull;
									});

									//出去 if
									c.Emit(OpCodes.Brtrue, proceedCond);

									//c.Emit(OpCodes.Br_S, nextCond);
									//c.Emit(OpCodes.Br_S, labelAtNext);

									// move forwards to avoid an infloop
									// 向后移动，避免无限循环
									c.GotoNext(MoveType.After,
										i => i.Match(OpCodes.Brtrue_S) || i.Match(OpCodes.Brtrue)
									);

									continue;
								}
							}
						}
					}


					// 4?
					Logs.Write($"[logIndex:brtrue edition] ???");

					c.Index = originalIndexBefore;

					c.Remove();
					c.Remove();
					c.Remove();

					c.Emit(OpCodes.Ldarg_0);
					// insert the condition
					// 插入条件判断
					c.EmitDelegate<Func<Player, bool>>(player =>
					{
						player.GetPlayerVar(out var pv);
						var stomachData = pv.stomachData;

						Log.LogInfo($"[logIndex:brtrue edition] IsEmpty: {stomachData.IsEmpty}");
						return !stomachData.IsEmpty;

						//return false;
						//return CanRegurgitate(player);
					});

					// if it's true, proceed as usual
					// 如果条件为真，照常执行
					c.Emit(OpCodes.Brtrue, proceedCond);

					//c.Emit(OpCodes.Br_S, nextCond);
					//c.Emit(OpCodes.Br_S, labelAtNext);

					// move forwards to avoid an infloop
					// 向后移动，避免无限循环
					c.GotoNext(MoveType.After,
						i => i.Match(OpCodes.Brtrue_S) || i.Match(OpCodes.Brtrue)
					);
				}


				c.Index = 0;
				//IL_1869: ldloc.1
				//IL_186a: brfalse IL_1af2
				if (c.TryGotoNext(MoveType.After,
					i => i.MatchLdloc(1),
					i => i.Match(OpCodes.Brfalse_S) || i.Match(OpCodes.Brfalse) // 匹配 brfalse（假时跳转 跳出去）
				))
				{
					Logs.Write($"[logIndex:flag3 edition]");

					c.Index -= 1;

					// Ldarg.0	将索引为 0 的参数加载到计算堆栈上。
					// Ldloc.0	将索引 0 处的局部变量加载到计算堆栈上。

					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<Func<bool, Player, bool>>((flag3, player) =>
					{
						//Log.LogInfo($"flag3: {flag3}");

						player.GetPlayerVar(out var pv);
						var stomachData = pv.stomachData;

						pv.stomachData.swallowOrRegurgitate = flag3;

						return flag3;
					});


				}

				Logs.Write(il.ToString(), false);
			}
			catch (Exception ex)
			{
				Logs.Write($"[Player_GrabUpdate] Exception: {ex}");
			}
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
		private static void Player_GrabUpdate_(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			// replace all mentions of 'isGourmand' with the equivalent of '(isGourmand || CanRegurgitate(player))'
			// 将所有对 'isGourmand' 的引用替换为等价于 '(isGourmand || CanRegurgitate(player))' 的逻辑

			// match 'isGourmand', brfalse edition
			// 匹配 'isGourmand'，brfalse 版本
			while (c.TryGotoNext(MoveType.After,
				i => i.MatchLdarg(0),
				i => i.MatchCallOrCallvirt<Player>("get_isGourmand"),
				i => i.Match(OpCodes.Brfalse_S) || i.Match(OpCodes.Brfalse) // 匹配 brfalse（假时跳转）
			))
			{
				// this is the condition we should skip to if our check succeeds, replicating the behavior if the vanilla che
				// 如果我们的检查通过，就跳转到这个标签，复制原版检查通过时的行为
				ILLabel skipGourmandCond = c.MarkLabel();
				//向前移动
				c.GotoPrev(MoveType.Before,
					i => i.MatchLdarg(0),
					i => i.Match(OpCodes.Call) || i.Match(OpCodes.Callvirt)
				);

				// insert the condition
				// 插入条件判断
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Func<Player, bool>>(player =>
				{
					return false;
					//return CanRegurgitate(player);
				});

				// if it's true, skip ahead
				// 如果条件为真，跳过原版检查
				c.Emit(OpCodes.Brtrue_S, skipGourmandCond);

				// move forwards to avoid an infloop
				// 向后移动，避免无限循环
				c.GotoNext(MoveType.After,
					i => i.Match(OpCodes.Brfalse_S) || i.Match(OpCodes.Brfalse)
				);
			}

			c.Index = 0;

			// match isGourmand', brtrue edition
			// 匹配 'isGourmand'，brtrue 版本
			while (c.TryGotoNext(MoveType.After,
				i => i.MatchLdarg(0),
				i => i.MatchCallOrCallvirt<Player>("get_isGourmand"),
				i => i.Match(OpCodes.Brtrue_S) || i.Match(OpCodes.Brtrue) // 匹配 brtrue（真时跳转）
			))
			{
				// a lot easier here, since you can just insert another cond
				// 这里简单多了，因为你可以直接插入另一个条件
				ILLabel? proceedCond = c.Prev.Operand as ILLabel;
				//c.Prev 光标位置前的指令，或如果光标位于指令列表开头，则为null

				c.Emit(OpCodes.Ldarg_0);
				// insert the condition
				// 插入条件判断
				c.EmitDelegate<Func<Player, bool>>(player =>
				{
					return false;
					//return CanRegurgitate(player);
				});

				// if it's true, proceed as usual
				// 如果条件为真，照常执行（跳进 if 块）
				c.Emit(OpCodes.Brtrue_S, proceedCond);
			}
		}

		private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(playerGraphics, sLeaser, rCam);

			Player player = playerGraphics.player;
			player.GetPlayerVar(out var pv);

			if (pv.myDebug != null)
			{
				pv.myDebug.InitiateSprites(sLeaser, rCam);
			}
		}

		public static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(playerGraphics, sLeaser, rCam, timeStacker, camPos);

			Player player = playerGraphics.player;
			player.GetPlayerVar(out var pv);

			if (pv.myDebug != null)
			{
				pv.myDebug.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			}
		}

		private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics playerGraphics)
		{
			// 保存原版 objectInStomach 状态
			Player player = playerGraphics.player;
			var originalObject = player.objectInStomach;

			player.GetPlayerVar(out var pv);
			var stomachData = pv.stomachData;

			if (stomachData.swallowOrRegurgitate)
			{
				bool Regurgitate;

				int inHand = InHand(player, true);
				if (inHand != -1 && !stomachData.IsFull)
				{
					Regurgitate = false;
				}
				else
				{
					Regurgitate = !stomachData.IsEmpty;
				}

				bool swallow = true;

				if (Regurgitate)
				{
					swallow = false;
				}
				if (stomachData.IsFull)
				{
					swallow = false;
				}

				//Log.LogInfo($"swallow: {swallow}");
				if (swallow)
				{
					player.objectInStomach = null;

				}
			}

			orig(playerGraphics);

			player.objectInStomach = originalObject;
		}

		public static void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand  hand)
		{
			if (hand.owner is PlayerGraphics playerGraphics)
			{
				// 保存原版 objectInStomach 状态
				Player player = playerGraphics.player;
				var originalObject = player.objectInStomach;

				player.GetPlayerVar(out var pv);
				var stomachData = pv.stomachData;

				if (stomachData.swallowOrRegurgitate)
				{
					bool Regurgitate;

					int inHand = InHand(player, true);
					if (inHand != -1 && !stomachData.IsFull)
					{
						Regurgitate = false;
					}
					else
					{
						Regurgitate = !stomachData.IsEmpty;
					}

					bool swallow = true;

					if (Regurgitate)
					{
						swallow = false;
					}
					if (stomachData.IsFull)
					{
						swallow = false;
					}

					Log.LogInfo($"swallow: {swallow}");
					if (swallow)
					{
						player.objectInStomach = null;

					}
				}

				orig(hand);

				player.objectInStomach = originalObject;
			}

		}


		public static int InHand(Player player, bool CanBeSwallowed)
		{
			int grasp = -1;
			for (int i = 0; i < player.grasps.Length; i++)
			{
				if (player.grasps[i] != null)
				{
					if (!CanBeSwallowed || player.CanBeSwallowed(player.grasps[i].grabbed))
					{
						grasp = i;
						break;
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


		public static void Player_ctor(On.Player.orig_ctor orig, Player player, AbstractCreature abs, World world)
		{
			//orig(player, abs, world);


			Log.LogDebug("Player ctor");

			/*int N = player.playerState.playerNumber;
			if (world.game.session is ArenaGameSession)//竞技场模式
			{
				if (GlobalVar.playerVars.TryGetValue(N, out _))
				{
					GlobalVar.playerVars.Remove(N);
				}
			}*/

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
			int N = player.playerState.playerNumber;

			if (stomachData.Current == null && stomachData.HistoryCount > 0)
			{
				stomachData.Current = stomachData.PopHistory();
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
					lanternCount = Math.Min(lanternCount, 4);

					float warmthMultiplier = 1f + (lanternCount - 1) * 0.8f;
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

			if (Plugin.EnableLog)
			{
				int now = player.room?.world.game.clock ?? -1;
				if (now % 40 == 0)
				{
					//Log.LogInfo($"N: {N}, pos: {player.mainBodyChunk.pos}");
				}
			}

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



			if (Input.GetKey("v"))
			{
				try
				{
					if (SteamManager.Initialized)
					{
						CSteamID steamID = SteamUser.GetSteamID();
						ulong steamId64 = steamID.m_SteamID;
						Log.LogInfo($"Steam ID: {steamId64}");
					}
					else
					{
						Log.LogInfo("Steam 尚未初始化，稍后再试");
					}
				}
				catch (Exception ex)
				{
					Log.LogWarning($"获取 Steam ID 失败: {ex}");
				}
			}

			if (Plugin.DebugMode)
			{

				if (Input.GetKey("c"))
				{
					if (pv.myDebug == null)
					{
						pv.myDebug = new MyDebug(player);
					}
				}

				if (pv.myDebug != null)
				{
					pv.myDebug.outStr_ = N.ToString();
				}


				if (Input.GetKeyDown("n"))
				{
					int grasp = InHand(player, false);

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

			bool CanSwallow = stomachData.Swallow(null);

			if (!CanSwallow)
			{
				Log.LogInfo($"无法吞咽物品");
				return;
			}

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

					/*if (!c.HasValue)
					{
						if (ModManager.MSC && player.objectInStomach.type == MoreSlugcatsEnums.AbstractObjectType.GlowWeed)
						{

						}
					}*/

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
