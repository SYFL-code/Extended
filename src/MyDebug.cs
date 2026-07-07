using RWCustom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ExtensionLib
{
    public class MyDebug
    {
        FContainer container;// 容纳标签的容器
        private FLabel Label_Text;// 显示的文本标签
        private WeakReference<Player> _playerRef;// 要监视的玩家对象
        public static string outStr = "null";// 静态输出字符串
        public string outStr_ = "null";// 实例输出字符串

        public MyDebug(Player player)
        {
            Label_Text = new FLabel(Custom.GetDisplayFont(), "");
            container = new FContainer();
            //this.player = player;
            _playerRef = new WeakReference<Player>(player);
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
#if MYDEBUG
			try
			{
#endif
            //取玩家变量
            container.RemoveAllChildren();
            container.AddChild(Label_Text);
            var hud = rCam.ReturnFContainer("HUD");
            hud.AddChild(container);
#if MYDEBUG
			}
			catch (Exception e)
			{
				StackTrace st = new StackTrace(new StackFrame(true));
				StackFrame sf = st.GetFrame(0);
				var sr = sf.GetFileName().Split('\\');
				MyDebug.outStr = sr[sr.Length - 1] + "\n";
				MyDebug.outStr += sf.GetMethod() + "\n";
				MyDebug.outStr += e;
				UDebug.Log(e);
			}
#endif
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
#if MYDEBUG
			try
			{
#endif
            if (_playerRef.TryGetTarget(out Player player))
            {
                //取玩家变量
                GlobalVar.GetPlayerVar(player, out var pv);

                var pos = Vector2.Lerp(player.bodyChunks[1].lastPos, player.bodyChunks[1].pos, timeStacker);
                pos.y += 80;
                pos.x -= rCam.pos.x;
                pos.y -= rCam.pos.y;
                Label_Text.SetPosition(pos);
                Label_Text.text = outStr;//string.Format("PressedIceShield={0}", GlobalVar.IsPressedIceShield(player));
                Label_Text.text += outStr_;
            }

#if MYDEBUG
			}
			catch (Exception e)
			{
				StackTrace st = new StackTrace(new StackFrame(true));
				StackFrame sf = st.GetFrame(0);
				var sr = sf.GetFileName().Split('\\');
				MyDebug.outStr = sr[sr.Length - 1] + "\n";
				MyDebug.outStr += sf.GetMethod() + "\n";
				MyDebug.outStr += e;
				UDebug.Log(e);
			}
#endif
        }
    }
}

