using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static StoredData;
using static ExtendedData;

namespace Extended
{
	public static class StoreRetrieve
	{
		public static void StoreItem(Player player, AbstractPhysicalObject apo)
		{
			StoredData storedData = ExtendedData.GetStoredData(player);
			player.GetPlayerVar(out var pv);

			if (storedData.storedObjects == null)
			{
				UDebug.Log("StoreRetrieve_StoreItem: storedObjects");
				storedData.storedObjects = new List<StoredObject?>();
			}

			List<int> itemsIndexes = storedData.GetStoredIndexes();
			int index = 0;
			while (itemsIndexes.Contains(index) && index < 1000)
			{
				index++;
			}

			if (itemsIndexes.Count >= pv.StorageCapacity)
			{
				UDebug.LogError("StoreRetrieve_StoreItem: StorageCapacity full!");
				//player.room.PlaySound(SoundID.MENU_Error_Ping);
				return;
			}

			StoreItem(player, apo, index);
		}

		public static AbstractPhysicalObject? RetrieveItem(Player player)
		{
			StoredData storedData = ExtendedData.GetStoredData(player);

			if (storedData.storedObjects == null)
			{
				return null;
			}

			List<int> itemsIndexes = storedData.GetStoredIndexes();

			int index = 0;

			if (itemsIndexes.Count > 0)
			{
				index = itemsIndexes[itemsIndexes.Count - 1];
			}
			else
			{
				return null;
			}

			return RetrieveItem(player, index);
		}


		public static void StoreItem(Player player, AbstractPhysicalObject apo, int index)
		{
			StoredData storedData = ExtendedData.GetStoredData(player);

			if (apo.type == AbstractPhysicalObject.AbstractObjectType.Creature && apo is AbstractCreature act)
			{
				StoredObject? storedItem = storedData.NewStoredObject(null, act, index);

				if (storedItem == null)
				{
					UDebug.LogException(new Exception("StoreRetrieve_StoreItem: Attempted to store invalid object: " + apo.type.value));
					//player.room.PlaySound(SoundID.MENU_Error_Ping);
					return;
				}
				try
				{
					act.realizedCreature.RemoveFromRoom();
					apo.Room.RemoveEntity(act);
					act.realizedCreature.Destroy();

					if (player.room.game.session is StoryGameSession storyGame)
					{
						storyGame.saveState.waitRespawnCreatures.Add(act.ID.spawner);
					}
				}
				catch
				{
					storedData.RemoveStoredObject(index);
					UDebug.LogException(new Exception("StoreRetrieve_StoreItem: Failed to remove Creature from world when storing object, it may not support being Abstracized or was not created from a Spawner"));
					//player.room.PlaySound(SoundID.MENU_Error_Ping);
					return;
				}
			}
			else
			{
				try
				{
					StoredObject? storedItem = storedData.NewStoredObject(apo, null, index);

					if (storedItem == null)
					{
						UDebug.LogException(new Exception("StoreRetrieve_StoreItem: Attempted to store invalid object: " + apo.type.value));
						//player.room.PlaySound(SoundID.MENU_Error_Ping);
						return;
					}
					apo.Room.entities.Remove(apo);
					apo.destroyOnAbstraction = true;
					apo.Abstractize(apo.pos);
				}
				catch
				{
					storedData.RemoveStoredObject(index);
					return;
				}
			}
			//player.grasps[i].Release();
			//activeSlot.itemSprite.SetElementByName(activeSlot.storedItem.spriteName);
			//activeSlot.itemSprite.color = activeSlot.storedItem.spriteColor;
			//break;

			//player.room.PlaySound(SoundID.Slugcat_Stash_Spear_On_Back, player.mainBodyChunk, false, 2f, 0.9f);
		}

		public static AbstractPhysicalObject? RetrieveItem(Player player, int index)
		{
			StoredData storedData = ExtendedData.GetStoredData(player);

			if (storedData.storedObjects == null || storedData.storedObjects.Count == 0)
			{
				//player.room.PlaySound(SoundID.MENU_Error_Ping);
				return null;
			}

			//StoredObject storedObject = storedData.storedObjects[index]!;
			StoredObject? storedObject = null;
			foreach (var obj in storedData.storedObjects)
			{
				if (obj != null && obj.index == index)
				{
					storedObject = obj;
					break;
				}
			}
			if (storedObject == null)
			{
				/* 错误提示 */
				UDebug.LogError("StoreRetrieve_RetrieveItem: Attempted to retrieve object from empty slot!");
				//player.room.PlaySound(SoundID.MENU_Error_Ping);
				return null;
			}

			AbstractPhysicalObject apo;
			if (storedObject.type == AbstractPhysicalObject.AbstractObjectType.Creature)
			{
				string[] array = Regex.Split(storedObject.data, "<cA>");
                if (array.Length < 4)
                {
                    UDebug.LogError("StoreRetrieve_RetrieveItem: Invalid creature data format");
                    return null;
                }

                UDebug.Log(array[1] ?? "");
				EntityID id = EntityID.FromString(Regex.Split(array[1], "<cB>")[0]);

				apo = new AbstractCreature(player.room.world, StaticWorld.GetCreatureTemplate(storedObject.critType), null, player.coord, id);
				if (apo is AbstractCreature act)
				{
					act.state.LoadFromString(Regex.Split(array[3], "<cB>"));
					act.abstractAI?.NewWorld(player.room.world);
				}
			}
			else
			{
				apo = SaveState.AbstractPhysicalObjectFromString(player.room.world, storedObject.data);

				if (apo is AbstractConsumable aco)
				{
					aco.Consume();
				}
			}
			player.room.abstractRoom.AddEntity(apo);
			apo.pos = player.abstractCreature.pos;
			apo.RealizeInRoom();

			/*if (apo.realizedObject != null)
			{
				apo.realizedObject.firstChunk.HardSetPosition(player.mainBodyChunk.pos);
				for (int j = 0; j < 2; j++)
				{
					if (player.grasps[j] == null)
					{
						if (player.CanIPickThisUp(apo.realizedObject))
						{
							bool hasSpear = false;
							for (int s = 0; s < 2; s++)
							{
								if (player.grasps[s] != null && player.grasps[s].grabbed != null)
								{
									if (player.grasps[s].grabbed is Spear)
									{
										UDebug.Log("HAS SPEAR");
										hasSpear = true;
									}
								}
							}
							if (hasSpear && apo.type == AbstractPhysicalObject.AbstractObjectType.Spear && player.spearOnBack != null && !player.spearOnBack.HasASpear)
							{
								player.spearOnBack.SpearToBack(apo.realizedObject as Spear);
							}
							else
							{
								player.SlugcatGrab(apo.realizedObject, j);
							}
						}
					}
				}
			}*/
			//player.room.PlaySound(SoundID.Slugcat_Stash_Spear_On_Back);
			storedData.RemoveStoredObject(storedObject.index);
			//storedData.storedObjects[index] = null;

			return apo;
		}


	}
}
