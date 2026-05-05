using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extended
{
    public class MyPlayer
    {

        public static void HookAdd()
        {
            /*On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.SwallowObject += Player_SwallowObject;
            On.Player.Regurgitate += Player_Regurgitate;*/
        }
        public static void HookSubtract()
        {
            /*On.Player.ctor -= Player_ctor;
            On.Player.Update -= Player_Update;
            On.Player.SwallowObject -= Player_SwallowObject;
            On.Player.Regurgitate -= Player_Regurgitate;*/
        }

    }
}
