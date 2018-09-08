using System.Linq;

namespace HollowPoint
{
    public struct Ammunition
    {
        public string AmmoName;
        public int CurrAmmo;
        public int MaxAmmo;
        public int CurrMag;
        public int MaxMag;
        public int Damage;
        public float Firerate;
        public float ReloadTime;
        public int MaxDegreeDeviation;
        public int CurrRecoilDeviation;
        public int SoulGain;
        // Each bullet can only hit enemies every x seconds
        public float hitCooldown;
        // Number of enemies bullet can travel through. 0 for infinite.
        public int PierceNumber;

        public Ammunition(string ammoName, int maxAmmo, int maxMag, int damage, float firerate, float reloadtime, int maxDegreeDeviation, int currRecoilDeviation, int soulGain, int pierceNumber)
        {
            AmmoName = ammoName;
            MaxAmmo = maxAmmo;
            MaxMag = maxMag;
            CurrAmmo = maxAmmo;
            CurrMag = maxMag;

            Damage = damage;
            Firerate = firerate;
            ReloadTime = reloadtime;

            MaxDegreeDeviation = maxDegreeDeviation;
            CurrRecoilDeviation = currRecoilDeviation;

            SoulGain = soulGain;
            hitCooldown = 0.2f;
            PierceNumber = pierceNumber;
        }
    }


    //reloadtime are of course still subjected to changes, add in 12gauge (shotgun), 7.62 (sniper rifle), .50 (anti materiel) and more if needed next time

    static class Ammo
    {
        public static Ammunition[] ammoTypes = new[]
        {
            new Ammunition("Nail", 0, 0, 0, 0, 0, 0, 0, 0, 0),
            new Ammunition(".45ACP", 10, 5, 8, 0.40f, 0.01f, 3, 1, 15, 1),
            new Ammunition("9mm", 30, 5, 3, 0.40f, 0.02f, 20, 1, 2, 1),
            new Ammunition("12 Gauge", 4, 5, 3, 0.40f, 0.03f, 20, 20, 5, 3),
            new Ammunition("5.56", 20, 5, 15, 0.40f, 0.04f, 10, 1, 5, 2),
            new Ammunition("7.62", 5, 5, 40, 0.80f, 0.05f, 0, 0, 0, 1),
        };

    }
}