using Kittehface.Framework20;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionLib
{
	public class PlayerVar
	{
		#region PlayerRef
		private WeakReference<Player?> _playerRef;

		public WeakReference<Player?> PlayerRef => _playerRef;


		// 供外部重新绑定 Player 引用
		public void SetPlayerRef(Player player) { _playerRef = new WeakReference<Player?>(player); }
		#endregion

		[JsonProperty]
		public StomachData stomachData;

		public WorldCoordinate coord;

		public class StomachData
		{
			public PlayerVar Owner;

			public List<AbstractPhysicalObject?> historyInStomach = new();  // 历史栈（不含当前）

			[JsonProperty("capacity")]
			public int capacity = 5;                               // 容量限制

			[JsonProperty("historyInStomach")]
			public List<string> _serializedHistory = new();

			//开始吞咽/反刍/物品制作流程
			//public bool swallowOrRegurgitate = false;
			public bool IsSwallowing = false;

			public int OnlineHistoryCount = 0;

			public StomachData(PlayerVar owner)
			{
				Owner = owner;
			}
			public AbstractPhysicalObject? Current
			{
				get
				{
					Owner._playerRef.TryGetTarget(out Player? player);
					if (player == null)
					{
						Log.LogError("_playerRef.player: null");
						//throw new NullReferenceException("_playerRef.player: null".MessageLog());
						return null;
					}
					return player.objectInStomach;
				}
				set
				{
					Owner._playerRef.TryGetTarget(out Player? player);
					if (player == null)
					{
						Log.LogError("_playerRef.player: null");
						//throw new NullReferenceException("_playerRef.player: null".MessageLog());
						return;
					}
					player.objectInStomach = value;
				}
			}// 指向 player.objectInStomach
			public int HistoryCount => historyInStomach.Count;
            public int TotalCount => (Current != null ? 1 : 0) + HistoryCount;
			public bool IsEmpty => Current == null && HistoryCount == 0;
			public bool IsFull => TotalCount >= capacity;
			public int RemainingSpace => capacity - TotalCount;
			public int Capacity { get => capacity; set => capacity = Math.Max(0, value); }

			// 历史栈操作
			public void Push(AbstractPhysicalObject obj) => historyInStomach.Add(obj);
			public AbstractPhysicalObject? PopHistory()
			{
				if (HistoryCount == 0) return null;
				var top = historyInStomach[HistoryCount - 1];
				historyInStomach.RemoveAt(HistoryCount - 1);
				return top;
			}
			public void ClearHistory() => historyInStomach.Clear();

			// 吞入
			public bool Swallow(AbstractPhysicalObject? obj)
			{
				if (IsFull) return false;
				if (Current == obj && Current != null) return false; // 不能重复吞入同一个对象

				// 当前变历史
				if (Current != null)
					Push(Current);

				// 设置新当前
				Current = obj;
				return true;
			}
			// 吐出/消化（移除当前，从历史补位）
			public AbstractPhysicalObject? PopCurrent()
			{
				var removed = Current;
				Current = null;  // 先清空当前

				// 从历史取一个补位
				if (HistoryCount > 0)
				{
					Current = PopHistory();
				}

				return removed;
			}

			// 清空全部
			public void ClearAll()
			{
				ClearHistory();
				Current = null;
			}

			// 获取完整内容（用于遍历/显示）
			public List<AbstractPhysicalObject?> GetAllContents()
			{
				var result = new List<AbstractPhysicalObject?>(historyInStomach);  // 历史（从底到顶）
				if (Current != null)
					result.Add(Current);    // 当前（栈顶）
				return result;
			}
		}



		// 反序列化的无参构造函数
		public PlayerVar()
		{
			_playerRef = new WeakReference<Player?>(null);

			stomachData = new StomachData(this);
			coord = new WorldCoordinate();
		}
		public PlayerVar(Player player)
		{
			_playerRef = new WeakReference<Player?>(player);

			stomachData = new StomachData(this);
			coord = player.coord;
		}

		#region Save/Load

		[OnSerializing]// 保存时自动调用
		internal void OnSerializing(StreamingContext context)
		{
			Save();
		}
		public void Save()
		{
			_playerRef.TryGetTarget(out Player? player);

			//stomachData._serializedHistory.Clear();

			foreach (var obj in stomachData.historyInStomach)
			{
				string objString = Helper.ObjectToString(obj, player != null ? player.coord : coord, true);
				stomachData._serializedHistory.Add(objString);
			}
			Log.LogDebug($"Save: 保存了 {stomachData.historyInStomach.Count} 个对象");
		}
		public void Malnourished_Save()
		{
			Save();

			stomachData.historyInStomach.Clear();
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




	}
}
