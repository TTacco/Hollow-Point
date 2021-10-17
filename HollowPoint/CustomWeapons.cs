using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace HollowPoint
{
    public static class CustomWeapons
    {
        internal static List<CustomGun> CustomGuns = new List<CustomGun>();
        internal static Dictionary<int, (Sprite, Sprite)> CustomSprites = new Dictionary<int, (Sprite, Sprite)>();
        public static void AddWeapon(CustomGun gunSettings)
            => CustomGuns.Add(gunSettings);
        public static int ReserveSpriteSlot()
        {
            int slot = CustomSprites.Count + 7;
            CustomSprites.Add(slot, (null, null));
            return slot;
        }
        public static void SetGunSprite(int slot, Sprite s)
        {
            var tuple = CustomSprites[slot];
            tuple.Item1 = s;
            CustomSprites[slot] = tuple;
        }
        public static void SetBulletSprite(int slot, Sprite s)
        {
            var tuple = CustomSprites[slot];
            tuple.Item2 = s;
            CustomSprites[slot] = tuple;
        }
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
