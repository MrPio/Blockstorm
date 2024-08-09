using Managers;
using Managers.Serializer;
using TMPro;
using UnityEngine;

namespace UI
{
    public class UsernameUI : MonoBehaviour
    {
        private SceneManager _sm;
        [SerializeField] private TextMeshProUGUI usernameText, editUsernameText;
        [SerializeField] private GameObject editUsername,confirmIcon;
        private ISerializer _serializer;

        private void Awake()
        {
            _sm = FindObjectOfType<SceneManager>();
            _serializer=BinarySerializer.Instance;
        }

        public void Initialize()
        {
            usernameText.text = _sm.lobbyManager.Username;
        }

        public void SaveUsername(string username)
        {
            usernameText.gameObject.SetActive(true);
            editUsername.SetActive(false);
            confirmIcon.SetActive(false);

            if (username.Length < 4)
                return;

            _sm.lobbyManager.Username = username.Length > 31 ? username[..31] : username;
            usernameText.text = _sm.lobbyManager.Username;
            _serializer.Serialize(_sm.lobbyManager.Username, ISerializer.ConfigsDir, "username");
            print("New username saved successfully!");
        }
    }
}