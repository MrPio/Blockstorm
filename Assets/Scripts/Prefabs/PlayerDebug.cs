using System;
using Model;
using Unity.Netcode;
using UnityEngine;
using Logger = UI.Logger;
using Random = UnityEngine.Random;

namespace Prefabs
{
    public class PlayerDebug : NetworkBehaviour
    {
        [SerializeField] private Camera mainCamera;
        private Logger _logger;

        private readonly NetworkVariable<byte> teamIndex = new(4, NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<Team> team = new(Team.None, NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            _logger = FindObjectOfType<Logger>();
        }


        public override void OnNetworkSpawn()
        {
            mainCamera.gameObject.SetActive(IsOwner);
            // If I'm the owner, randomly spawn the player and change the team
            if (IsOwner)
            {
                transform.position = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                ChangeTeam();
            }
            else
            {
                teamIndex.OnValueChanged += (_, newValue) =>
                {
                    _logger.Log($"[teamIndex] new value={newValue}", color: Color.yellow);
                };
                team.OnValueChanged += (_, newValue) =>
                {
                    _logger.Log($"[team] new value={newValue}", color: Color.yellow);
                };
                _logger.Log($"[teamIndex] current value={teamIndex.Value}", color: Color.yellow);
                _logger.Log($"[team] current value={team.Value}", color: Color.yellow);
            }
        }

        private void Update()
        {
            if (!IsOwner) return;
            if (Input.GetKeyDown(KeyCode.Space))
                ChangeTeam();
        }

        // Choose a new random team
        private void ChangeTeam()
        {
            var newTeam = (byte)Random.Range(0, 4);
            teamIndex.Value = newTeam;
            team.Value = (Team)Enum.GetValues(typeof(Team)).GetValue(newTeam);
            _logger.Log($"New team '{team.Value}' selected!", color: Color.cyan);
        }
    }
}