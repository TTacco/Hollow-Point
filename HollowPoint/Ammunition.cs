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
        public int SoulGain;
        // Each bullet can only hit enemies every x seconds
        public float hitCooldown;
        // Number of enemies bullet can travel through. 0 for infinite.
        public int PierceNumber;

        public Ammunition(string ammoName, int maxAmmo, int maxMag, int damage, float firerate, int soulGain, int pierceNumber)
        {
            AmmoName = ammoName;
            MaxAmmo = maxAmmo;
            MaxMag = maxMag;
            CurrAmmo = maxAmmo;
            CurrMag = maxMag;
            Damage = damage;
            Firerate = firerate;
            SoulGain = soulGain;
            hitCooldown = 0.2f;
            PierceNumber = pierceNumber;
        }
    }


    static class Ammo
    {
        public static readonly Ammunition[] ammoTypes = new[]
        {
            new Ammunition("Nail", 0, 0, 0, 0, 0, 0),
            new Ammunition("45ACP", 10, 5, 8, 0.40f, 15, 0),
            new Ammunition("5.56", 20, 5, 15, 0.40f, 5, 2),
            new Ammunition("9MM", 30, 5, 3, 0.40f, 2, 1)
        };

    }
}