using System.Collections.Generic;
using UnityEngine;

public class CanvasEffects : MonoBehaviour
{
    public static CanvasEffects instance;
    public List<GameObject> muzzles;

    private void Start()
    {
        instance = this;
    }
}
