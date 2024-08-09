using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Managers.Serializer
{
    public class BinarySerializer : ISerializer
    {
        private static BinarySerializer _instance;
        public static BinarySerializer Instance => _instance ??= new BinarySerializer();

        private BinarySerializer()
        {
        }

        public void Serialize(object obj, string dir, string filename)
        {
            var formatter = new BinaryFormatter();
            var path = Path.Combine(Application.persistentDataPath, dir + "/");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            using var fileStream = new FileStream(path + filename + ".binary", FileMode.Create);
            formatter.Serialize(fileStream, obj);
        }

        public T Deserialize<T>(string filePath, T ifNotExist)
        {
            var path = Path.Combine(Application.persistentDataPath, filePath + ".binary");
            if (!File.Exists(path)) return ifNotExist;
            var formatter = new BinaryFormatter();
            using var fileStream = new FileStream(path, FileMode.Open);
            var obj = (T)formatter.Deserialize(fileStream);
            return obj;
        }
    }
}