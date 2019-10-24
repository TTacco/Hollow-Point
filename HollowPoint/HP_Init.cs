using Modding;
using System.IO;
using System.Reflection;
using UnityEngine;
using System;


namespace HollowPoint
{
    public class HollowPointInit : Mod, ITogglableMod
    {
        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public HollowPointInit()
        {
            FieldInfo field = typeof(Mod).GetField
                ("Name", BindingFlags.Instance | BindingFlags.Public);
            field?.SetValue(this, "Hollow Point");
        }

        public override void Initialize()
        {
            ModHooks.Instance.AfterSavegameLoadHook += SaveGame;
            ModHooks.Instance.NewGameHook += NewGame;
            LoadAssets.LoadResources();

        }

        private static void NewGame()
        {
            GameManager.instance.gameObject.AddComponent<HP_Prefabs>();
            GameManager.instance.gameObject.AddComponent<HP_AttackHandler>();
            GameManager.instance.gameObject.AddComponent<HP_DirectionHandler>();
            GameManager.instance.gameObject.AddComponent<HP_WeaponHandler>();
            GameManager.instance.gameObject.AddComponent<HP_WeaponSwapHandler>();
            //GameManager.instance.gameObject.AddComponent<HP_UIHandler>();
            GameManager.instance.gameObject.AddComponent<HP_DamageCalculator>();
            GameManager.instance.gameObject.AddComponent<HP_Sprites>();
            GameManager.instance.gameObject.AddComponent<HP_HeatHandler>();
            GameManager.instance.gameObject.AddComponent<HP_SpellControl>();
            //GameManager.instance.gameObject.AddComponent<HP_Stats>();

        }

        private static void SaveGame(SaveGameData sgd)
        {
            NewGame();
        }

        public void Unload()
        {
            ModHooks.Instance.AfterSavegameLoadHook -= SaveGame;
            ModHooks.Instance.NewGameHook -= NewGame;
        }

    }
}