using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingUI : MonoBehaviour
{
    public Image fillImage;

    public TextMeshProUGUI percentText;

    private float currentProgress = 0f;

    private float targetProgress = 0f;

    void Update()
    {
        currentProgress =
            Mathf.Lerp(
                currentProgress,
                targetProgress,
                Time.deltaTime * 10f
            );

        fillImage.fillAmount =
            currentProgress;

        int percent =
            Mathf.RoundToInt(
                currentProgress * 100f
            );

        percentText.text =
            percent + "%";
    }

    public void SetProgress(
        float value
    )
    {
        targetProgress =
            Mathf.Clamp01(value);
    }
}