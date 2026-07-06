using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace ExtensionLib
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
            GlobalVar.game = world.game;

            int N = player.playerState.playerNumber;

            if (world.game.session is ArenaGameSession)//竞技场模式
            {
                if (GlobalVar.playerVars.TryGetValue(N, out PlayerVar pv))
                {
                    GlobalVar.playerVars.Remove(N);
                }
            }

			//playerVars.Add(player, new PlayerVar(player));
			player.GetPlayerVar();

			MyPlayer.Player_ctor(orig, player, abstractCreature, world);
        }

		public static PlayerVar GetPlayerVar(this Player player)
		{
			int N = player.playerState.playerNumber;

			if (playerVars.TryGetValue(N, out PlayerVar pv))
			{
				player.BindPlayerRef(pv);
				return pv;
			}
			pv = new PlayerVar(player);
			player.BindPlayerRef(pv);
			playerVars.Add(N, pv);
			return pv;
		}
		public static PlayerVar GetPlayerVar(this Player player, out PlayerVar pv)
		{
			pv = GetPlayerVar(player);
			return pv;
		}

		public static bool BindPlayerRef(this Player player, PlayerVar pv)
		{
			// 检查现有引用是否有效
			if (pv.PlayerRef.TryGetTarget(out Player? target) && target != null && target == player && !target.slatedForDeletetion)
			{
				// 引用有效且对象未被标记删除，不替换
				return false;
			}

			// 绑定新引用
			pv.SetPlayerRef(player);
			return true;
		}
		#endregion
	}
}
