using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowPoint
{
    class Ammunition
    {
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
