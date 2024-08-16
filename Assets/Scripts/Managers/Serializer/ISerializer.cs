namespace Managers.Serializer
{
    public interface ISerializer
    {
        public const string MapsDir = "maps/";
        public const string ConfigsDir = "configs/";
        public const string DebugDir = "debug/";

        public void Serialize(object obj, string dir, string filename);

        public T Deserialize<T>(string filePath, T ifNotExist);
    }
}