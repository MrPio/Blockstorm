using Network;
using TMPro;
using UnityEngine;

public class ScoresHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI red, blue, green, yellow;

    public void SetScores(Scores scores)
    {
        red.text = scores.Red.ToString();
        blue.text = scores.Blue.ToString();
        green.text = scores.Green.ToString();
        yellow.text = scores.Yellow.ToString();
    }

    public void Reset() => SetScores(new Scores());
}