using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionLib
{
    public class PlayerModule
    {
        WeakReference<Player> playerRef;

        public string key = "";

        public MyDebug? myDebug = null;//调试图像

        public bool StomachExtension = true;

        public PlayerModule(Player player)
        {
            playerRef = new WeakReference<Player>(player);
        }

    }

}
