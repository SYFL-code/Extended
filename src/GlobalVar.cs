using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace Extended
{
	public static class GlobalVar
	{
        //玩家变量
        public static Dictionary<int, PlayerVar> playerVars = new();
        //全局系统变量
        public static RainWorldGame? game = null;

        #region 玩家变量
        public static void Hook()
		{
			On.Player.ctor += Player_ctor;
		}
        public static void Hook_()
        {
            On.Player.ctor -= Player_ctor;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player player, AbstractCreature abstractCreature, World world)
		{
			orig.Invoke(player, abstractCreature, world);

            //赋值给全局变量供其他函数使用
            GlobalVar.game = player.room.world.game;

			int N = player.playerState.playerNumber;

            //playerVars.Add(player, new PlayerVar(player));
            if (!playerVars.TryGetValue(N, out _))
			{
				playerVars.Add(N, new PlayerVar(player));
			}
		}

		public static PlayerVar GetPlayerVar(this Player player, out PlayerVar pv)
		{
            int N = player.playerState.playerNumber;

            if (playerVars.TryGetValue(N, out PlayerVar pm_))
			{
				pv = pm_;
				return pv;
			}
			pv = new PlayerVar(player);
			playerVars.Add(N, pv);
			return pv;
		}
		public static PlayerVar GetPlayerVar(this Player player)
		{
            int N = player.playerState.playerNumber;

            if (playerVars.TryGetValue(N, out PlayerVar pv))
			{
				return pv;
			}
			pv = new PlayerVar(player);
			playerVars.Add(N, pv);
			return pv;
		}
        #endregion
    }
}
