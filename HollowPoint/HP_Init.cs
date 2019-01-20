using Modding;
using System.Reflection;


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
            LoadAssets.LoadBulletSounds();
        }

        private static void NewGame()
        {
            GameManager.instance.gameObject.AddComponent<HP_Handler>();
            GameManager.instance.gameObject.AddComponent<HP_Sprites>();
            GameManager.instance.gameObject.AddComponent<HP_DirectionHandler>();
            GameManager.instance.gameObject.AddComponent<HP_BulletPrefab>();
            GameManager.instance.gameObject.AddComponent<HP_SwapWeapon>();
            GameManager.instance.gameObject.AddComponent<HP_UIHandler>();
            //GameManager.instance.gameObject.AddComponent<SpellControl>();
            //GameManager.instance.gameObject.AddComponent<CharmControl>();
            //GameManager.instance.gameObject.AddComponent<BulletObject>();
            //GameManager.instance.gameObject.AddComponent<GunSpriteController>();
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