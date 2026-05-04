using Extended;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;


public static class ExtendedData
{
	public static Dictionary<int, StoredData> StoredDatas = new();

	public static string SaveString()
	{
		var parts = new List<string>();
		foreach (var item in StoredDatas)
		{
			parts.Add($"{item.Key}<Dict>{item.Value.SaveString()}");
		}

		return string.Join("<P>", parts);
	}

	public static void LoadString(string data)
	{
		if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(data.Trim()))
		{
			StoredDatas = new();
			return;
		}

		string[] datas = Regex.Split(data, "<P>");

		StoredDatas = new();
		foreach (var item in datas)
		{
			if (string.IsNullOrEmpty(item) || string.IsNullOrEmpty(item.Trim())) 
			{
				continue;
			}

			string[] items = Regex.Split(item, "<Dict>");

			if(items.Length == 2 && int.TryParse(items[0], out int key))
			{
				StoredData value = new StoredData();
				value.LoadString(items[1]);

				StoredDatas[key] = value;
			}
		}

	}
}

/// <summary>
/// 扩展物品栏的静态数据管理类。负责存储、删除、序列化/反序列化储物对象。
/// </summary>
public class StoredData
{
	/// <summary>
	/// 创建一个新的储物对象，并将其加入存储列表。
	/// </summary>
	/// <param name="apo">要存储的抽象物理对象（物品），如果是生物则为 null。</param>
	/// <param name="crit">要存储的抽象生物对象，如果是物品则为 null。</param>
	/// <param name="index">在物品栏中的槽位索引。</param>
	/// <returns>创建成功的 StoredObject 实例，若失败则返回 null。</returns>
	public StoredData.StoredObject? NewStoredObject(AbstractPhysicalObject apo, AbstractCreature crit, int index)
	{
		// 确保存储列表已初始化
		if (this.storedObjects == null)
		{
			this.storedObjects = new List<StoredData.StoredObject?>();
			UDebug.Log("New StoredObject list created");
		}

		UDebug.Log("Current list length: " + this.storedObjects.Count.ToString());

		// 检查是否已有相同槽位的对象，避免重复创建
		for (int i = 0; i < this.storedObjects.Count; i++)
		{
			if (this.storedObjects[i] != null && this.storedObjects[i]!.index == index)
			{
				UDebug.Log("An object already exists with index " + index.ToString());
				return this.storedObjects[i]; // 直接返回已存在的对象
			}
		}

		StoredData.StoredObject? newObject = null;
		try
		{
			// 尝试创建新的存储对象（内部会根据 apo 或 crit 构造数据）
			newObject = new StoredData.StoredObject(apo, crit, index);
		}
		catch
		{
			UDebug.LogException(new Exception("Failed to created StoredObject"));
		}

		if (newObject != null)
		{
			// 加入列表
			this.storedObjects.Add(newObject);

			// 二次确认（防止极端并发情况，实际单线程游戏很少发生）
			for (int j = 0; j < this.storedObjects.Count; j++)
			{
				if (this.storedObjects[j] != null && this.storedObjects[j]!.index == index)
				{
					UDebug.Log("New stored object created with index " + index.ToString());
					return this.storedObjects[j];
				}
			}
			UDebug.Log("Error creating or obtaining stored object with index " + index.ToString());
			return null;
		}

		UDebug.Log("Error creating or obtaining stored object with index " + index.ToString());
		return null;
	}

	/// <summary>
	/// 从存储列表中移除指定槽位的对象。
	/// </summary>
	/// <param name="index">要移除的槽位索引。</param>
	public void RemoveStoredObject(int index)
	{
		if (this.storedObjects == null)
		{
			UDebug.Log("No stored objects to remove");
			return;
		}

		int listIndex = -1;
		// 查找列表中对应的对象，将其置 null（标记删除）
		for (int i = 0; i < this.storedObjects.Count; i++)
		{
			if (this.storedObjects[i] != null && this.storedObjects[i]!.index == index)
			{
				listIndex = i;
				this.storedObjects[i] = null;
			}
		}

		// 移除 list 中的 null 项（此处仅移除第一个找到的索引）
		if (listIndex != -1)
		{
			this.storedObjects.RemoveAt(listIndex);
		}
		UDebug.Log("Removed object from inventory at index " + index.ToString());
	}

	/// <summary>
	/// 将当前所有存储对象序列化为存档字符串。
	/// 格式：{inventoryType}<V>{invSize.x}<V>{invSize.y}<V><SObj>物品1<SObj>物品2...
	/// </summary>
	/// <returns>序列化后的存档字符串。</returns>
	public string SaveString()
	{
		string objectData = "";

		// 遍历每个存储对象，拼接其序列化数据
		if (this.storedObjects != null)
		{
			for (int i = 0; i < this.storedObjects.Count; i++)
			{
				if (this.storedObjects[i] != null)
				{
					objectData += "<SObj>";
					objectData += this.storedObjects[i]!.ToDataString();
				}
			}
		}

		int invType = (int)StoredData.inventoryType;
		// 组合最终的存档字符串
		return string.Concat(new string[]
		{
			invType.ToString(),
			"<V>",
			StoredData.invSize.x.ToString(),
			"<V>",
			StoredData.invSize.y.ToString(),
			"<V>",
			objectData
		});
	}

	/// <summary>
	/// 从存档字符串中加载物品栏数据。
	/// </summary>
	/// <param name="data">存档字符串。</param>
	public void LoadString(string data)
	{
		// 按 <V> 切割：物品栏类型、宽度、高度、对象列表
		string[] invData = Regex.Split(data, "<V>");
		StoredData.inventoryType = (StoredData.InventoryType)int.Parse(invData[0]);
		StoredData.invSize.x = int.Parse(invData[1]);
		StoredData.invSize.y = int.Parse(invData[2]);

		// 按 <SObj> 切割出每个存储对象的数据段
		string[] objectData = Regex.Split(invData[3], "<SObj>");

		// 重新初始化存储列表
		this.storedObjects = new List<StoredData.StoredObject?>();

		// 从索引 1 开始（第一个元素通常为空），逐个解析对象
		for (int i = 1; i < objectData.Length; i++)
		{
			this.storedObjects.Add(this.GenerateStoredObjectFromString(objectData[i]));
		}
	}

	/// <summary>
	/// 从单独的对象数据字符串反序列化出一个 StoredObject。
	/// 格式：{type.value}<X>{critType.value}<X>{index}<X>{spriteName}<X>{r}<X>{g}<X>{b}<X>{data}
	/// </summary>
	/// <param name="data">单个对象的序列化字符串。</param>
	/// <returns>重建的 StoredObject 实例。</returns>
	public StoredData.StoredObject GenerateStoredObjectFromString(string data)
	{
		// 创建一个空的模板对象（参数 null, null, -1 表示内部字段将全部从字符串读取）
		StoredData.StoredObject SObj = new StoredData.StoredObject(null, null, -1);
		string[] objData = Regex.Split(data, "<X>");

		// 输出每个字段用于调试
		for (int i = 0; i < objData.Length; i++)
		{
			UDebug.Log(objData[i]);
		}

		// 按顺序解析各字段
		SObj.type = new AbstractPhysicalObject.AbstractObjectType(objData[0], false);
		UDebug.Log(SObj.type.ToString());
		SObj.critType = new CreatureTemplate.Type(objData[1], false);
		SObj.index = int.Parse(objData[2]);
		SObj.spriteName = objData[3];
		SObj.spriteColor.r = float.Parse(objData[4]);
		SObj.spriteColor.g = float.Parse(objData[5]);
		SObj.spriteColor.b = float.Parse(objData[6]);
		SObj.spriteColor.a = 1f; // 透明度固定为 1（不透明）
		SObj.data = objData[7];
		return SObj;
	}

	/// <summary>
	/// 根据槽位索引获取物品的图标精灵名称。
	/// </summary>
	/// <param name="index">槽位索引。</param>
	/// <returns>精灵名称，若未找到则返回默认的 "Futile_White"。</returns>
	public string ObjectSprite(int index)
	{
		if (this.storedObjects == null)
		{
			return "Futile_White";
		}

		for (int i = 0; i < this.storedObjects.Count; i++)
		{
			if (this.storedObjects[i] != null &&
				this.storedObjects[i]!.spriteName != null &&
				this.storedObjects[i]!.index == index)
			{
				return this.storedObjects[i]!.spriteName!;
			}
		}
		return "Futile_White";
	}

	/// <summary>
	/// 根据槽位索引获取物品的图标颜色。
	/// </summary>
	/// <param name="index">槽位索引。</param>
	/// <returns>颜色值，若未找到则返回默认的绿色 (0,1,0)。</returns>
	public Color ObjectColor(int index)
	{
		if (this.storedObjects == null)
		{
			return new Color(0f, 1f, 0f);
		}

		for (int i = 0; i < this.storedObjects.Count; i++)
		{
			if (this.storedObjects[i] != null && this.storedObjects[i]!.index == index)
			{
				return this.storedObjects[i]!.spriteColor;
			}
		}
		return new Color(0f, 1f, 0f);
	}

	// --- 静态字段：物品栏的默认尺寸、类型、槽位数 ---
	public static IntVector2 invSize = new IntVector2(3, 2);
	//public static int invSlots = 7;
	public static StoredData.InventoryType inventoryType = StoredData.InventoryType.Grid;

	/// <summary>
	/// 存储对象的列表。使用可空类型以支持空位（某些历史版本可能有 null 元素）。
	/// </summary>
	public List<StoredData.StoredObject?>? storedObjects;

	// 保留字段，可能为调试或未来扩展预留
	//public static string test = "";

	/// <summary>
	/// 物品栏显示类型枚举。
	/// </summary>
	public enum InventoryType
	{
		Grid,  // 网格排列
		Cycle  // 轮盘/循环选择
	}

	/// <summary>
	/// 代表一个存储的物品或生物，包含其类型、数据、图标等信息。
	/// </summary>
	public class StoredObject
	{
		/// <summary>
		/// 构造函数：根据传入的抽象物理对象或抽象生物创建存储条目。
		/// </summary>
		/// <param name="apo">物品的抽象对象（可为 null）。</param>
		/// <param name="crit">生物的抽象对象（可为 null）。</param>
		/// <param name="index">槽位索引。</param>
		public StoredObject(AbstractPhysicalObject? apo, AbstractCreature? crit, int index)
		{
			this.index = index;

			// 处理物品存储
			if (apo != null)
			{
				this.type = apo.type;
				string str = "SAVING OBJECT: ";
				AbstractPhysicalObject.AbstractObjectType abstractObjectType = apo.type;
				UDebug.Log(str + ((abstractObjectType != null) ? abstractObjectType.ToString() : null));

				// 保存物品的序列化数据（ToString() 返回的描述字符串）
				this.data = apo.ToString();

				// 尝试获取物品的图标符号信息
				try
				{
					IconSymbol.IconSymbolData symbol = ItemSymbol.SymbolDataFromItem(apo)!.Value;
					this.spriteName = ItemSymbol.SpriteNameForItem(this.type, symbol.intData);
					this.spriteColor = ItemSymbol.ColorForItem(this.type, symbol.intData);
				}
				catch
				{
					// 获取失败时使用默认白色图标和紫色底色，并对业力花做特殊处理
					this.spriteName = "Futile_White";
					this.spriteColor = new Color(1f, 0f, 1f);
					if (this.type == AbstractPhysicalObject.AbstractObjectType.KarmaFlower)
					{
						this.spriteName = "FlowerMarker";
						this.spriteColor = RainWorld.GoldRGB;
					}
				}

				// 如果是消耗品，将其标记为已消耗（避免重复利用）
				if (apo is AbstractConsumable absConsumable)
				{
					if (apo is DataPearl.AbstractDataPearl dataPearl)
					{
						dataPearl.Consume();
					}
					else
					{
						absConsumable.Consume();
					}
				}
			}

			// 处理生物存储
			if (crit != null)
			{
				this.type = crit.type;
				this.critType = crit.creatureTemplate.type;
				// 将生物状态序列化为 StoryWorld 格式的字符串
				this.data = SaveState.AbstractCreatureToStringStoryWorld(crit);
				UDebug.Log("Stored Creature: " + this.data);
				UDebug.Log("critType: " + this.critType.value);

				try
				{
					IconSymbol.IconSymbolData creature = CreatureSymbol.SymbolDataFromCreature(crit);
					this.spriteName = CreatureSymbol.SpriteNameOfCreature(creature);
					this.spriteColor = CreatureSymbol.ColorOfCreature(creature);

					// 如果是蛞蝓猫 NPC（非玩家），使用其 ID 作为种子生成随机毛色
					if (this.critType == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC)
					{
						int ID;
						int.TryParse(Regex.Split(this.data, "<cA>")[1].Split(new char[] { '.' })[2], out ID);
						UDebug.Log("ID of SlugNPC: " + ID.ToString());

						// 使用 ID 作为随机种子生成颜色，保证同一 NPC 颜色一致
						UnityEngine.Random.State state = UnityEngine.Random.state;
						UnityEngine.Random.InitState(ID);
						float Met = Mathf.Pow(UnityEngine.Random.Range(0f, 1f), 1.5f);
						float Stealth = Mathf.Pow(UnityEngine.Random.Range(0f, 1f), 1.5f);
						float H = Mathf.Lerp(UnityEngine.Random.Range(0.15f, 0.58f), UnityEngine.Random.value, Mathf.Pow(UnityEngine.Random.value, 1.5f - Met));
						float S = Mathf.Pow(UnityEngine.Random.Range(0f, 1f), 0.3f + Stealth * 0.3f);
						float L = Mathf.Pow(UnityEngine.Random.Range((UnityEngine.Random.Range(0f, 1f) <= 0.3f + Stealth * 0.2f) ? 0.9f : 0.75f, 1f), 1.5f - Stealth);
						this.spriteColor = Custom.HSL2RGB(H, S, L);
					}
				}
				catch
				{
					UDebug.Log("StoreObject creation failed getting SymbolData");
					this.spriteName = "Futile_White";
					this.spriteColor = new Color(1f, 0f, 1f);
				}
			}
		}

		/// <summary>
		/// 将存储对象序列化为字符串，用于存档。
		/// 格式：{type.value}<X>{critType.value}<X>{index}<X>{spriteName}<X>{r}<X>{g}<X>{b}<X>{data}
		/// </summary>
		/// <returns>序列化字符串。</returns>
		public string ToDataString()
		{
			return string.Concat(new string[]
			{
				(this.type != null) ? this.type.value : "NULL",
				"<X>",
				(this.critType != null) ? this.critType.value : "NULL",
				"<X>",
				this.index.ToString(),
				"<X>",
				(this.spriteName != null) ? this.spriteName : "Futile_White",
				"<X>",
				this.spriteColor.r.ToString(),
				"<X>",
				this.spriteColor.g.ToString(),
				"<X>",
				this.spriteColor.b.ToString(),
				"<X>",
				(this.data != null) ? this.data : "NULL"
			});
		}

		// 物品的抽象类型（如矛、石头等），生物时也可能被赋值
		public AbstractPhysicalObject.AbstractObjectType? type;

		// 生物的模板类型（如蜥蜴、蛞蝓猫等），物品时为 null
		public CreatureTemplate.Type? critType;

		// 对象的核心状态数据（物品的文本表示或生物的序列化状态）
		public string? data;

		// 重量（当前未使用，可能为未来功能预留）
		public int weight;

		// 在物品栏中的槽位索引
		public int index;

		// 界面显示的图标精灵名称
		public string? spriteName;

		// 界面显示的图标颜色
		public Color spriteColor;
	}
}