using Network;
using UI;
using UnityEngine;
using VoxelEngine;

namespace Managers
{
    public class SceneManager : MonoBehaviour
    {
        [Header("Ground")] public ParticleSystem blockDigEffect;
        public Transform highlightBlock;
        public Transform placeBlock;

        [Header("Prefabs")] public GameObject playerPrefab;

        [Header("UI")] public Canvas worldCanvas;
        public Transform crosshair;
        public Animator crosshairAnimator;
        public Dashboard dashboard;

        [Header("Managers")] public WorldManager worldManager;
        public ClientManager clientManager;
        public ServerManager serverManager;
    }
}