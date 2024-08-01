namespace Model
{
    public enum CollectableType
    {
        Weapon,
        Ammo,
        Hp
    }

    public class Collectable
    {
        public CollectableType Type;
        public Weapon Item;

        public Collectable(CollectableType type, Weapon item)
        {
            Type = type;
            Item = item;
        }
    }
}