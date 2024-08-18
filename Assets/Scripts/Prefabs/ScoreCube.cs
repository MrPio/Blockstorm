using System.Collections.Generic;
using System.Linq;
using Managers;
using Model;
using Network;
using Unity.Netcode;
using UnityEngine;

namespace Prefabs
{
    public class ScoreCube : NetworkBehaviour
    {
        private SceneManager _sm;
        public readonly List<Player.Player> insidePlayers = new();
        private List<Team> InsideTeams => insidePlayers.Select(it => it.Team).ToList();

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsHost)
                InvokeRepeating(nameof(AddPoints), 1f, 1f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsHost) return;
            if (other.gameObject.CompareTag("Player"))
            {
                var player = other.gameObject.GetComponentInParent<Player.Player>();
                if (insidePlayers.Contains(player)) return;
                insidePlayers.Add(player);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsHost) return;
            if (other.gameObject.CompareTag("Player"))
            {
                var player = other.gameObject.GetComponentInParent<Player.Player>();
                if (!insidePlayers.Contains(player)) return;
                insidePlayers.Remove(player);
            }
        }

        private void AddPoints()
        {
            var insideTeams = InsideTeams;
            if (insideTeams.Count <= 0) return;
            var mostFrequent = insideTeams.GroupBy(x => x).OrderByDescending(g => g.Count()).First();
            var count = insideTeams.Count(v => v == mostFrequent.Key);
            var allSame = insideTeams.Any(it => it != mostFrequent.Key) &&
                          insideTeams.All(x => insideTeams.Count(v => v == x) == mostFrequent.Count());
            if (!allSame)
            {
                var oldScore = _sm.ClientManager.scores.Value;
                _sm.ClientManager.scores.Value = new Scores
                {
                    Red = (ushort)(oldScore.Red + (mostFrequent.Key is Team.Red ? count : 0)),
                    Blue = (ushort)(oldScore.Blue + (mostFrequent.Key is Team.Blue ? count : 0)),
                    Green = (ushort)(oldScore.Green + (mostFrequent.Key is Team.Green ? count : 0)),
                    Yellow = (ushort)(oldScore.Yellow + (mostFrequent.Key is Team.Yellow ? count : 0)),
                };
            }
        }
    }
}