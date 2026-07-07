using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
