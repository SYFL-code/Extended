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


namespace Extended
{
	public static class PlayersStoreRetrieve
	{


		public static bool StorageMore(this Player pl, out int capacity)
		{
			pl.GetPlayerVar(out var pv);
			capacity = pv.StorageCapacity;
			return capacity != 1;
		}


		public class PlayerStoreRetrieve
		{
			public PlayerStoreRetrieve(Player pl)
			{
				this.player = pl;
				Storage = new AbstractPhysicalObject[capacity]; // 容量 5
				SwallowOrRegurgitateCoutner = 0;                // 吞咽/呕吐长按计数器
				GraspToTakeFrom = -1;                            // 未被指定取物手
			}

			public Player player;

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
			public bool Floaty;

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

					if (num == 0)
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
					}
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
				Floaty = StorageContains(MoreSlugcatsEnums.AbstractObjectType.EnergyCell, false, null);
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
                abstractPhysicalObject.abstractPhysicalObject.realizedObject.RemoveFromRoom();

                // 抽象化（保留抽象对象，销毁物理实体）
                abstractPhysicalObject.abstractPhysicalObject.Abstractize(player.abstractCreature.pos);

                // 从房间实体列表中移除
                abstractPhysicalObject.abstractPhysicalObject.Room.RemoveEntity(abstractPhysicalObject.abstractPhysicalObject.ID);

                // 身体小弹跳反馈
                BodyChunk mainBodyChunk = player.mainBodyChunk;
                mainBodyChunk.vel.y += 2f;

                // 播放吞物音效
                player.room.PlaySound(SoundID.Slugcat_Swallow_Item, player.mainBodyChunk);
            }



        }

	}
}
