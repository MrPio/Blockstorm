using System.Linq;
using EasyButtons;
using UnityEngine;
using VoxelEngine;

public class MapConverter : MonoBehaviour
{
    [Button]
    private void GenerateMap()
    {
        var meshRenderers = GameObject.Find("Map").GetComponentsInChildren<MeshRenderer>();
        foreach (var tex in meshRenderers.Select(e => e.material.mainTexture))
        {
            var id = int.Parse(tex.name.Split('_')[1]) - 1;
            print(WorldManager.instance.blockTypes.First(e => e.topID == id || e.bottomID == id || e.sideID == id).name);
        }
    }
}