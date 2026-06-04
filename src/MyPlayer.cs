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
        }
		public static void Hook_()
		{
			On.Player.ctor -= Player_ctor;
			On.Player.Update -= Player_Update;
			On.Player.Destroy -= Player_Destroy;
        }

		private static void Player_ctor(On.Player.orig_ctor orig, Player player, AbstractCreature abs, World world)
		{
			orig(player, abs, world);

			player.GetPlayerVar(out var pv);

			pv.coord = player.coord;

            if (pv.swallowedObjectsTemp.Count != 0)
			{
                foreach (var s in pv.swallowedObjectsTemp)
                {
					var Object = Helper.ObjectFromString(s, player.room.world, player.coord, player.coord);
					
					if (Object != null)
                    {
                        pv.objectsInStomach.Add(Object);
                    }
                }
				//pv.swallowedObjectsTemp.Clear();
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



    }
}
