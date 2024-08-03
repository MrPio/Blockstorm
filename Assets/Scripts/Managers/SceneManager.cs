using Network;
using UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
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
        public HpHUD hpHUD;
        public AmmoHUD ammoHUD;
        public ReloadBar reloadBar;
        public ReloadBar staminaBar;
        public Transform circleDamageContainer;
        public Mipmap mipmap;
        public GameObject scopeContainer;
        public CrosshairFire crosshairFire;
        public CrosshairFire scopeFire;

        [Header("Managers")] public WorldManager worldManager;
        public ClientManager clientManager;
        public ServerManager serverManager;
        public NetworkManager networkManager;
    }
}