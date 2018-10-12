using Modding;
using System.Reflection;


namespace HollowPoint
{
    public class HollowPointMain : Mod, ITogglableMod 
    {
        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public HollowPointMain()
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
            GameManager.instance.gameObject.AddComponent<HPControl>();
            GameManager.instance.gameObject.AddComponent<HPUI>();
            GameManager.instance.gameObject.AddComponent<AmmunitionControl>();
            GameManager.instance.gameObject.AddComponent<SpellControl>();
            GameManager.instance.gameObject.AddComponent<CharmControl>();
            GameManager.instance.gameObject.AddComponent<BulletObject>();
            GameManager.instance.gameObject.AddComponent<GunSpriteController>();
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
