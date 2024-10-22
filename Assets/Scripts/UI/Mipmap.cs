using System.Collections.Generic;
using Managers;
using Model;
using Prefabs.Player;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class Mipmap : MonoBehaviour
    {
        private SceneManager _sm;
        [SerializeField] private GameObject playerMarker;
        private Transform _player;
        private Dictionary<ulong, PlayerMarker> spawnedMarkers = new();

        private void Start()
        {
            _sm = FindObjectOfType<SceneManager>();
            Debug.Log(_sm.worldManager.Map.name);
            Instantiate(Resources.Load<GameObject>($"Prefabs/mipmaps/maps/{_sm.worldManager.Map.name}"), transform);
        }

        /// <summary>
        /// Add a player marker to the mipmap.
        /// The marker should always mimic the player's position and rotation until the player dies.
        /// </summary>
        /// <param name="team"> The color of to assign to the marker. </param>
        /// <param name="player"> The player's transform to mimic. </param>
        public void AddPlayerMarker(Team team, Transform player)
        {
            var go = Instantiate(playerMarker, transform);
            var playerId = player.GetComponent<Player>().OwnerClientId;
            if (spawnedMarkers.TryGetValue(playerId, out var marker))
                Destroy(marker.gameObject);
            spawnedMarkers[playerId] = go.GetComponent<PlayerMarker>();
            spawnedMarkers[playerId].player = player;
            go.GetComponent<Image>().color = TeamData.Colors[team];
        }
    }
}