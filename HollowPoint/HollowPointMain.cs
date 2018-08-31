using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modding;
using System.Reflection;


namespace HollowPoint
{
    public class HollowPointMain : Mod
    {
        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public override void Initialize()
        {
            ModHooks.Instance.AfterSavegameLoadHook += SaveGame;
            ModHooks.Instance.NewGameHook += NewGame;
        }

        public void NewGame()
        {
            GameManager.instance.gameObject.AddComponent<HPControl>();
            GameManager.instance.gameObject.AddComponent<HPUI>();
        }

        public void SaveGame(SaveGameData sgd)
        {
            GameManager.instance.gameObject.AddComponent<HPControl>();
            GameManager.instance.gameObject.AddComponent<HPUI>();
        }

    }
}
