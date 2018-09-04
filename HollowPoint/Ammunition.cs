using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowPoint
{
    class Ammunition
    {
        Ammunition thisAmmo;
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

        public Ammunition(String an, int ca, int ma, int cm, int mm, int dm, float fr)
        {
            ammoName = an;
            currAmmo = ca;
            maxAmmo = ma;
            currMag = cm;
            maxMag = mm;
            damage = dm;
            firerate = fr;          
        }

        public static List<Ammunition> CreateInstanceAmmuntionList()
        {
            List<Ammunition> am = new List<Ammunition>();

            am.Add(new Ammunition("45ACP", 10, 10, 5, 5, 8, 0.40f));
            am.Add(new Ammunition("5.56", 20, 20, 5, 5, 15, 0.40f));
            am.Add(new Ammunition("9MM", 30, 30, 5, 5, 3, 0.40f));

            return am;
        }

        public static Ammunition[] CreateInstanceAmmunitionArray()
        {
            Ammunition[] am = new Ammunition[4];

            am[0] = new Ammunition("Nail", 0, 0, 0, 0, 0, 0.40f);
            am[1] = new Ammunition("45ACP", 10, 10, 5, 5, 8, 0.40f);
            am[2] = new Ammunition("5.56", 20, 20, 5, 5, 15, 0.40f);
            am[3] = new Ammunition("9MM", 30, 30, 5, 5, 3, 0.40f);

            return am;
        }

    }
}
