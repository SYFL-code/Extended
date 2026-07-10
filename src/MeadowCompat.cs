using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ExtensionLib.GlobalVar;
using static ExtensionLib.SaveFile;
using RainMeadow;

namespace ExtensionLib;

public static class MeadowCompat
{

	public static bool IsModEnabled_RainMeadow => 
		ModManager.ActiveMods.Any(m => m.id == "henpemaz.rainmeadow" || m.id == "henpemaz_rainmeadow");

	public static bool RainMeadow_IsHost => !IsModEnabled_RainMeadow || MeadowCompat.IsHost;
	public static bool RainMeadow_IsOnline => IsModEnabled_RainMeadow && MeadowCompat.IsOnline;
	public static bool IsHost => !MeadowCompat.IsOnline || OnlineManager.lobby.isOwner;
	public static bool IsOnline => OnlineManager.lobby != null;
	public static bool IsOnlineFriendlyFire => RainMeadow.RainMeadow.isStoryMode(out StoryGameMode? storyGameMode) && storyGameMode.friendlyFire;

	public static bool Compat
	{
		get
		{
			//return false;

			if (MeadowCompat.IsModEnabled_RainMeadow)
			{
				if (MeadowCompat.IsMeadowStoryMode())
				{
					return !MeadowCompat.AllPlayersHaveMod(Plugin.GUID);
					//return true;
				}
			}
			return false;
		}
	}

	public static void InitModCompat()
	{
		if (IsModEnabled_RainMeadow)
		{
			MeadowCompat.InitCompat();
		}
	}

	internal static void InitCompat()
	{
		OnlineResource.OnAvailable += MeadowCompat.OnlineResourceOnOnAvailable;
	}
	// 可用的在线资源
	private static void OnlineResourceOnOnAvailable(OnlineResource resource)
	{
	}


	public static bool IsMeadowStoryMode(out StoryGameMode? gameMode)
	{
		gameMode = null;
		if (IsModEnabled_RainMeadow && RainMeadow.RainMeadow.isStoryMode(out var meadowMode))
		{
			gameMode = meadowMode;
			return true;
		}
		return false;
	}
	public static bool IsMeadowStoryMode()
	{
		if (!IsModEnabled_RainMeadow)
		{
			return false;
		}
		return IsModEnabled_RainMeadow && IsMeadowStoryMode(out _);
	}

	public static string GetLobbyIdentifier()
	{
		if (MeadowCompat.IsModEnabled_RainMeadow)
		{
			if (MeadowCompat.IsMeadowStoryMode())
			{
				if (OnlineManager.lobby == null) return "null";

				// 使用 MatchmakingManager 的当前实例
				var manager = MatchmakingManager.currentInstance;
				if (manager == null) return "null";

				// 获取大厅 ID（Steam 大厅的 ID）
				var lobbyId = manager.GetLobbyID();
				if (lobbyId != null)
				{
					return lobbyId.ToString();
				}
			}
		}
		return "null";
	}

	public static OnlinePlayer? GetOnlinePlayer(this Player player)
	{
		if (IsModEnabled_RainMeadow)
		{
			if (IsMeadowStoryMode())
			{
				// // 1. 获取 OnlineObject
				if (player.abstractCreature.GetOnlineObject(out OnlinePhysicalObject? opo) && opo != null)
				{
					return opo.owner;
				}
			}
		}
		Log.LogWarning($"GetOnlinePlayer: Failed to get OnlinePlayer for {player}");
		return null;
	}
	public static string GetUniqueID(Player player)
	{
		if (IsModEnabled_RainMeadow)
		{
			if (IsMeadowStoryMode())
			{
				var onlinePlayer = GetOnlinePlayer(player);
				if (onlinePlayer != null)
				{
					return GetUniqueID(onlinePlayer);
				}
			}
		}
		Log.LogWarning($"GetUniqueID: Failed to get UniqueID for {player}");
		return "Null";
	}
	public static string GetUniqueID(OnlinePlayer onlinePlayer)
	{
		if (IsModEnabled_RainMeadow)
		{
			if (IsMeadowStoryMode())
			{
				if (onlinePlayer != null)
				{
					if (onlinePlayer.id is SteamMatchmakingManager.SteamPlayerId steamPlayerID)
					{
						string uniqueID = onlinePlayer.GetUniqueID();
						string personaName = onlinePlayer.id.GetPersonaName();

						//steamMapping[personaName] = uniqueID;

						return $"{uniqueID}";
						//return $"{onlinePlayer.id.GetPersonaName()}";
					}
					else
					{
						if (false && onlinePlayer.isMe && GetSteamID(out ulong steamID))
						{
							string uniqueID = steamID.ToString();
							if (GetSteamName(out string steamName))
							{
								string personaName = steamName;

								//steamMapping[personaName] = uniqueID;
							}
							return $"{uniqueID}";
						}
						else
						{
							string personaName = onlinePlayer.id.GetPersonaName();
							string inLobbyId = onlinePlayer.inLobbyId.ToString();

							//if (steamMapping.TryGetValue(personaName, out string mappedID))
							//{
							//	return $"{mappedID}";
							//}

							return $"{personaName}" ?? $"{inLobbyId}";
						}
					}
					//return onlinePlayer.id is SteamMatchmakingManager.SteamPlayerId steamPlayerID ? steamPlayerID.steamID.m_SteamID.ToString() : inLobbyId.ToString();
				}
			}
		}
		Log.LogWarning($"GetUniqueID: Failed to get UniqueID for {onlinePlayer}");
		return "Null";
	}

	public static bool AllPlayersHaveMod(string modID)
	{
		if (IsModEnabled_RainMeadow)
		{
			if (IsMeadowStoryMode())
			{
				if (OnlineManager.lobby == null) return false;
				if (OnlineManager.players == null || OnlineManager.players.Count == 0) return false;

				foreach (var player in OnlineManager.players)
				{
					if (player.isMe) continue;

					if (!OnlineManager.lobby.clientSettings.TryGetValue(player, out var settings))
						return false;

					if (!settings.TryGetData<CustomClientSettings>(out var customSettings))
						return false;

					if (!customSettings.keys.Contains(modID))
						return false;
				}

				return true;
			}
		}
		return true;
	}

	public static bool IsMe(this Player player)
	{
		if (MeadowCompat.IsModEnabled_RainMeadow)
		{
			if (MeadowCompat.IsMeadowStoryMode())
			{
				var onlinePlayer = GetOnlinePlayer(player);
				if (onlinePlayer != null)
				{
					if (onlinePlayer.isMe)
					{
						return true;
					}
					else
					{
						return false;
					}
				}
				return false;
			}
		}
		return true;
	}

	public static OnlinePlayer? FindOnlinePlayer(string uniqueID)
	{
		if (IsModEnabled_RainMeadow)
		{
			if (IsMeadowStoryMode())
			{
				if (OnlineManager.lobby == null) return null;
				if (OnlineManager.players == null || OnlineManager.players.Count == 0) return null;

				foreach (var player in OnlineManager.players)
				{
					if (GetUniqueID(player) == uniqueID)
					{
						return player;
					}
				}
				return null;
			}
		}
		return null;
	}
	public static Player? FindPlayer(OnlinePlayer? onlinePlayer)
	{
		if (IsModEnabled_RainMeadow)
		{
			if (IsMeadowStoryMode())
			{
				if (onlinePlayer != null)
				{
					//if (RWCustom.Custom.rainWorld?.processManager?.currentMainLoop is RainWorldGame game)
					//{
					//}
					RainWorldGame? game = GlobalVar.game;
					if (game != null)
					{
						foreach (var ac in game.Players)
						{
							if (ac.realizedCreature is Player player)
							{
								OnlinePlayer? onlinePlayer_ = GetOnlinePlayer(player);
								if (onlinePlayer_ == onlinePlayer)
								{
									return player;
								}
							}
						}
					}
				}
			}
		}
		return null;
	}
    public static Player? FindPlayer(string uniqueID)
	{
		return FindPlayer(FindOnlinePlayer(uniqueID));
	}

    // 定义 RPC 方法
    [RPCMethod(security = RPCSecurity.InLobby)]
	public static void SyncStomachCapacity(string playerID, int currentCapacity, int maxCapacity)
	{
		Player? player = FindPlayer(playerID);
		if (player == null) return;

		// 更新该玩家的容量显示/状态
		UpdatePlayerStomachDisplay(player, currentCapacity, maxCapacity);
	}

	// 广播 发送同步
	public static void BroadcastCapacity(Player player)
	{
		if (MeadowCompat.IsModEnabled_RainMeadow)
		{
			if (MeadowCompat.IsMeadowStoryMode())
			{
				var pv = player.GetPlayerVar();

				if (player.abstractCreature.GetOnlineObject(out var opo) && opo != null)
				{
					int historyCount = pv.stomachData.historyInStomach.Count;
					int max = pv.stomachData.capacity;

					// 广播给房间内所有人
					opo.BroadcastRPCInRoom(SyncStomachCapacity,
						GetUniqueID(player),
						historyCount,
						max);
					//opo.BroadcastRPCInRoom(SyncStomachCapacity,
					//	(ushort)OnlineManager.mePlayer.inLobbyId,
					//	historyCount,
					//	max);
				}
			}
		}
	}

	// 在容量变化时触发
	// 吞咽时
	//public void OnSwallow(Player player)
	//{
	//	var pv = player.GetPlayerVar();
	//	//pv.stomachData.historyInStomach.Add(item);

	//	// 广播容量变化
	//	BroadcastCapacity(player);
	//}

	//// 反刍时
	//public void OnRegurgitate(Player player)
	//{
	//	var pv = player.GetPlayerVar();
	//	//pv.stomachData.historyInStomach.RemoveAt(pv.stomachData.historyInStomach.Count - 1);

	//	// 广播容量变化
	//	BroadcastCapacity(player);
	//}

	// 接收端的处理
	public static void UpdatePlayerStomachDisplay(Player player, int current, int max)
	{
		// 更新 UI 显示
		// 例如：在玩家头顶显示容量条
		// 或者：更新 HUD 上的图标

		// 存储到本地缓存，用于其他玩家查看
		//_playerCapacities[onlinePlayer.inLobbyId] = (current, max);
		player.GetPlayerVar(out var pv);
		var stomachData = pv.stomachData;

		stomachData.Capacity = max;

        stomachData.historyInStomach.Clear();
        for (int i = 0; i < current; i++)
        {
			stomachData.historyInStomach.Add(null!);
        }
    }

	// 获取其他玩家的容量信息
	//public static (int current, int max) GetPlayerCapacity(OnlinePlayer player)
	//{
	//	return _playerCapacities.TryGetValue(player.inLobbyId, out var cap)
	//		? cap
	//		: (0, 0);
	//}

	// 容量变化时广播
	//public static void CheckAndBroadcast(Player player)
	//{
	//	var pv = player.GetPlayerVar();
	//	if (pv == null) return;

	//	// 只在容量变化时广播，避免频繁发送
	//	int current = pv.stomachData.historyInStomach.Count;
	//	int max = pv.stomachData.capacity;

	//	var onlinePlayer = player.GetOnlinePlayer();
	//	if (onlinePlayer == null) return;

	//	// 检查是否变化
	//	if (_lastBroadcast.TryGetValue(onlinePlayer.inLobbyId, out var last))
	//	{
	//		if (last.current == current && last.max == max) return;
	//	}

	//	_lastBroadcast[onlinePlayer.inLobbyId] = (current, max);
	//	BroadcastCapacity(player);
	//}




	// copied code from rain meadow's explode hooks
	// 从Rain Meadow的爆炸钩子上复制了代码
	/// <summary>
	/// 处理风弹爆炸的网络同步。
	/// 返回值表示“是否已经处理了网络同步”（若为 true，则本地不再执行爆炸逻辑）。
	/// </summary>
	public static bool ExplodeRPC(PhysicalObject self)
	{
		// 1. 检查是否在 Meadow 大厅中（即处于多人模式）
		if (IsMeadowStoryMode())
		{
			// 2. 获取该物体的网络对象（OnlinePhysicalObject）
			//    如果物体尚未被 Meadow 托管，则无法同步。
			if (!self.abstractPhysicalObject.GetOnlineObject(out var opo))
			{
				RainMeadow.RainMeadow.Error($"Entity {self} doesn't exist in online space!");
				return false;  // 同步失败，交由本地逻辑处理
			}

			// 3. 判断执行权：自己是房间所有者（isOwner）且（拥有该实体 或 当前已在 RPC 事件中）
			if (opo!.roomSession.isOwner && (opo.isMine || RPCEvent.currentRPCEvent is not null))
			{
				// 我是所有者 → 直接广播 RPC 给所有玩家
				// 参数：要调用的方法（opo.Explode），以及方法的参数（爆炸位置）
				opo.BroadcastRPCInRoom(opo.Explode, self.bodyChunks[0].pos);
			}
			else if (RPCEvent.currentRPCEvent is null)  // 当前不是由 RPC 触发的
			{
				if (!opo.isMine)
				{
					// 我不拥有该物体 → 等待拥有者发来的 RPC，本地先不执行
					return true;
				}
				// 我是拥有者但不是房间所有者 → 请求房间所有者代为广播
				opo.roomSession.owner.InvokeOnceRPC(opo.Explode, self.bodyChunks[0].pos);
			}
			// 注意：如果当前已经是 RPC 接收状态（RPCEvent.currentRPCEvent != null），
			// 则不再重复发送，防止死循环。
		}
		return false;  // 未处理网络同步（例如单机模式），本地继续执行爆炸
	}

}
