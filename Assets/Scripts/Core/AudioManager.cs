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

    [Header("SE クリップ")]
    public AudioClip seBlockBreak;
    public AudioClip seMiss;
    public AudioClip seClear;
    public AudioClip seGameOver;
    public AudioClip seBallLaunch;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    // ---- 再生 ----

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM() => bgmSource.Stop();

    public void PlaySE(AudioClip clip)
    {
        if (clip == null) return;
        seSource.PlayOneShot(clip);
    }

    // ---- 音量 ----

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
        PlayerPrefs.SetFloat("BGMVolume", volume);
    }

    public void SetSEVolume(float volume)
    {
        seSource.volume = volume;
        PlayerPrefs.SetFloat("SEVolume", volume);
    }

    private void LoadSettings()
    {
        bgmSource.volume = PlayerPrefs.GetFloat("BGMVolume", 1f);
        seSource.volume = PlayerPrefs.GetFloat("SEVolume", 1f);
    }
}
