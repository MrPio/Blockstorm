namespace Model
{
    public class Collectable
    {
        public enum CollectableType
        {
            Weapon, Ammo, Hp
        }

        public CollectableType Type;
        public Weapon Item;

        public Collectable(CollectableType type, Weapon item)
        {
            Type = type;
            Item = item;
        }
    }
}