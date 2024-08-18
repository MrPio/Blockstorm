using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace Managers.Encoder
{
    public class GzipEncoder : IEncoder
    {
        private static GzipEncoder _instance;
        public static GzipEncoder Instance => _instance ??= new GzipEncoder();

        private GzipEncoder()
        {
        }

        public void Encode(string path)
        {
            using var gzipFileStream = new FileStream(Path.Combine(Application.persistentDataPath, path), FileMode.Open,
                FileAccess.Read);
            using var decompressionStream = new GZipStream(gzipFileStream, CompressionMode.Compress);
            using var outputFileStream = new FileStream(Path.Combine(Application.persistentDataPath, $"{path}.gz"),
                FileMode.Create, FileAccess.Write);
            decompressionStream.CopyTo(outputFileStream);
        }

        public string Decode(string path)
        {
            var newPath = string.Join(".gz", path.Split(".gz")[..^1]);
            using var gzipFileStream = new FileStream(Path.Combine(Application.persistentDataPath, path), FileMode.Open,
                FileAccess.Read);
            using var decompressionStream = new GZipStream(gzipFileStream, CompressionMode.Decompress);
            using var outputFileStream =
                new FileStream(Path.Combine(Application.persistentDataPath, newPath), FileMode.Create,
                    FileAccess.Write);
            decompressionStream.CopyTo(outputFileStream);
            return newPath;
        }
    }
}