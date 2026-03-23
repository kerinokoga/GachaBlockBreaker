using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ポーズメニュー：リジューム・リタイヤ・BGM/SE音量設定を担う
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("音量")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    [Header("ヘルプ")]
    [SerializeField] private GameObject helpPanel;

    [Header("リタイヤ確認ダイアログ")]
    [SerializeField] private GameObject retireConfirmPanel;

    void OnEnable()
    {
        if (bgmSlider != null)
            bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
        if (seSlider != null)
            seSlider.value = PlayerPrefs.GetFloat("SEVolume", 1f);

        if (helpPanel != null) helpPanel.SetActive(false);
        if (retireConfirmPanel != null) retireConfirmPanel.SetActive(false);
    }

    // ---- ボタンコールバック ----

    public void OnResumeClicked()
    {
        GameManager.Instance?.Resume();
        gameObject.SetActive(false);
    }

    public void OnRetireClicked()
    {
        if (retireConfirmPanel != null)
            retireConfirmPanel.SetActive(true);
    }

    public void OnRetireConfirmYes()
    {
        GameManager.Instance?.Retire();
    }

    public void OnRetireConfirmNo()
    {
        if (retireConfirmPanel != null)
            retireConfirmPanel.SetActive(false);
    }

    public void OnHelpClicked()
    {
        if (helpPanel != null)
            helpPanel.SetActive(true);
    }

    public void OnHelpCloseClicked()
    {
        if (helpPanel != null)
            helpPanel.SetActive(false);
    }

    // ---- 設定コールバック ----

    public void OnBGMVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("BGMVolume", value);
        AudioManager.Instance?.SetBGMVolume(value);
    }

    public void OnSEVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SEVolume", value);
        AudioManager.Instance?.SetSEVolume(value);
    }
}
