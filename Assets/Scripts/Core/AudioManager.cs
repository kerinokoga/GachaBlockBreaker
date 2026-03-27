using UnityEngine;

/// <summary>
/// BGM / SE の再生・音量管理を一元化するシングルトン
/// DontDestroyOnLoad でシーン間を通して存在し続ける
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("AudioSource")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("BGM クリップ（Inspector で設定）")]
    public AudioClip bgmHome;
    public AudioClip bgmGame;
    public AudioClip bgmGacha;
    public AudioClip bgmResult;

    [Header("SE クリップ（Inspector で設定）")]
    public AudioClip seBlockBreak;
    public AudioClip seMiss;
    public AudioClip seClear;
    public AudioClip seGameOver;
    public AudioClip seBallLaunch;
    public AudioClip seButton;
    public AudioClip seGacha;
    public AudioClip seUlt;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource が未設定の場合はランタイムで追加
        if (bgmSource == null)
        {
            var bgmGo = new GameObject("BGMSource");
            bgmGo.transform.SetParent(transform);
            bgmSource = bgmGo.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (seSource == null)
        {
            var seGo = new GameObject("SESource");
            seGo.transform.SetParent(transform);
            seSource = seGo.AddComponent<AudioSource>();
            seSource.loop = false;
            seSource.playOnAwake = false;
        }

        LoadSettings();
    }

    // ---- BGM 再生 ----

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM() => bgmSource.Stop();

    /// <summary>シーン名に応じた BGM を再生する</summary>
    public void PlayBGMForScene(string sceneName)
    {
        AudioClip clip = null;
        switch (sceneName)
        {
            case "HomeScene":
            case "StageSelectScene":
            case "CharaSelectScene":
            case "LoginScene":
                clip = bgmHome; break;
            case "GameScene":
                clip = bgmGame; break;
            case "GachaScene":
                clip = bgmGacha; break;
            case "ResultScene":
                clip = bgmResult; break;
        }
        if (clip != null) PlayBGM(clip);
    }

    // ---- SE 再生 ----

    public void PlaySE(AudioClip clip)
    {
        if (clip == null) return;
        seSource.PlayOneShot(clip);
    }

    public void PlayButtonSE() => PlaySE(seButton);
    public void PlayGachaSE()  => PlaySE(seGacha);
    public void PlayUltSE()    => PlaySE(seUlt);

    // ---- 音量 ----

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("BGMVolume", volume);
    }

    public void SetSEVolume(float volume)
    {
        seSource.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SEVolume", volume);
    }

    public float BGMVolume => bgmSource.volume;
    public float SEVolume  => seSource.volume;

    private void LoadSettings()
    {
        bgmSource.volume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        seSource.volume  = PlayerPrefs.GetFloat("SEVolume",  1f);
    }
}
