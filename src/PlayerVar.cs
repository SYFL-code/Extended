using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extended
{
	public class PlayerVar
	{
		WeakReference<Player> playerRef;
		public int StorageCapacity = 1;


		public PlayerVar(Player player)
		{
			playerRef = new WeakReference<Player>(player);


		}


	}
}
