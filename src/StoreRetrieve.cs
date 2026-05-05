using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static StoredData;

namespace Extended
{
	public static class StoreRetrieve
	{
		public static void StoreItem(Player player, AbstractPhysicalObject apo)
		{
			StoredData storedData = ExtendedData.StoredDatas[player.playerState.playerNumber];

			if (storedData.storedObjects == null)
			{
				return;
			}

			List<int> listIndexes = new();
			for (int i = 0; i < storedData.storedObjects.Count; i++)
			{
				StoredObject? item = storedData.storedObjects[i];
				if (item != null)
				{
					listIndexes.Add(item.index);
				}
			}
			listIndexes.Sort();

			int index = 0;

			if (listIndexes.Count > 0) 
			{
				index = listIndexes[listIndexes.Count - 1] + 1;
			}

			StoreItem(player, apo, index);
		}

		public static void RetrieveItem(Player player)
		{
			StoredData storedData = ExtendedData.StoredDatas[player.playerState.playerNumber];

			if (storedData.storedObjects == null)
			{
				return;
			}

			List<int> listIndexes = new();
			for (int i = 0; i < storedData.storedObjects.Count; i++)
			{
				StoredObject? item = storedData.storedObjects[i];
				if (item != null)
				{
					listIndexes.Add(item.index);
				}
			}
			listIndexes.Sort();

			int index = 0;

			if (listIndexes.Count > 0)
			{
				index = listIndexes[listIndexes.Count - 1];
			}
			else
			{
				return;
			}

			RetrieveItem(player, index);
		}


		public static void StoreItem(Player player, AbstractPhysicalObject apo, int index)
		{
			//AbstractPhysicalObject apo = player.grasps[i].grabbed.abstractPhysicalObject;

			StoredData storedData = ExtendedData.StoredDatas[player.playerState.playerNumber];

			if (apo.type == AbstractPhysicalObject.AbstractObjectType.Creature && apo is AbstractCreature act)
			{
				StoredObject? storedItem = storedData.NewStoredObject(null, act, index);

				if (storedItem == null)
				{
					UDebug.LogException(new Exception("INV: Attempted to store invalid object: " + apo.type.value));
					player.room.PlaySound(SoundID.MENU_Error_Ping);
					return;
				}
				//UDebug.Log("INV: " + ((activeSlot.storedItem.type != null) ? activeSlot.storedItem.type.value : "Null apo type"));
				//UDebug.Log("INV: " + ((activeSlot.storedItem.critType != null) ? (activeSlot.storedItem.critType.value + " | " + activeSlot.storedItem.data) : "Null crit type"));
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
					UDebug.LogException(new Exception("Failed to remove Creature from world when storing object, it may not support being Abstracized or was not created from a Spawner"));
					player.room.PlaySound(SoundID.MENU_Error_Ping);
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
						UDebug.LogException(new Exception("INV: Attempted to store invalid object: " + apo.type.value));
						player.room.PlaySound(SoundID.MENU_Error_Ping);
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

			player.room.PlaySound(SoundID.Slugcat_Stash_Spear_On_Back, player.mainBodyChunk, false, 2f, 0.9f);
		}

		public static void RetrieveItem(Player player, int index)
		{
			/*int freeGrasp = 2;
			for (int i = 0; i < player.grasps.Length; i++)
			{
				if (player.grasps[i] != null)
				{
					freeGrasp--;
				}
			}*/

			if (false)//(freeGrasp == 0)
			{
				player.room.PlaySound(SoundID.MENU_Error_Ping);
			}
			else
			{
				StoredData storedData = ExtendedData.StoredDatas[player.playerState.playerNumber];

				if (storedData.storedObjects == null || storedData.storedObjects.Count == 0)
				{
					player.room.PlaySound(SoundID.MENU_Error_Ping);
					return;
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
					UDebug.LogError("INV: Attempted to retrieve object from empty slot!");
					player.room.PlaySound(SoundID.MENU_Error_Ping);
					return; 
				}

				AbstractPhysicalObject apo;
				if (storedObject.type == AbstractPhysicalObject.AbstractObjectType.Creature)
				{
					string[] array = Regex.Split(storedObject.data, "<cA>");
					UDebug.Log(array[1] ?? "");
					EntityID id = EntityID.FromString(Regex.Split(array[1], "<cB>")[0]);

					apo = new AbstractCreature(player.room.world, StaticWorld.GetCreatureTemplate(storedObject.critType), null, player.coord, id);
					if (apo is AbstractCreature act)
					{
						act.state.LoadFromString(Regex.Split(array[3], "<cB>"));
					}
				}
				else
				{
					UDebug.Log("RECREATE FROM SAVE STRING");
					apo = SaveState.AbstractPhysicalObjectFromString(player.room.world, storedObject.data);

					if (apo is AbstractConsumable aco)
					{
						aco.Consume();
					}
				}
				UDebug.Log("ADD ENTITY");
				player.room.abstractRoom.AddEntity(apo);
				apo.pos = player.abstractCreature.pos;
				UDebug.Log("REALIZE OBJECT");
				apo.RealizeInRoom();

				if (apo.realizedObject != null)
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
									UDebug.Log("SLUGCAT GRAB");
									player.SlugcatGrab(apo.realizedObject, j);
								}
							}
						}
					}
				}
				player.room.PlaySound(SoundID.Slugcat_Stash_Spear_On_Back);
				storedData.RemoveStoredObject(storedObject.index);
				//storedData.storedObjects[index] = null;
			}
		}


	}
}
