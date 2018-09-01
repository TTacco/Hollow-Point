using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowPoint
{
    class Ammunition
    {
        private static Ammunition[] ammoInstance = new Ammunition[6];

        private Ammunition() { }

        public static Ammunition[] GetAmmoInstance()
        {
            ammoInstance[0].ammoName = "9mm";
            ammoInstance[0].currAmmo = 10;
            ammoInstance[0].maxAmmo = 10;
            ammoInstance[0].currMag = 5;
            ammoInstance[0].maxMag = 5;
            ammoInstance[0].damage = 8;
            ammoInstance[0].firerate = 0.40f;

            ammoInstance[1].ammoName = "45acp";
            ammoInstance[1].currAmmo = 10;
            ammoInstance[1].maxAmmo = 10;
            ammoInstance[1].currMag = 5;
            ammoInstance[1].maxMag = 5;
            ammoInstance[1].damage = 8;
            ammoInstance[1].firerate = 0.40f;

            ammoInstance[2].ammoName = "5.56";
            ammoInstance[2].currAmmo = 10;
            ammoInstance[2].maxAmmo = 10;
            ammoInstance[2].currMag = 5;
            ammoInstance[2].maxMag = 5;
            ammoInstance[2].damage = 8;
            ammoInstance[2].firerate = 0.40f;

            return ammoInstance;
        }

        String ammoName;
        int currAmmo;
        int maxAmmo;
        int currMag;
        int maxMag;
        int damage;
        float firerate;

        public string AmmoName { get => ammoName; set => ammoName = value; }
        public int CurrAmmo { get => currAmmo; set => currAmmo = value; }
        public int MaxAmmo { get => maxAmmo; set => maxAmmo = value; }
        public int CurrMag { get => currMag; set => currMag = value; }
        public int MaxMag { get => maxMag; set => maxMag = value; }
        public int Damage { get => damage; set => damage = value; }
        public float Firerate { get => firerate; set => firerate = value; }
    }
}
