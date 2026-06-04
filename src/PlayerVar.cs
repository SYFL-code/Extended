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
		public void SetPlayerRef(Player player) { _playerRef = new WeakReference<Player?>(player); }
		[JsonIgnore]
		public WeakReference<Player?> PlayerRef => _playerRef;


		public int StorageCapacity = 1;

		[JsonIgnore]
		public List<AbstractPhysicalObject> objectsInStomach = new();//胃部存储列表
		public List<string> swallowedObjects
		{
			get
			{
				_playerRef.TryGetTarget(out Player? player);

				List<string> strings = new();

				foreach (var Object in objectsInStomach)
				{
					strings.Add(Helper.ObjectToString(Object, player != null ? player.coord :coord, true));
				}
				return strings;

            }
			set  // 暂存，延迟绑定
			{
                swallowedObjectsTemp = value;
            }
		}
        [JsonIgnore]
		public List<string> swallowedObjectsTemp = new();
        [JsonIgnore]
		public WorldCoordinate coord;



		// 反序列化的无参构造函数
		public PlayerVar()
		{
			_playerRef = new WeakReference<Player?>(null);

			swallowedObjectsTemp.Add("ID.-1.5964<oB>0<oA>Rock<oA>SU_S01.22.17.0");

        }
		public PlayerVar(Player player)
		{
			_playerRef = new WeakReference<Player?>(player);


		}


	}
}
