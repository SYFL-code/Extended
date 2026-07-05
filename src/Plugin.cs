using BepInEx;
using BepInEx.Logging;
using System;
using UnityEngine;


namespace ExtensionLib
{
	[BepInPlugin(GUID, Name, Version)]
	class Plugin : BaseUnityPlugin
	{
        public const string GUID = "extension-lib.Redlyn";
        public const string Name = "Extension Lib";
        public const string Version = "0.1.0";

        public const string version = "0.1.14";

        private static bool isEnabled;

        //public static ManualLogSource Log { get; private set; }


        // Add hooks-添加钩子
        public void OnEnable()
		{
            if (Plugin.isEnabled)
            {
                return;
            }
            Plugin.isEnabled = true;

            Log.SetLog(base.Logger);

            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            // Put your custom hooks here!-在此放置你自己的钩子

            GlobalVar.Hook();

			// 保存加载
			SaveFile.AddData(new LibData());
            SaveFile.SaveHooks();

            MyPlayer.Hook();

            //PlayersStoreRetrieve.Hook();

            /*On.Player.Jump += Player_Jump;
			//在玩家触发跳跃时执行Player_Jump
			On.Player.Die += Player_Die;
			On.Lizard.ctor += Lizard_ctor;*/
        }

		public void OnDisable()
		{
            if (!Plugin.isEnabled)
            {
                return;
            }
            Plugin.isEnabled = false;

            // Unhook your hooks here!-在此取消你的钩子

            GlobalVar.Hook_();

            // 保存加载
            SaveFile.SaveHooks_();

			MyPlayer.Hook_();

            //PlayersStoreRetrieve.Hook_();
        }

        // Load any resources, such as sprites or sounds-加载任何资源 包括图像素材和音效
        private void LoadResources(RainWorld rainWorld)
		{
		}




        #region 默认钩子示例
        // Implement MeanLizards-实现激怒蜥蜴的效果
        /*private void Lizard_ctor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
		{
			orig(self, abstractCreature, world);

			if (MeanLizards.TryGet(world.game, out float meanness))
			{
				self.spawnDataEvil = Mathf.Min(self.spawnDataEvil, meanness);
			}
		}*/


        // Implement SuperJump-实现超高跳跃的效果
        /*private void Player_Jump(On.Player.orig_Jump orig, Player self)
		{
			orig(self);//总不能挂完钩子把原本该执行的东西给弄丢吧 这一句就是为了再把它塞进来让它正常运行

			if (SuperJump.TryGet(self, out var power))
			{
				self.jumpBoost *= 1f + power;
			}
		}*/

        // Implement ExlodeOnDeath-实现死亡自爆效果
        /*private void Player_Die(On.Player.orig_Die orig, Player self)
		{
			bool wasDead = self.dead;
			//布尔值wasDead判断玩家是否死亡

			orig(self);

			if (!wasDead && self.dead
				&& ExplodeOnDeath.TryGet(self, out bool explode)
				&& explode)
			{
				// Adapted from ScavengerBomb.Explode-改编自ScavengerBomb.Explode，即拾荒者炸弹的爆炸效果
				var room = self.room;
				var pos = self.mainBodyChunk.pos;
				var color = self.ShortCutColor();
				//这三行分别获取了房间 身体位置和ShortCutColor，也就是这个生物通过管道时显示的颜色
				room.AddObject(new Explosion(room, self, pos, 7, 250f, 6.2f, 2f, 280f, 0.25f, self, 0.7f, 160f, 1f));
				//在 当前房间 从自己身体 在当前身体的位置 生成一个 持续时长7 半径250，力度6.2，伤害2，眩晕280，致聋0.25，判定击杀由自己造成，伤害乘数0.7，最小眩晕160，背景噪声1的爆炸
				room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, color));
				//玩家位置 半径280，透明度1，持续时长7，发光颜色就是上面的shortcutcolor的爆炸光效
				room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
				//和上面差不多，玩家位置 半径230，透明度1，持续时长3，发光颜色1f1f1f
				room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, color));
				//自己解释下这玩意罢咱累了（

				room.AddObject(new ShockWave(pos, 330f, 0.045f, 5, false));
				//或许没那么累 在自身位置生成冲击波效果 大小330 强度0.045 时长5 false表示绘制顺序（绘制到HUD图层，True就是HUD2
				//思考一下 如果把持续时长改成180会怎么样
				//再想想如何让这玩意影响面积更大（？

				room.ScreenMovement(pos, default, 1.3f);
				//屏幕震动
				room.PlaySound(SoundID.Bomb_Explode, pos);
				//播放爆炸音效
				room.InGameNoise(new Noise.InGameNoise(pos, 9000f, self, 1f));
				//游戏内噪声效果
			}
		}*/
        #endregion


    }
}