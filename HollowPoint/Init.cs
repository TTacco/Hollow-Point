using Modding;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace HollowPoint
{
    public class HollowPointInit : Mod, ITogglableMod
    {
        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        //public HollowPointInit() : base("Hollow Point");

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

        private void NewGame()
        {
            GameManager.instance.gameObject.AddComponent<HollowPointPrefabs>();
            GameManager.instance.gameObject.AddComponent<AttackHandler>();
            GameManager.instance.gameObject.AddComponent<OrientationHandler>();
            GameManager.instance.gameObject.AddComponent<WeaponSwapAndStatHandler>();
            GameManager.instance.gameObject.AddComponent<UIHandler>();
            GameManager.instance.gameObject.AddComponent<DamageOverride>();
            GameManager.instance.gameObject.AddComponent<HollowPointSprites>();
            GameManager.instance.gameObject.AddComponent<HeatHandler>();
            GameManager.instance.gameObject.AddComponent<SpellControlOverride>();
            GameManager.instance.gameObject.AddComponent<Stats>();
            GameManager.instance.gameObject.AddComponent<HudController>();
            GameManager.instance.gameObject.AddComponent<AudioHandler>();
        }

        private void SaveGame(SaveGameData sgd)
        {
            NewGame();
        }

        public void Unload()
        {
            ModHooks.Instance.AfterSavegameLoadHook -= SaveGame;
            ModHooks.Instance.NewGameHook -= NewGame;
            Modding.Logger.Log("Unload on Init is called");
        }

        public void OnDestroy()
        {
            ModHooks.Instance.AfterSavegameLoadHook -= SaveGame;
            ModHooks.Instance.NewGameHook -= NewGame;
            Modding.Logger.Log("Destroy on Init is called");
        }
    }
}