using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Extended
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
		}
		public static void Hook_()
		{
			On.Player.ctor -= Player_ctor;
			On.Player.Update -= Player_Update;
			On.Player.Destroy -= Player_Destroy;

			On.Player.SwallowObject -= Player_SwallowObject;
			On.Player.Regurgitate -= Player_Regurgitate;
		}

		private static void Player_ctor(On.Player.orig_ctor orig, Player player, AbstractCreature abs, World world)
		{
			orig(player, abs, world);

			Log.LogDebug("Player ctor");

			player.GetPlayerVar(out var pv);

			pv.coord = player.coord;

			if (pv.swallowedObjects.Count != 0)
			{
				foreach (var s in pv.swallowedObjects)
				{
					Log.LogDebug($"Trying to spawn {s}");

					var Object = Helper.ObjectFromString(s, player.room.world, player.coord, player.coord);
					
					if (Object != null)
					{
						Log.LogDebug($"Successfully spawn {s}");
						pv.objectsInStomach.Add(Object);
					}
				}
				pv.swallowedObjects.Clear();
			}
		}

		private static void Player_Update(On.Player.orig_Update orig, Player player, bool eu)
		{
			orig(player, eu);

			player.GetPlayerVar(out var pv);

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

			if (Input.GetKey("n"))
			{
				if (player.objectInStomach != null)
				{
					pv.objectsInStomach.Add(player.objectInStomach);
					player.objectInStomach = null;
				}
			}

			if (Input.GetKey("m"))
			{
				if (pv.objectsInStomach.Count > 0 && player.objectInStomach == null)
				{
					player.objectInStomach = pv.objectsInStomach[pv.objectsInStomach.Count - 1];
					pv.objectsInStomach.RemoveAt(pv.objectsInStomach.Count - 1);
				}
			}
		}

		private static void Player_Destroy(On.Player.orig_Destroy orig, Player player)
		{
			player.GetPlayerVar(out var pv);

			pv.coord = player.coord;

			orig(player);
		}

		private static void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player player, int grasp)
		{
            Log.LogInfo("===Before");

            player.GetPlayerVar(out var pv);

			if (!pv.HasSpace(player))
			{
				Log.LogInfo($"Has not space");
				return;
			}

			player.objectInStomach = null;

			orig(player, grasp);

			if (player.objectInStomach != null)
			{
				pv.objectsInStomach.Add(player.objectInStomach);

				Log.LogInfo($"吞咽成功！胃部物品数量: {pv.objectsInStomach.Count}");
				for (int i = 0; i < pv.objectsInStomach.Count; i++)
				{
					Log.LogInfo(pv.objectsInStomach[i].ToString());
				}
			}
			else
			{
				Log.LogInfo("原版吞咽函数没有处理物品");
			}
            Log.LogInfo("===After");
        }

		private static void Player_Regurgitate(On.Player.orig_Regurgitate orig, Player player)
		{
            Log.LogInfo("===Before");

            player.GetPlayerVar(out var pv);

            bool hasItems = pv.objectsInStomach.Count > 0;
            AbstractPhysicalObject? lastItem = hasItems ? pv.objectsInStomach[pv.objectsInStomach.Count - 1] : null;

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
            }

            Log.LogInfo("===After");
        }


	}
}
