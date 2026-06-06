using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extended
{
    public class Helper
    {

        // 将物品转为字符串
        public static string ObjectToString(AbstractPhysicalObject Object, WorldCoordinate coord, bool setCoord)
        {
            try
            {
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

            return Object.ToString();
        }
        // 从字符串解析物品
        public static AbstractPhysicalObject? ObjectFromString(string itemStr, World world, WorldCoordinate? coord, WorldCoordinate? pos)
        {
            try
            {
                if (string.IsNullOrEmpty(itemStr))
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

    }
}
