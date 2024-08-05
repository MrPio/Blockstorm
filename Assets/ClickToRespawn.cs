using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Assuming it starts disabled with its children
/// </summary>
public class ClickToRespawn : MonoBehaviour
{
    public const float DelayBeforeRespawning = 5f;
    private SceneManager _sm;
    private List<Transform> _children;
    private bool _canRespawn;

    private void Start()
    {
        _sm = FindObjectOfType<SceneManager>();
        _children = transform.GetComponentsInChildren<Transform>().Where(it => it != transform).ToList();
        HideChildren();
    }

    private void HideChildren() => _children.ForEach(it => it.gameObject.SetActive(false));

    private void Update()
    {
        if (!_canRespawn) return;
        if (Input.GetMouseButtonDown(0))
        {
            _sm.serverManager.RespawnServerRpc(new ServerRpcParams
            {
                Send = new ServerRpcSendParams(),
                Receive = new ServerRpcReceiveParams { SenderClientId = _sm.clientManager.OwnerClientId }
            });
            HideChildren();
            gameObject.SetActive(false);
            _canRespawn = false;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(ShowText());
        return;

        IEnumerator ShowText()
        {
            yield return new WaitForSeconds(DelayBeforeRespawning);
            _children.ForEach(it => it.gameObject.SetActive(true));
            _canRespawn = true;
        }
    }
}