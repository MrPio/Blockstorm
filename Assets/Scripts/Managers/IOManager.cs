using System.IO;
using UnityEngine;

namespace Managers
{
    public static class IOManager
    {
        public static void Serialize(object obj, string dir, string filename)
        {
            var path = Path.Combine(Application.persistentDataPath, dir + "/");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            File.WriteAllText(path + filename + ".json", JsonUtility.ToJson(obj));
        }

        public static T Deserialize<T>(string filePath) =>
            JsonUtility.FromJson<T>(File.ReadAllText(Path.Combine(Application.persistentDataPath, filePath + ".json")));
    }
}