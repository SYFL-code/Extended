using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using MoreSlugcats;
using RainMeadow;
using Steamworks;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
using System.Text.RegularExpressions;

namespace ExtensionLib
{
	public class Helper
	{

		// 将物品转为字符串
		public static string ObjectToString(AbstractPhysicalObject? Object, WorldCoordinate coord, bool setCoord)
		{
			try
			{
				if (Object == null) 
				{
					return "null";
				}

				if (Object is AbstractCreature ac)
				{
					if (ac.world.GetAbstractRoom(ac.pos.room) == null)
					{
						ac.pos = coord;
					}
					if (setCoord)
					{
						ac.pos = coord;
					}
					return SaveState.AbstractCreatureToStringStoryWorld(ac, coord);
				}
			}
			catch (Exception ex)
			{
				Log.LogWarning($"将物品转为字符串失败: {ex.Message}");
			}

            if (Object == null)
            {
                return "null";
            }
            return Object.ToString();
		}
		// 从字符串解析物品
		public static AbstractPhysicalObject? ObjectFromString(string itemStr, World world, WorldCoordinate? coord, WorldCoordinate? pos)
		{
			try
			{
				if (string.IsNullOrEmpty(itemStr))
					return null;

				if (itemStr == "null" || itemStr == "Null")
                    return null;

                AbstractPhysicalObject? physicalObject = null;

				if (itemStr.Contains("<oA>"))
				{
					physicalObject = SaveState.AbstractPhysicalObjectFromString(world, itemStr);
				}
				else if (itemStr.Contains("<cA>"))
				{
					physicalObject = SaveState.AbstractCreatureFromString(world, itemStr, false, coord ?? default(WorldCoordinate));
				}
				if (physicalObject != null && pos != null)
				{
					physicalObject.pos = (WorldCoordinate)pos;
				}
				return physicalObject;
			}
			catch (Exception ex)
			{
				Log.LogWarning($"从字符串解析物品失败: {ex.Message}");
			}
			return null;
		}
		
		public static Color GetRoomWaterColor(AbstractRoom abstractRoom)
        {
            if (abstractRoom == null || abstractRoom.world == null)
            {
                return Color.white;
            }

            try
            {
                RoomSettings? settings = null;
                if (abstractRoom.realizedRoom != null)
                {
                    settings = abstractRoom.realizedRoom.roomSettings;
                }
                if (settings == null)
                {
                    settings = new RoomSettings(null, WorldLoader.RoomNameManipulator(abstractRoom.FileName, abstractRoom.world.game), abstractRoom.world.region, template: false, firstTemplate: false, abstractRoom.world.game?.TimelinePoint, abstractRoom.world.game);
                }
                if (settings == null)
                {
                    return Color.white;
                }
                Texture2D paletteTex = LoadRoomPalette(settings.Palette);
                if (paletteTex == null)
                {
                    return Color.white;
                }
                Color waterColor = Color.Lerp(paletteTex.GetPixel(4, 15), paletteTex.GetPixel(4, 7),0.5f);
                return waterColor;
            }
            catch
            {
                return Color.white;
            }
        }
        
        private static Texture2D LoadRoomPalette(int paletteNumber)
        {
            Texture2D texture = new Texture2D(32, 16, TextureFormat.ARGB32, mipChain: false);

            string path = AssetManager.ResolveFilePath(
                "palettes" + Path.DirectorySeparatorChar +
                "palette" + paletteNumber.ToString(CultureInfo.InvariantCulture) + ".png"
            );

            try
            {
                AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + path, clampWrapMode: false, crispPixels: true);
            }
            catch
            {
                path = AssetManager.ResolveFilePath("palettes" + Path.DirectorySeparatorChar + "palette-1.png");
                AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + path, clampWrapMode: false, crispPixels: true);
            }

            texture.Apply(updateMipmaps: false);
            return texture;
        }
		

		public static (bool, List<int>) IncludeById(List<AbstractPhysicalObject> b, AbstractPhysicalObject a)
        {
			bool found = false;
			List<int> ints = new();

            for (int i = 0; i < b.Count; i++)
            {
				var item = b[i];

                if (EqualsById(a, item))
                {
                    found = true;
                    ints.Add(i);
                }
            }
			return (found, ints);
        }
        public static (bool, List<int>) IncludeById(List<string> b, string a)
        {
            bool found = false;
            List<int> ints = new();

            for (int i = 0; i < b.Count; i++)
            {
                var item = b[i];

                if (EqualsById(a, item))
                {
                    found = true;
                    ints.Add(i);
                }
            }
            return (found, ints);
        }
        public static bool EqualsById(AbstractPhysicalObject a, AbstractPhysicalObject b)
		{
			string strA = ObjectToString(a, a.pos, false);
			string strB = ObjectToString(b, b.pos, false);

            if (string.IsNullOrEmpty(strA) || string.IsNullOrEmpty(strB)) return false;
			int idA = ExtractId(strA);
			int idB = ExtractId(strB);
			if (idA == -1 || idB == -1) return false;
            return (idA == idB);
		}
		public static bool EqualsById(string strA, string strB)
		{
            if (string.IsNullOrEmpty(strA) || string.IsNullOrEmpty(strB)) return false;
            int idA = ExtractId(strA);
            int idB = ExtractId(strB);
            if (idA == -1 || idB == -1) return false;
            return (idA == idB);
        }

		public static int ExtractId(string str)
		{
			//ID.-1.5964<oB>0<oA>Rock<oA>SU_S01.20.16.0		(AbstractPhysicalObject)
			//ID.-1.2274<oB>0<oA>FirecrackerPlant<oA>SU_S01.23.17.0<oA>-1<oA>-1		(AbstractConsumable)
			//ID.-1.1980<oB>0<oA>Rock<oA>SU_S01.23.16.0		(AbstractPhysicalObject)
			//Hazer ID.-1.1982		(AbstractCreature)
			//ID(如5964)

			if (string.IsNullOrEmpty(str)) return -1;

			// 匹配 "ID." 后面跟着的整数（可选的 "-1." 前缀）
			Match match = Regex.Match(str, @"ID\.(?:-1\.)?(\d+)");

			if (match.Success && int.TryParse(match.Groups[1].Value, out int result))
			{
				return result;
			}

			return -1;
		}


	}
}
