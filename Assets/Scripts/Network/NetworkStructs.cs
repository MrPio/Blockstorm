using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using VoxelEngine;

namespace Network
{
    /// <summary>
    /// A string message of 512 bytes in size.
    /// </summary>
    public struct NetString : INetworkSerializable
    {
        public FixedString512Bytes Message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Message);
        }
    }

    /// <summary>
    /// A list of changes to the original map.
    /// Each voxel edit has three coordinates and the id of the new block type.
    /// </summary>
    /// <remarks> This is used to synchronise the map status when a new player connects. </remarks>
    public struct MapStatus : INetworkSerializable
    {
        public short[] Xs, Ys, Zs;
        public byte[] Ids;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Xs);
            serializer.SerializeValue(ref Ys);
            serializer.SerializeValue(ref Zs);
            serializer.SerializeValue(ref Ids);
        }

        public MapStatus(Map map)
        {
            // Debug.Log($"Host is updating the MapStatus...");
            var positions = map.BlocksEdits.Keys;
            var ids = map.BlocksEdits.Values;
            Xs = positions.Select(it => (short)it.x).ToArray();
            Ys = positions.Select(it => (short)it.y).ToArray();
            Zs = positions.Select(it => (short)it.z).ToArray();
            Ids = ids.ToArray();
        }
    }
}