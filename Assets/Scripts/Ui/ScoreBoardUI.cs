using TMPro;
using UnityEngine;

public class ScoreBoardUI : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text onlineText;

    public TMP_Text rankText;

    public TMP_Text trailPercentText;

    void Update()
    {
        UpdateOnline();

        UpdateRank();

        UpdateTrailPercent();
    }

    void UpdateOnline()
    {
        int count =
            FindObjectsByType
            <PlayerController>(
                FindObjectsSortMode.None
            ).Length;

        onlineText.text =
            "ONLINE : "
            + count;
    }

    void UpdateRank()
    {
        rankText.text =
            "RANK : 1";
    }

    void UpdateTrailPercent()
    {
        trailPercentText.text =
            "AREA : 0%";
    }
}