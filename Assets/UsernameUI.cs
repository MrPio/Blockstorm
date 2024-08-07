using System;
using Managers.Serializer;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class UsernameUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usernameText, editUsernameText;
    private ISerializer _serializer;
    private string _username;

    private void Start()
    {
        _serializer = BinarySerializer.Instance;
        try
        {
            _username = _serializer.Deserialize<string>($"{ISerializer.ConfigsDir}/username");
        }
        catch (Exception e)
        {
            SaveUsername($"Player{Random.Range(10, 10000)}");
        }

        usernameText.text = _username;
    }

    public void SaveUsername(string username)
    {
        if (username.Length < 4)
            return;

        _username = username;
        usernameText.text = _username;
        _serializer.Serialize(_username, ISerializer.ConfigsDir, "username");
        print("New username saved successfully!");
    }
}