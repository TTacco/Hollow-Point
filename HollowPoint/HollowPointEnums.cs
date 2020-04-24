namespace HollowPoint
{
    public class HollowPointEnums
    {
        public enum FireModes
        {
            Single,
            Burst,
            Spread,
        }

        public enum BulletType
        {
            Standard,
            Secondary,
            Flare,
            FireSupport,
            Typhoon,
        }

        public enum GunType
        {
            Primary, 
            Secondary,
        }

        public enum WeaponType
        {
            Melee,
            Ranged,
        }

        public enum DirectionalOrientation
        {
            Vertical,
            Horizontal,
            Diagonal,
        }


        public enum DamageSeverity
        {
            Critical,
            Major,
            Minor,
            Nullified,
        }
    }
}
