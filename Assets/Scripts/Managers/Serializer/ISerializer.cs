namespace Managers.IO
{
    public interface ISerializer
    {
        public void Serialize(object obj, string dir, string filename);

        public T Deserialize<T>(string filePath);
    }
}