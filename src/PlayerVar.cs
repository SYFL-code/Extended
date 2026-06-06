using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Extended
{
	public class PlayerVar
	{
		#region PlayerRef
		[JsonIgnore]
		private WeakReference<Player?> _playerRef;
		[JsonIgnore]
		public WeakReference<Player?> PlayerRef => _playerRef;
		// 供外部重新绑定 Player 引用
		public void SetPlayerRef(Player player) { _playerRef = new WeakReference<Player?>(player); }
		#endregion

		#region swallowedObjects
		public int StorageCapacity = 1;
		[JsonIgnore]
		public List<AbstractPhysicalObject> objectsInStomach = new();//胃部存储列表
		public List<string> swallowedObjects = new();
		[JsonIgnore]
		public WorldCoordinate coord;

        // 获取胃部容量
        public int GetStomachCapacity(Player player)
        {
            int Capacity = StorageCapacity;

            /*if (MyOptions.Instance?.StomachCapacity != null)
            {
                Capacity = MyOptions.Instance.StomachCapacity.Value;
            }*/
            /*if (ModManager.MSC && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
            {
                Capacity += 2;
            }*/

            return Capacity;
        }

        // 检查是否有空间
        public bool HasSpace(Player player)
        {
            return objectsInStomach.Count < GetStomachCapacity(player);
        }
        #endregion

        #region Save Load
        [OnSerializing]// 保存时自动调用
		internal void OnSerializing(StreamingContext context)
		{
			Save();
		}
		public void Save()
		{
			_playerRef.TryGetTarget(out Player? player);
			swallowedObjects.Clear();

			foreach (var obj in objectsInStomach)
			{
				string objString = Helper.ObjectToString(obj, player != null ? player.coord : coord, true);
				swallowedObjects.Add(objString);
			}
		}
		public void Malnourished_Save()
		{
			Save();

			objectsInStomach.Clear();
		}

		[OnDeserialized]// 加载时自动调用
		internal void OnDeserialized(StreamingContext context)
		{
			Load();
		}
		public void Load()
		{
			/*_playerRef.TryGetTarget(out Player? player);
			objectsInStomach.Clear();

			foreach (string objString in _savedSwallowedObjects)
			{
				AbstractPhysicalObject obj = Helper.StringToObject(objString, player != null ? player.coord : coord);
				if (obj != null)
				{
					objectsInStomach.Add(obj);
				}
			}*/
		}
		#endregion


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
