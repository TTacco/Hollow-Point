using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowPoint
{
    struct Ammunition
    {
        public string AmmoName;
        public int CurrAmmo;
        public int MaxAmmo;
        public int CurrMag;
        public int MaxMag;
        public int Damage;
        public float Firerate;

        public Ammunition(string ammoName, int maxAmmo, int maxMag, int damage, float firerate)
        {
            AmmoName = ammoName;
            MaxAmmo = maxAmmo;
            MaxMag = maxMag;
            CurrAmmo = maxAmmo;
            CurrMag = maxMag;
            Damage = damage;
            Firerate = firerate;
        }
    }
    
    
    static class Ammo
    {
        public static Ammunition[] CreateInstanceAmmunitionArray()
        {
            Ammunition[] am = new Ammunition[3];

            am[0] = new Ammunition("45ACP", 10, 5, 8, 0.40f);
            am[1] = new Ammunition("5.56", 20, 5, 15, 0.40f);
            am[2] = new Ammunition("9MM", 30, 5, 3, 0.40f);

            return am;
        }


    }
}
