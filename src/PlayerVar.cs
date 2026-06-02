using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extended
{
    public class PlayerVar
	{
        [JsonIgnore]
        private WeakReference<Player?> _playerRef;
        // 供外部重新绑定 Player 引用
        public void SetPlayerRef(Player player)
        {
            _playerRef = new WeakReference<Player?>(player);
        }
        [JsonIgnore]
        public WeakReference<Player?> PlayerRef => _playerRef;


        //public DateTime CreatedTime { get; set; } = DateTime.Now;


        public int StorageCapacity = 1;
        public List<string> Items = new List<string>();

        // 反序列化的无参构造函数
        public PlayerVar()
        {
            _playerRef = new WeakReference<Player?>(null);
        }

        public PlayerVar(Player player)
		{
			_playerRef = new WeakReference<Player?>(player);


		}


	}
}
