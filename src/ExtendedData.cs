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

	#region GetStoredData
	public static StoredData GetStoredData(int playerNumber)
	{
		if (StoredDatas.TryGetValue(playerNumber, out StoredData value))
		{
			return value;
		}
		else
		{
			value = new StoredData();
			StoredDatas[playerNumber] = value;
			return value;
		}
	}
	public static StoredData GetStoredData(this Player player)
	{
		return GetStoredData(player.playerState.playerNumber);
	}
	#endregion


	public static string SaveString()
	{
		if (ExtendedData.StoredDatas.Count <= 0)
		{
			return "";
		}

		var parts = new List<string>();
		foreach (var item in StoredDatas)
		{
			parts.Add($"{item.Key}<D>{item.Value.SaveString()}");
		}

		return string.Join("<P>", parts);
	}

	public static void LoadString(string data)
	{
		if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(data.Trim()))
		{
            StoredDatas.Clear();
            return;
		}

		string[] datas = Regex.Split(data, "<P>");

        StoredDatas.Clear();
        foreach (var item in datas)
		{
			if (string.IsNullOrEmpty(item) || string.IsNullOrEmpty(item.Trim())) 
			{
				continue;
			}

			string[] items = Regex.Split(item, "<D>");

			if(items.Length == 2 && int.TryParse(items[0], out int key))
			{
				StoredData value = new StoredData();
				value.LoadString(items[1]);

				StoredDatas[key] = value;
			}
		}

	}


	public static List<int> GetStoredIndexes(this StoredData data)
	{
		List<int> Index = new();
		if (data.storedObjects == null) return Index;

		foreach (var item in data.storedObjects)
		{
			if (item != null)
			{ Index.Add(item.index); }
		}
		Index.Sort();
		return Index;
	}

}

public class StoredData
{
	/// <summary>
	/// 创建一个新的储物对象
	/// </summary>
	/// <returns>创建成功的 StoredObject 实例，若失败则返回 null。</returns>
	public StoredData.StoredObject? NewStoredObject(AbstractPhysicalObject? apo, AbstractCreature? crit, int index)
	{
		// 确保存储列表已初始化
		if (this.storedObjects == null)
		{
			this.storedObjects = new List<StoredData.StoredObject?>();
			UDebug.Log("StoredData: New StoredObject list created");
		}

		UDebug.Log("StoredData: Current list length: " + this.storedObjects.Count.ToString());

		// 检查是否已有相同槽位的对象，避免重复创建
		for (int i = 0; i < this.storedObjects.Count; i++)
		{
			if (this.storedObjects[i] != null && this.storedObjects[i]!.index == index)
			{
				UDebug.Log("StoredData: An object already exists with index " + index.ToString());
				return this.storedObjects[i]; // 直接返回已存在的对象
			}
		}

		StoredData.StoredObject? newObject = null;
		try
		{
			newObject = new StoredData.StoredObject(apo, crit, index);
		}
		catch
		{
			UDebug.LogException(new Exception("StoredData: Failed to created StoredObject"));
		}

		if (newObject != null)
		{
			this.storedObjects.Add(newObject);

			// 二次确认（防止极端并发情况，实际单线程游戏很少发生）
			for (int j = 0; j < this.storedObjects.Count; j++)
			{
				if (this.storedObjects[j] != null && this.storedObjects[j]!.index == index)
				{
					UDebug.Log("StoredData: New stored object created with index " + index.ToString());
					return this.storedObjects[j];
				}
			}
			UDebug.Log("StoredData: Error creating or obtaining stored object with index " + index.ToString());
			return null;
		}

		UDebug.Log("StoredData: Error creating or obtaining stored object with index " + index.ToString());
		return null;
	}

	/// <summary>
	/// 从存储列表中移除指定槽位的对象。
	/// </summary>
	public void RemoveStoredObject(int index)
	{
		if (this.storedObjects == null)
		{
			UDebug.Log("StoredData: No stored objects to remove");
			return;
		}

		List<int> listIndexes = new();

		for (int i = this.storedObjects.Count - 1; i >= 0; i--)
		{
			if (this.storedObjects[i] != null && this.storedObjects[i]!.index == index)
			{
				listIndexes.Add(i);
				this.storedObjects[i] = null;
				this.storedObjects.RemoveAt(i);
			}
		}

		if (listIndexes.Count > 0)
        {
            listIndexes.Sort();
            UDebug.Log("StoredData: Removed object from inventory at index " + index.ToString() + ", listIndexes: " + listIndexes.ToString());
        }
		else
		{
			UDebug.Log("StoredData: No stored object found with index " + index.ToString());
        }
	}

	public string SaveString()
	{
		string objectData = "";

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

		// 组合最终的存档字符串
		return string.Concat(new string[]
		{
			"v1<V>",
			//slots.ToString(),
			//"<V>",
			//StoredData.invSize.x.ToString(),
			//"<V>",
			//StoredData.invSize.y.ToString(),
			//"<V>",
			objectData
		});
	}
	public void LoadString(string data)
	{
		string[] invData = Regex.Split(data, "<V>");

        UDebug.Log("StoredData: data version: " + invData[0]);
        if (invData[0] == "v1")
		{
            //slots = int.Parse(invData[1]);
            //StoredData.invSize.x = int.Parse(invData[1]);
            //StoredData.invSize.y = int.Parse(invData[2]);

            string[] objectData = Regex.Split(invData[1], "<SObj>");

            this.storedObjects = new List<StoredData.StoredObject?>();

            for (int i = 1; i < objectData.Length; i++)
            {
                this.storedObjects.Add(this.StoredObjectFromString(objectData[i]));
            }
        }
        else
        {
            UDebug.Log("StoredData: Error Unrecognized data version: " + invData[0]);
        }
    }

    /// <summary>
    /// 反序列化StoredObject
    /// </summary>
    public StoredData.StoredObject? StoredObjectFromString(string data)
	{
		string[] objData = Regex.Split(data, "<X>");

		if (objData.Length != 8)
		{
			return null;
		}

		// 输出每个字段用于调试
		for (int i = 0; i < objData.Length; i++)
		{
			UDebug.Log(objData[i]);
		}

		// 创建一个空的模板对象（参数 null, null, -1 表示内部字段将全部从字符串读取）
		StoredData.StoredObject SObj = new StoredData.StoredObject(null, null, -1);

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
	/// 获取物品的图标精灵名称
	/// </summary>
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
	/// 获取物品的图标颜色
	/// </summary>
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
	//public static IntVector2 invSize = new IntVector2(3, 2);
	//public int slots = 5;
	//public static StoredData.InventoryType inventoryType = StoredData.InventoryType.Grid;

	/// <summary>
	/// 存储对象的列表。
	/// </summary>
	public List<StoredData.StoredObject?>? storedObjects;

	/// <summary>
	/// 物品栏显示类型枚举。
	/// </summary>
	/*public enum InventoryType
	{
		Grid,  // 网格排列
		Cycle  // 轮盘/循环选择
	}*/


	public class StoredObject
	{
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
						UnityEngine.Random.state = state;
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
        /// 序列化StoredObject
        /// </summary>
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