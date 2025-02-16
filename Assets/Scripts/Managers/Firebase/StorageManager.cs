using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
#if !UNITY_WEBGL
using Firebase.Storage;
#endif

namespace Managers.Firebase
{
    public class StorageManager : MonoBehaviour
    {
        private SceneManager _sm;
#if !UNITY_WEBGL
        private FirebaseStorage storage;
        private StorageReference storageRef;
#endif
        private void Awake()
        {
            _sm = FindFirstObjectByType<SceneManager>();
#if !UNITY_WEBGL
            storage = FirebaseStorage.DefaultInstance;
            storageRef = storage.GetReferenceFromUrl("gs://blockstorm-7ff87.appspot.com");
#endif
        }


        // Download a file from Firebase Storage
        public async Task DownloadFileAsync(string path)
        {
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, string.Concat(path.Split('/')[..^1]))))
                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath,
                    string.Concat(path.Split('/')[..^1])));
            var localPath = Path.Combine(Application.persistentDataPath, path);
#if !UNITY_WEBGL
            await storageRef.Child(path).GetFileAsync(localPath).ContinueWith(
                task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                        Debug.LogError("Download failed: " + task.Exception);
                    else
                        Debug.Log($"File {path} downloaded successfully!");
                });
#else
            throw new Exception("Feature not implemented");
#endif
        }
    }
}