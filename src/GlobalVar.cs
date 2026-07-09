using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using UnityEngine;


namespace ExtensionLib
{
	public static class GlobalVar
	{
		//玩家变量

		public static ConditionalWeakTable<Player, PlayerModule> playerModules = new();
		public static Dictionary<string, PlayerVar> playerVars = new();

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
				if (GlobalVar.playerVars.TryGetValue(N.ToString(), out _))
				{
					GlobalVar.playerVars.Remove(N.ToString());
				}
			}

			//playerVars.Add(player, new PlayerVar(player));
			if (Plugin.DebugMode)
			{
				player.GetPlayerModule(out var module);

				module.myDebug = new MyDebug(player);
			}

			MyPlayer.Player_ctor(orig, player, abstractCreature, world);
		}

		public static PlayerModule GetPlayerModule(this Player player)
		{
			if (playerModules.TryGetValue(player, out PlayerModule Module))
			{
				return Module;
			}
			Module = new PlayerModule(player);
			playerModules.Add(player, Module);
			return Module;
		}
		public static PlayerModule GetPlayerModule(this Player player, out PlayerModule module)
		{
			module = GetPlayerModule(player);
			return module;
		}

		public static bool EnableStomach(this Player player)
		{
			player.GetPlayerModule(out PlayerModule module);
			if (module.StomachExtension)
			{
				if (MeadowCompat.IsModEnabled_RainMeadow)
				{
					if (MeadowCompat.IsMeadowStoryMode())
					{
						if (MeadowCompat.Compat && !MeadowCompat.IsMe(player))
						{
							return false;
						}
					}
				}
				return true;
			}
			return false;
		}

		public static PlayerVar GetPlayerVar(this Player player)
		{
			string key = GetPlayerKey(player);

			if (playerVars.TryGetValue(key, out PlayerVar pv))
			{
				player.BindPlayerRef(pv);
				return pv;
			}
			pv = new PlayerVar(player);
			player.BindPlayerRef(pv);
			playerVars.Add(key, pv);
			return pv;
		}
		public static PlayerVar GetPlayerVar(this Player player, out PlayerVar pv)
		{
			pv = GetPlayerVar(player);
			return pv;
		}
		public static string GetPlayerKey(Player player)
		{
			player.GetPlayerModule(out var module);
			if (module.key != "" && module.key != "Null")
			{
				return module.key;
			}

			if (MeadowCompat.IsModEnabled_RainMeadow)
			{
				if (MeadowCompat.IsMeadowStoryMode())
				{
					string UniqueID = MeadowCompat.GetUniqueID(player);
					if (UniqueID != "Null" && UniqueID != "")
					{
						module.key = UniqueID;
						return UniqueID;
					}
					else
					{
						
					}
				}
			}
			string N = player.playerState.playerNumber.ToString();
			module.key = N;
			return N;
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

		#region Steam
		public static bool GetSteamID(out ulong steamID)
		{
			try
			{
				if (SteamManager.Initialized)
				{
					CSteamID cSteamID = SteamUser.GetSteamID();
					steamID = cSteamID.m_SteamID;
					Log.LogInfo($"Steam ID: {steamID}");
					return true;
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
			steamID = 0;
			return false;
		}
		public static bool GetSteamName(out string steamName)
		{
			try
			{
				if (SteamManager.Initialized)
				{
					steamName = SteamFriends.GetPersonaName();
					// 例如，获取某个好友的名字
					//CSteamID friendID = new CSteamID(/* 这里填好友的64位SteamID */);
					//string friendName = SteamFriends.GetFriendPersonaName(friendID);
					Log.LogInfo($"Steam Name: {steamName}");
					return true;
				}
				else
				{
					Log.LogInfo("Steam 尚未初始化，稍后再试");
				}
			}
			catch (Exception ex)
			{
				Log.LogWarning($"获取 Steam Name 失败: {ex}");
			}
			steamName = string.Empty;
			return false;
		}
		#endregion

	}
}
