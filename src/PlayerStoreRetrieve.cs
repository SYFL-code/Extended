using BepInEx;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using MoreSlugcats;
using RWCustom;
using JetBrains.Annotations;
using Expedition;
using Noise;
using Random = UnityEngine.Random;
using Color = UnityEngine.Color;
using System.Linq;
using System.Drawing;
using System.Xml.Schema;
using ObjType = AbstractPhysicalObject.AbstractObjectType;
using static UnityEngine.Input;
using static StoredData;
using static ExtendedData;


namespace Extended
{
	public static class PlayersStoreRetrieve
	{
		public static void HookAdd()
		{
			On.Player.Update += Player_Update;
			//On.PlayerGraphics.DrawSprites += Player_DrawSprites;
			//On.SlugcatHand.Update += SlugcatHand_Update;

			//On.Player.StomachGlowLightColor += StomachGlowLightColor;
			//On.Player.UpdateMSC += Player_UpdateMSC;
		}
		public static void HookSubtract()
		{
			On.Player.Update -= Player_Update;
			//On.PlayerGraphics.DrawSprites -= Player_DrawSprites;
			//On.SlugcatHand.Update -= SlugcatHand_Update;

			//On.Player.StomachGlowLightColor += StomachGlowLightColor;
			//On.Player.UpdateMSC -= Player_UpdateMSC;
		}

		/*public static bool StorageMore(this Player pl, out int capacity)
		{
			pl.GetPlayerVar(out var pv);
			capacity = pv.StorageCapacity;
			return capacity != 1;
		}*/

		public static void Player_Update(On.Player.orig_Update orig, Player player, bool eu)
		{
			orig(player, eu);

			player.GetPlayerVar(out var pv);
			var data = pv.playerStoreRetrieve;

			data.Update();

			/*if (ModManager.MSC)
			{
				player.Hypothermia -= Mathf.Lerp(data.HeatSources(), 0f, player.HypothermiaExposure);
			}*/
		}

        /*/*public static void MSCUpdate(On.Player.orig_UpdateMSC orig, Player player)
		{
			orig(player);

			player.GetPlayerVar(out var pv);
			var data = pv.playerStoreRetrieve;
		}*//*

		public static void Player_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);

			if (self.player is Player player)
			{
				player.GetPlayerVar(out var pv);
				var data = pv.playerStoreRetrieve;

				if (data.SwallowOrRegurgitateCoutner > 0 && data.CanRetrieve())
				{
					data.UpdateRegurgitationGraphics(self, sLeaser);
				}
			}
		}


		public static void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand self)
		{
			orig(self);
			if (self.owner.owner is Player player && player.StorageMore(out var capacity))
			{
				player.GetPlayerVar(out var pv);
				var data = pv.playerStoreRetrieve;

				if (data.SwallowOrRegurgitateCoutner > 10)
				{
					int num3 = -1;
					int num4 = 0;
					while (num3 < 0 && num4 < 2)
					{
						if (player.grasps[num4] != null && data.CanStore())
						{
							num3 = num4;
						}
						num4++;
					}
					if (num3 == self.limbNumber)
					{
						float num5 = Mathf.InverseLerp(10f, 90f, (float)data.SwallowOrRegurgitateCoutner);
						if (num5 < 0.5f)
						{
							self.relativeHuntPos *= Mathf.Lerp(0.9f, 0.7f, num5 * 2f);
							self.relativeHuntPos.y = self.relativeHuntPos.y + Mathf.Lerp(2f, 4f, num5 * 2f);
							self.relativeHuntPos.x = self.relativeHuntPos.x * Mathf.Lerp(1f, 1.2f, num5 * 2f);
						}
						else if (self.owner is PlayerGraphics playerGraphics)
						{
							playerGraphics.blink = 5;
							self.relativeHuntPos = new Vector2(0f, -4f) + Custom.RNV() * 2f * Random.value * Mathf.InverseLerp(0.5f, 1f, num5);
							playerGraphics.head.vel += Custom.RNV() * 2f * Random.value * Mathf.InverseLerp(0.5f, 1f, num5);
							self.owner.owner.bodyChunks[0].vel += Custom.RNV() * 0.2f * Random.value * Mathf.InverseLerp(0.5f, 1f, num5);
						}
					}
				}
			}
		}

		*//*public static Color? StomachGlowLightColor(On.Player.orig_StomachGlowLightColor orig, Player player)
		{
			player.GetPlayerVar(out var pv);
			var data = pv.playerStoreRetrieve;

			if (self.TryGetCloud(out var data))
			{
				if (data.Storage.Length > 0)
				{
					if (data.StorageContains(AbstractPhysicalObject.AbstractObjectType.Lantern, false, null))
					{
						return new Color?(new Color(1f, 0.4f, 0.3f, 0.85f));
					}
					if (data.StorageContains(AbstractPhysicalObject.AbstractObjectType.NSHSwarmer, false, null)) //|| data.StorageContains(MoreSlugcatsEnums.AbstractObjectType.GlowWeed, false, null))
					{
						return new Color?(new Color(0.2f, 1f, 0.3f, 0.45f));
					}
					if (data.StorageContains(AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer, false, null))
					{
						return new Color?(new Color(1f, 1f, 1f, 0.35f));
					}
				}
				return null;
			}
			return orig(self);
		}*//*



		public class PlayerStoreRetrieve
		{
			public PlayerStoreRetrieve(Player pl)
			{
				playerRef = new WeakReference<Player>(pl);
				//Storage = new AbstractPhysicalObject[capacity]; // 容量 5
				SwallowOrRegurgitateCoutner = 0;                // 吞咽/呕吐长按计数器
				GraspToTakeFrom = -1;                            // 未被指定取物手
			}

			WeakReference<Player> playerRef;
			public Player player 
			{
				get 
				{
					playerRef.TryGetTarget(out Player pl);
					return pl; 
				}

			}

			/// <summary> 记录触发吞咽时使用的“手”索引 (0或1) </summary>
			public int GraspToTakeFrom;

			/// <summary>
			/// 仓储是否已满
			/// (所有槽位均非空)
			/// </summary>
			public bool Stuffed
			{
				get
				{
					return FreeSlot() == -1;
				}
			}

			/// <summary>
			/// 是否具有漂浮效果
			/// (腹中含有能量细胞时启用)
			/// </summary>
			//public bool Floaty;

			/// <summary>
			/// 吞咽/呕吐蓄力计数器
			/// 满足条件时每帧 +1，达到 90 帧触发动作
			/// </summary>
			public int SwallowOrRegurgitateCoutner;

			public bool StorageInput
			{
				get
				{
					int num = player.playerState.playerNumber;

					return Input.GetKey("y");

					*//*if (num == 0)
					{
						return Input.GetKey(CloudtailOptions.Instance.KeyCode1.Value);
					}
					else if (num == 1)
					{
						return Input.GetKey(CloudtailOptions.Instance.KeyCode2.Value);
					}
					else if (num == 2)
					{
						return Input.GetKey(CloudtailOptions.Instance.KeyCode3.Value);
					}
					else
					{
						return Input.GetKey(CloudtailOptions.Instance.KeyCode4.Value);
					}*//*
				}
			}

			/// <summary>
			/// 检查当前是否可以执行“吞入”操作
			/// 条件：
			///   1. 无移动输入 + 按下自定义键
			///   2. 未在水中过深 (submersion <= 0.5)
			///   3. 腹中仍有空位
			///   4. 手中有非食用品 (或普通物品)
			///   5. 至少一只手非空
			/// </summary>
			public bool CanStore()
			{
				// 无方向键移动 + 按下自定义存储键
				bool Storeinputs = player.input[0].x == 0 && player.input[0].y == 0 && StorageInput;

				if (player.room == null) return false;              // 房间不存在
				if (!Storeinputs) return false;                   // 未按下存储键
				if (player.firstChunk.submersion > 0.5f) return false; // 半身入水时禁止
				if (Stuffed) return false;                        // 腹中已满

				// 双手均空，无法吞入
				if (player.grasps[0] == null && player.grasps[1] == null) return false;

				int handnum = -1;

				// 检查手中物品
				for (int i = 0; i < player.grasps.Length; i++)
				{
					// 如果是可食物品，不允许吞入（因可直接吃，防止绕过食物系统）
					if (player.grasps[i] != null && player.grasps[i].grabbed is IPlayerEdible food && food.Edible)
						return false;

					// 记录第一个非空手的索引
					if (player.grasps[i] != null)
					{
						handnum = i;
						break;
					}
				}

				if (handnum == -1) return false;

				// 记住将要拿取物品的手
				GraspToTakeFrom = handnum;

				return true;
			}

			/// <summary>
			/// 检查当前是否可以执行“吐出”操作
			/// 条件：
			///   1. 按下投掷键
			///   2. 未在吃肉动作中 (eatMeat <= 20)
			///   3. 未按拾取键 / 跳跃键
			///   4. 未在水中过深
			///   5. 至少一只手空闲
			/// </summary>
			public bool CanRetrieve()
			{
				return player.input[0].thrw
					&& player.eatMeat <= 20
					&& !player.input[0].pckp
					&& !player.input[0].jmp
					&& player.firstChunk.submersion < 0.5f
					&& player.room != null
					&& player.FreeHand() != -1;
			}

			/// <summary>
			/// 每帧更新
			/// - 检测吞/吐条件，累积计数器
			/// - 达到 90 帧阈值时执行吞或吐
			/// - 更新 Floaty 状态 (是否含能量细胞)
			/// </summary>
			public void Update()
			{
				if (player.room == null) return; // 房间未就绪，跳过

				bool s = CanStore();    // 是否可吞
				bool r = CanRetrieve(); // 是否可吐

				// 满足任一条件，累加蓄力计数器
				if (r || s)
				{
					SwallowOrRegurgitateCoutner++;

					// 蓄力满 90 帧 (~1.5秒)，触发动作
					if (SwallowOrRegurgitateCoutner > 90)
					{
						if (s)
						{
							Swallow(GraspToTakeFrom);

							// 触发吞物动画效果
							if (player.graphicsModule != null && player.graphicsModule is PlayerGraphics playerGraphics)
								playerGraphics.swallowing = 20;
						}
						else
						{
							Regurgitate();
						}

						SwallowOrRegurgitateCoutner = 0; // 重置计数器
					}
				}
				else
				{
					// 条件不满足，立即归零（避免误触）
					SwallowOrRegurgitateCoutner = 0;
				}

				// 未在合成物品时，重置原生吞/吐计数器
				if (!player.craftingObject)
					player.swallowAndRegurgitateCounter = 0;

				// 检测是否含有能量细胞 (用于悬浮效果)
				//Floaty = StorageContains(MoreSlugcatsEnums.AbstractObjectType.EnergyCell, false, null);
			}

			public void UpdateRegurgitationGraphics(PlayerGraphics playerGraphics, RoomCamera.SpriteLeaser sLeaser)
			{
				// 蓄力超 15 帧开始眨眼
				if (SwallowOrRegurgitateCoutner > 15)
				{
					playerGraphics.blink = 5;
				}

				// 计算波动强度 (num10) 和波动频率参数 (num11)
				float num10 = Mathf.InverseLerp(0f, 110f, (float)SwallowOrRegurgitateCoutner);
				float num11 = (float)SwallowOrRegurgitateCoutner / Mathf.Lerp(30f, 15f, num10);

				if (playerGraphics.player.standing)
				{
					// === 站立姿态：身体呈上下垂直波动 ===
					sLeaser.sprites[0].y += Mathf.Sin(num11 * 3.1415927f * 2f) * num10 * 2f;
					sLeaser.sprites[1].y += -Mathf.Sin((num11 + 0.2f) * 3.1415927f * 2f) * num10 * 3f;
					sLeaser.sprites[3].y += Mathf.Sin(num11 * 3.1415927f * 2f) * num10 * 2f;
					sLeaser.sprites[9].y += Mathf.Sin(num11 * 3.1415927f * 2f) * num10 * 2f;
				}
				else
				{
					// === 非站立姿态 (匍匐/倒挂等)：加入水平方向波动 ===
					sLeaser.sprites[0].y += Mathf.Sin(num11 * 3.1415927f * 2f) * num10 * 3f;
					sLeaser.sprites[0].x += Mathf.Cos(num11 * 3.1415927f * 2f) * num10 * 1f;
					sLeaser.sprites[1].y += Mathf.Sin((num11 + 0.2f) * 3.1415927f * 2f) * num10 * 2f;
					sLeaser.sprites[1].x += -Mathf.Cos(num11 * 3.1415927f * 2f) * num10 * 3f;
					sLeaser.sprites[3].y += Mathf.Sin(num11 * 3.1415927f * 2f) * num10 * 3f;
					sLeaser.sprites[3].x += Mathf.Cos(num11 * 3.1415927f * 2f) * num10 * 1f;
					sLeaser.sprites[9].y += Mathf.Sin(num11 * 3.1415927f * 2f) * num10 * 3f;
					sLeaser.sprites[9].x += Mathf.Cos(num11 * 3.1415927f * 2f) * num10 * 1f;
				}
			}

			/// <summary>
			/// 执行吞入操作
			/// 将手中物品/生物的抽象对象存入 Storage 数组，并从世界中移除其物理对象
			/// </summary>
			/// <param name="grasp">要吞入的手的索引 (0 或 1)</param>
			public void Swallow(int grasp)
			{
				// 获取手中抓取的物理对象
				PhysicalObject abstractPhysicalObject = player.grasps[grasp].grabbed;

				// 寻找第一个空槽位
				int storeindex = FreeSlot();
				if (storeindex == -1) return; // 无空槽位，取消

				// ————— 特殊物品处理 —————

				// 矛：清除插墙计数器（防止吞入卡状态的矛）
				if (abstractPhysicalObject is Spear spear)
				{
					spear.abstractSpear.stuckInWallCycles = 0;
				}

				// 带电电矛：不能吞入，触发强烈电击惩罚
				if (abstractPhysicalObject is ElectricSpear electricSpear && electricSpear.abstractSpear.electricCharge > 0)
				{
					player.room.AddObject(new ZapCoil.ZapFlash(player.firstChunk.pos, 10f));
					player.room.PlaySound(SoundID.Zapper_Zap, player.firstChunk.pos, 1f, 1.5f + Random.value * 1.5f);

					// 若在水中，产生更强电击扩散
					if (player.Submersion > 0.5f)
					{
						player.room.AddObject(new UnderwaterShock(player.room, null, player.firstChunk.pos, 10, 800f, 2f, player, new Color(0.8f, 0.8f, 1f)));
					}

					player.Stun(200);                                        // 晕眩 200 帧
					player.room.AddObject(new CreatureSpasmer(player, false, 200)); // 身体抽搐
					return; // 终止吞入
				}

				// 水母：不能吞入，触发较弱电击惩罚
				if (abstractPhysicalObject is JellyFish)
				{
					player.room.PlaySound(SoundID.Centipede_Shock, player.firstChunk.pos);
					player.room.AddObject(new UnderwaterShock(player.room, player, player.firstChunk.pos, 10, 100f, 0f, null, new Color(0.8f, 0.8f, 1f)));
					player.room.AddObject(new CreatureSpasmer(player, false, 60));
					player.Stun(80);
					return;
				}

				// ————— 正常吞入流程 —————

				// 获取抽象对象（这是保存的关键）
				var ToSwallow = abstractPhysicalObject.abstractPhysicalObject;

				// 存入仓储数组
				//Storage[storeindex] = ToSwallow;
				StoreRetrieve.StoreItem(player, ToSwallow);

				// 松开手
				player.ReleaseGrasp(grasp);

				// 从房间移除物理对象
				//abstractPhysicalObject.abstractPhysicalObject.realizedObject.RemoveFromRoom();

				// 抽象化（保留抽象对象，销毁物理实体）
				//abstractPhysicalObject.abstractPhysicalObject.Abstractize(player.abstractCreature.pos);

				// 从房间实体列表中移除
				//abstractPhysicalObject.abstractPhysicalObject.Room.RemoveEntity(abstractPhysicalObject.abstractPhysicalObject.ID);

				// 身体小弹跳反馈
				BodyChunk mainBodyChunk = player.mainBodyChunk;
				mainBodyChunk.vel.y += 2f;

				// 播放吞物音效
				player.room.PlaySound(SoundID.Slugcat_Swallow_Item, player.mainBodyChunk);
			}

			/// <summary>
			/// 执行吐出操作
			/// LIFO (后进先出)：最后存入的物体优先吐出
			/// 若仓储为空但有饱食度，可消耗 1 饱食度随机生成物品
			/// 若两者皆空，产生晕眩惩罚
			/// </summary>
			public void Regurgitate()
			{
				// 从后往前查找最后一个非空槽位 (LIFO 栈顶)
				int index = ItemInQueue();
				AbstractPhysicalObject? stomach;
				//bool PluckedFromNowhere = false; // 是否为凭空变出的物品

				if (index == -1 && false) // 仓储中没有存储物
				{
					*//*// 角色胃中有正常食物点数 → 消耗饱食度凭空变物
					if (player.FoodInStomach >= 1)
					{
						stomach = RandItem(player);    // 从随机池中生成一个物品
						PluckedFromNowhere = true;   // 标记为凭空生成
					}
					else
					{
						// 胃和仓储皆空 → 空吐惩罚：晕眩 + 增加劳累值
						player.Stun(80);
						player.AerobicIncrease(12f);
						return;
					}*//*
				}
				else
				{
					// 正常取出仓储中的抽象对象
					//stomach = Storage[index];//
					stomach = StoreRetrieve.RetrieveItem(player);
				}

				if (stomach == null)
				{
					return;
				}

				// ————— 将抽象对象重新实体化回世界 —————

				// 重新绑定当前世界
				//stomach.world = player.abstractCreature.world;

				// 如果是生物，重新绑定 AI 到当前世界
				*//*if (stomach is AbstractCreature c)
				{
					c.abstractAI?.NewWorld(player.room.world);
				}*//*

				// 重新加入房间抽象实体列表
				//player.room.abstractRoom.AddEntity(stomach);

				// 设置生成位置（在角色位置）
				//stomach.pos = player.abstractCreature.pos;

				// 实体化：根据抽象对象生成物理对象
				//stomach.RealizeInRoom();

				// 消耗 1 点饱食度（如果是凭空变出）
				*//*if (PluckedFromNowhere)
				{
					player.SubtractFood(1);
				}*//*

				// ————— 物理反馈：从嘴部喷射出物体 —————

				Vector2 vector = player.bodyChunks[0].pos;             // 头部位置
				Vector2 a = Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos); // 身体方向
				bool flag = false;

				// 若角色近乎直立且头在上方，调整喷出方向和位置
				if (Mathf.Abs(player.bodyChunks[0].pos.y - player.bodyChunks[1].pos.y) > Mathf.Abs(player.bodyChunks[0].pos.x - player.bodyChunks[1].pos.x)
					&& player.bodyChunks[0].pos.y > player.bodyChunks[1].pos.y)
				{
					vector += Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos) * 5f;
					a *= -1f;
					a.x += 0.4f * (float)player.flipDirection;
					a.Normalize();
					flag = true;
				}

				// 硬设置物体位置和速度（模拟吐出）
				stomach.realizedObject.firstChunk.HardSetPosition(vector);
				stomach.realizedObject.firstChunk.vel = Vector2.ClampMagnitude(
					(a * 2f + Custom.RNV() * Random.value) / stomach.realizedObject.firstChunk.mass,
					6f
				);

				// 反冲：角色身体轻微后退
				player.bodyChunks[0].pos -= a * 2f;
				player.bodyChunks[0].vel -= a * 2f;

				// 头部抖动效果
				if (player.graphicsModule != null && player.graphicsModule is PlayerGraphics playerGraphics)
				{
					playerGraphics.head.vel += Custom.RNV() * Random.value * 3f;
				}

				// 口水/水滴特效 x3
				for (int i = 0; i < 3; i++)
				{
					player.room.AddObject(new WaterDrip(
						vector + Custom.RNV() * Random.value * 1.5f,
						Custom.RNV() * 3f * Random.value + a * Mathf.Lerp(2f, 6f, Random.value),
						false
					));
				}

				// 播放呕吐音效
				player.room.PlaySound(SoundID.Slugcat_Regurgitate_Item, player.mainBodyChunk);

				// 如果满足条件且有空手，自动抓住吐出的物体
				if (flag && player.FreeHand() > -1)
				{
					player.SlugcatGrab(stomach.realizedObject, player.FreeHand());
				}

				// 清空对应槽位（如果是正常吐出而非凭空变物）
				*//*if (!PluckedFromNowhere)
				{
					Storage[index] = null;
				}*//*
			}

			/// <summary>
			/// 计算腹中存储物带来的保暖效果
			/// 每多一盏灯笼 (Lantern)，保暖倍数 +1
			/// </summary>
			public float HeatSources()
			{
				float heat = RainWorldGame.DefaultHeatSourceWarmth; // 基础温暖值
				float num = 1; // 基础倍率

				StoredData storedData = ExtendedData.GetStoredData(player);

				if (storedData.storedObjects == null) return heat;
				List<StoredObject?> storedObjects = storedData.storedObjects;

				// 统计灯笼数量
				for (int i = 0; i < storedObjects.Count; i++)
				{
					if (storedObjects[i] != null && storedObjects[i]!.type == ObjType.Lantern)
					{num++;}
				}

				heat *= num; // 应用倍率
				return heat;
			}

			/// <summary>
			/// 查找第一个空槽位的索引
			/// 用于吞入时寻找存放位置
			/// </summary>
			/// <returns>空槽位索引，-1 表示已满</returns>
			public int FreeSlot()
			{
				StoredData storedData = ExtendedData.GetStoredData(player);
				if (storedData.storedObjects == null) return -1;

				List<int> itemsIndexes = storedData.GetUsedIndices();

				if (itemsIndexes.Count >= storedData.slots)
				{
					UDebug.LogError("INV: Inventory full!");
					player.room.PlaySound(SoundID.MENU_Error_Ping);
					return -1;
				}

				int index = 0;

				if (itemsIndexes.Count > 0)
				{
					index = itemsIndexes[itemsIndexes.Count - 1] + 1;
				}

				return index; // 仓储已满
			}


			/// <summary>
			/// 查找最后一个非空槽位的索引 (LIFO 栈顶)
			/// 用于吐出时确定要取出的物体
			/// </summary>
			/// <returns>最后一个非空槽位的索引，-1 表示仓储为空</returns>
			public int ItemInQueue()
			{
				StoredData storedData = ExtendedData.GetStoredData(player);
				if (storedData.storedObjects == null) return -1;

				List<int> itemsIndexes = storedData.GetUsedIndices();

				int index = -1;
				if (itemsIndexes.Count > 0)
				{
					index = itemsIndexes[itemsIndexes.Count - 1];
				}

				return index; // 仓储已满
			}

			/// <summary>
			/// 检查腹中是否含有指定类型的物品或生物
			/// </summary>
			/// <param name="objType">物品的抽象对象类型</param>
			/// <param name="CheckForCreatures">false=检查物品 / true=检查生物</param>
			/// <param name="creatType">(仅 CheckForCreatures=true 时) 要匹配的生物类型</param>
			/// <returns>是否包含</returns>
			public bool StorageContains(ObjType objType, bool CheckForCreatures, CreatureTemplate.Type? creatType)
			{
				StoredData storedData = ExtendedData.GetStoredData(player);
				if (storedData.storedObjects == null) return false;
				List<StoredObject?> storedObjects = storedData.storedObjects;

				if (!CheckForCreatures)
				{
					// 匹配物品类型
					for (int i = 0; i < storedObjects.Count; i++)
					{
						if (storedObjects[i]?.type == objType)
						{
							return true;
						}
					}
					return false;
				}
				else
				{
					// 匹配生物类型
					for (int i = 0; i < storedObjects.Count; i++)
					{
						
						if (storedObjects[i] != null)
						{
							AbstractCreature apo = SaveState.AbstractCreatureFromString(player.room.world, storedObjects[i]!.data, false, default(WorldCoordinate));
							if (apo.creatureTemplate.type == creatType)
							{
								return true;
							}
						}
					}
					return false;
				}
			}



		}*/

    }
}
