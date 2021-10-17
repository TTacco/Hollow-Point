using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowPoint
{
    public static class CustomWeapons
    {
        internal static List<CustomGun> CustomGuns = new List<CustomGun>();
        public static void AddWeapon(Gun gunSettings, Func<bool> useGun, bool overrideOriginal)
            => AddWeapon(new CustomGun(gunSettings, useGun, overrideOriginal));
        public static void AddWeapon(Gun gunSettings, int charmNum, bool overrideOriginal)
            => AddWeapon(new CustomGun(gunSettings, charmNum, overrideOriginal));
        public static void AddWeapon(CustomGun gunSettings)
            => CustomGuns.Add(gunSettings);
    }
    public enum CustomGunType
    {
        charm,
        func
    }
    public struct CustomGun
    {
        public Gun settings;
        public CustomGunType type;
        public int charmNum;
        public Func<bool> useGun;
        public bool overrideOriginal;
        public CustomGun(Gun settings, int charmNum, bool overrideOriginal)
        {
            this.settings = settings;
            type = CustomGunType.charm;
            this.charmNum = charmNum;
            useGun = null;
            this.overrideOriginal = overrideOriginal;
        }
        public CustomGun(Gun settings, Func<bool> useGun, bool overrideOriginal)
        {
            this.settings = settings;
            type = CustomGunType.func;
            charmNum = default(int);
            this.useGun = useGun;
            this.overrideOriginal = overrideOriginal;
        }
    }
}
