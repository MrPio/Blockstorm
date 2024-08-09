using Managers;
using Managers.Serializer;
using TMPro;
using UnityEngine;

public class UsernameUI : MonoBehaviour
{
    private SceneManager _sm;
    [SerializeField] private TextMeshProUGUI usernameText, editUsernameText;
    private ISerializer _serializer;

    private void Awake()
    {
        _sm = FindObjectOfType<SceneManager>();
    }

    public void Initialize()
    {
        usernameText.text = _sm.lobbyManager.Username;
    }

    public void SaveUsername(string username)
    {
        if (username.Length < 4)
            return;

        _sm.lobbyManager.Username = username.Length > 31 ? username[..31] : username;
        usernameText.text = _sm.lobbyManager.Username;
        _serializer.Serialize(_sm.lobbyManager.Username, ISerializer.ConfigsDir, "username");
        print("New username saved successfully!");
    }
}