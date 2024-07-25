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
        
        /*void SaveToFile(string filePath, MyClass obj)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                formatter.Serialize(fileStream, obj);
            }
            Debug.Log("Object saved to " + filePath);
        }*/
        
        /*MyClass LoadFromFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                MyClass obj = (MyClass)formatter.Deserialize(fileStream);
                Debug.Log("Object loaded from " + filePath);
                return obj;
            }
        }
        else
        {
            Debug.LogError("File not found: " + filePath);
            return null;
        }
    }*/
    }
}