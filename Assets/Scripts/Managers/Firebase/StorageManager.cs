using System;
using System.IO;
using System.Threading.Tasks;
using Firebase.Storage;
using UnityEngine;

namespace Managers.Firebase
{
    public class StorageManager : MonoBehaviour
    {
        private SceneManager _sm;
        private FirebaseStorage storage;
        private StorageReference storageRef;

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
            storage = FirebaseStorage.DefaultInstance;
            storageRef = storage.GetReferenceFromUrl("gs://blockstorm-7ff87.appspot.com");
        }


        // Download a file from Firebase Storage
        public async Task DownloadFileAsync(string path)
        {
            Debug.Log(path);
            await storageRef.Child(path).GetFileAsync(Path.Combine(Application.persistentDataPath, path)).ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    Debug.LogError("Download failed: " + task.Exception);
                else
                    Debug.Log($"File {path} downloaded successfully!");
            });
        }
    }
}