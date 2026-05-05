using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Extended
{
	public static class GlobalVar
	{
		public static ConditionalWeakTable<Player, PlayerVar> playerVars = new ConditionalWeakTable<Player, PlayerVar>();

		public static void HookOn()
		{
			On.Player.ctor += Player_ctor;
		}

		private static void Player_ctor(On.Player.orig_ctor orig, Player player, AbstractCreature abstractCreature, World world)
		{
			orig.Invoke(player, abstractCreature, world);
			playerVars.Add(player, new PlayerVar(player));
		}

		public static PlayerVar GetPlayerVar(this Player player, out PlayerVar pv)
		{
			if (playerVars.TryGetValue(player, out PlayerVar pm_))
			{
				pv = pm_;
				return pv;
			}
			pv = new PlayerVar(player);
			playerVars.Add(player, pv);
			return pv;
		}
		public static PlayerVar GetPlayerVar(this Player player)
		{
			if (playerVars.TryGetValue(player, out PlayerVar pv))
			{
				return pv;
			}
			pv = new PlayerVar(player);
			playerVars.Add(player, pv);
			return pv;
		}
	}

	public class PlayerVar
	{
		WeakReference<Player> playerRef;

        //public bool StoreMore = false;
        public int StorageCapacity = 1;

        public PlayerVar(Player player)
		{
			playerRef = new WeakReference<Player>(player);


            if (true)
			{
				StorageCapacity = 5;
            }
		}
	}

}
