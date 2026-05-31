using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extended
{
    public class MyPlayer
    {

        public static void Hook()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
        }
        public static void Hook_()
        {
            On.Player.ctor -= Player_ctor;
            On.Player.Update -= Player_Update;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player player, AbstractCreature abs, World world)
        {
            orig(player, abs, world);
        }

        private static void Player_Update(On.Player.orig_Update orig, Player player, bool eu)
        {
            orig(player, eu);
        }



    }
}
