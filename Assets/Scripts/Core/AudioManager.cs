using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// BGM / SE の再生・音量管理を一元化するシングルトン
/// DontDestroyOnLoad でシーン間を通して存在し続ける
/// 全ボタンに自動でボタンSEを付与する（ButtonSE コンポーネント方式）
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("AudioSource")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioSource voiceSource;

    [Header("BGM クリップ（Inspector で設定）")]
    public AudioClip bgmHome;
    public AudioClip bgmGame;
    public AudioClip bgmGameBoss;   // ボス戦BGM（未設定時はbgmGame使用）
    public AudioClip bgmGacha;
    public AudioClip bgmResult;
    public AudioClip bgmCollection; // コレクション/ショップ/ランキング（未設定時はbgmHome使用）

    [Header("SE クリップ（Inspector で設定）")]
    public AudioClip seBlockBreak;
    public AudioClip seMiss;
    public AudioClip seClear;
    public AudioClip seGameOver;
    public AudioClip seBallLaunch;
    public AudioClip seButton;
    public AudioClip seGacha;
    public AudioClip seUlt;
    public AudioClip seUltReady;     // 奥義ゲージ満タン時
    public AudioClip sePaddleHit;    // パドルにボールが当たった時
    public AudioClip seDanger;       // 裏ステージ突入時の危険演出SE

    // ステージ別BGM（Resources から自動読み込み）
    private AudioClip[] stageBGMs = new AudioClip[20];

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
        if (voiceSource == null)
        {
            var voiceGo = new GameObject("VoiceSource");
            voiceGo.transform.SetParent(transform);
            voiceSource = voiceGo.AddComponent<AudioSource>();
            voiceSource.loop = false;
            voiceSource.playOnAwake = false;
        }

        // AudioListener がシーンに無ければ自身に追加
        if (FindObjectOfType<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        LoadSettings();
        LoadSEFromResources();
        LoadBGMFromResources();

        // シーンロード時にボタンSE付与
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // AudioListener が無ければ自身に追加（シーン遷移でカメラが変わる場合の対策）
        var listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length == 0)
            gameObject.AddComponent<AudioListener>();
        else if (listeners.Length > 1)
        {
            // 重複があれば自分以外を残して自分のを削除（または逆）
            foreach (var l in listeners)
                if (l.gameObject == gameObject && listeners.Length > 1)
                    Destroy(l);
        }

        // シーンロード直後 + 少し遅延してスキャン（動的生成ボタン対応）
        StartCoroutine(AttachButtonSEDelayed());
    }

    IEnumerator AttachButtonSEDelayed()
    {
        // 即座にスキャン
        AttachButtonSEToAll();
        // 1フレーム後（Start() で生成されるボタン対応）
        yield return null;
        AttachButtonSEToAll();
        // さらに少し待って（遅延生成ボタン対応）
        yield return new WaitForSeconds(0.1f);
        AttachButtonSEToAll();
        yield return new WaitForSeconds(0.3f);
        AttachButtonSEToAll();
        yield return new WaitForSeconds(0.5f);
        AttachButtonSEToAll();
    }

    /// <summary>毎フレーム新しいボタンを検出してSEを付与</summary>
    void LateUpdate()
    {
        AttachButtonSEToAll();
    }

    /// <summary>シーン内の全 Button にSEリスナーを即座に付与</summary>
    void AttachButtonSEToAll()
    {
        // アクティブなボタン
        var activeButtons = FindObjectsOfType<Button>();
        foreach (var btn in activeButtons)
        {
            if (btn.GetComponent<ButtonSE>() == null)
            {
                btn.gameObject.AddComponent<ButtonSE>();
                btn.onClick.AddListener(PlayButtonSE);
            }
        }

        // 非アクティブなボタンも含めて検索（Canvas 経由）
        var allCanvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in allCanvases)
        {
            var buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.GetComponent<ButtonSE>() == null)
                {
                    btn.gameObject.AddComponent<ButtonSE>();
                    btn.onClick.AddListener(PlayButtonSE);
                }
            }
        }

        // DontDestroyOnLoad 以外のルートオブジェクトも走査
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.GetComponent<ButtonSE>() == null)
                {
                    btn.gameObject.AddComponent<ButtonSE>();
                    btn.onClick.AddListener(PlayButtonSE);
                }
            }
        }
    }

    /// <summary>Resources/Audio/SE/ から SE クリップを自動読み込み</summary>
    void LoadSEFromResources()
    {
        if (seBlockBreak == null) seBlockBreak = Resources.Load<AudioClip>("Audio/SE/blockbreak");
        if (seBallLaunch == null) seBallLaunch = Resources.Load<AudioClip>("Audio/SE/ballstart");
        if (seMiss       == null) seMiss       = Resources.Load<AudioClip>("Audio/SE/miss");
        if (seGameOver   == null) seGameOver   = Resources.Load<AudioClip>("Audio/SE/gameover");
        if (seClear      == null) seClear      = Resources.Load<AudioClip>("Audio/SE/clear");
        if (seGacha      == null) seGacha      = Resources.Load<AudioClip>("Audio/SE/gacha");
        if (seButton     == null) seButton     = Resources.Load<AudioClip>("Audio/SE/bottan");
        if (seUlt        == null) seUlt        = Resources.Load<AudioClip>("Audio/SE/ougi");
        if (seUltReady   == null) seUltReady   = Resources.Load<AudioClip>("Audio/SE/ultcharge");
        if (sePaddleHit  == null) sePaddleHit  = Resources.Load<AudioClip>("Audio/SE/paddlehit");
        if (seDanger     == null) seDanger     = Resources.Load<AudioClip>("Audio/SE/danger");

        // デバッグ: SE読み込み結果
        Debug.Log($"[AudioManager] SE読込: button={seButton != null}, gacha={seGacha != null}, " +
                  $"blockBreak={seBlockBreak != null}, miss={seMiss != null}, clear={seClear != null}, " +
                  $"gameOver={seGameOver != null}, ballLaunch={seBallLaunch != null}, " +
                  $"ult={seUlt != null}, ultReady={seUltReady != null}, paddleHit={sePaddleHit != null}");
    }

    /// <summary>Resources/Audio/BGM/ から BGM クリップを自動読み込み</summary>
    void LoadBGMFromResources()
    {
        if (bgmHome == null) bgmHome = Resources.Load<AudioClip>("Audio/BGM/bgm_home");

        // ステージ別BGM（1〜20）
        for (int i = 0; i < 20; i++)
        {
            if (stageBGMs[i] == null)
            {
                string path = $"Audio/BGM/bgm_stage_{(i + 1):D2}";
                stageBGMs[i] = Resources.Load<AudioClip>(path);
            }
        }

        Debug.Log($"[AudioManager] BGM読込: home={bgmHome != null}, stage={stageBGMs[0] != null}〜{stageBGMs[19] != null}");
    }

    // ---- BGM 再生 ----

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.pitch = 1f; // 新規再生時はピッチをリセット
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
        bgmSource.pitch = 1f; // 停止時もピッチを復帰
        SetBGMBoost(false);   // ステージ用増幅も解除
    }

    /// <summary>BGM のピッチ（速度）を変更する。裏ステージなどの緊迫演出に使用。</summary>
    public void SetBGMPitch(float pitch)
    {
        if (bgmSource != null)
            bgmSource.pitch = Mathf.Clamp(pitch, 0.5f, 2.0f);
    }

    /// <summary>シーン名に応じた BGM を再生する</summary>
    public void PlayBGMForScene(string sceneName)
    {
        AudioClip clip = null;
        switch (sceneName)
        {
            case "HomeScene":
            case "StageSelectScene":
            case "CharaSelectScene":
            case "CharaManageScene":
            case "LoginScene":
                clip = bgmHome; break;
            case "GameScene":
                clip = bgmGame; break;
            case "GachaScene":
                clip = bgmGacha != null ? bgmGacha : bgmHome; break;
            case "ResultScene":
                clip = bgmResult != null ? bgmResult : bgmHome; break;
            case "CollectionScene":
            case "ShopScene":
            case "RankingScene":
                clip = bgmCollection != null ? bgmCollection : bgmHome; break;
        }
        // ゲームシーンのみBGM増幅、それ以外は等倍に戻す
        SetBGMBoost(sceneName == "GameScene");
        if (clip != null) PlayBGM(clip);
    }

    /// <summary>ステージ番号に応じたBGMを再生する（全ステージボス兼用）</summary>
    public void PlayStageBGM(int stageNumber)
    {
        int idx = Mathf.Clamp(stageNumber - 1, 0, 19);
        AudioClip clip = stageBGMs[idx];
        // 個別BGMが無ければ汎用ゲームBGM
        if (clip == null) clip = bgmGame;
        SetBGMBoost(true); // SEにかき消されないようステージ中は増幅
        if (clip != null) PlayBGM(clip);
    }

    /// <summary>ボス戦BGMに切り替える（後方互換）</summary>
    public void PlayBossBGM()
    {
        // ステージ別BGMが既に再生中ならそのまま（ボス兼用のため）
        // 個別指定がある場合のみ切り替え
        if (bgmGameBoss != null) PlayBGM(bgmGameBoss);
    }

    // ---- SE 再生 ----

    public void PlaySE(AudioClip clip)
    {
        if (clip == null) return;
        seSource.PlayOneShot(clip);
    }

    public void PlayButtonSE()   => PlaySE(seButton);
    public void PlayGachaSE()    => PlaySE(seGacha);
    public void PlayUltSE()      => PlaySE(seUlt);
    public void PlayUltReadySE() => PlaySE(seUltReady);
    public void PlayPaddleHitSE() => PlaySE(sePaddleHit);

    /// <summary>裏ステージ突入時の危険演出SE。未設定時は seMiss で代替。</summary>
    public void PlayDangerSE()    => PlaySE(seDanger != null ? seDanger : seMiss);

    // ---- ボイス再生 ----

    /// <summary>ボイスの優先度。高優先度が再生中なら低優先度はスキップされる。</summary>
    public enum VoicePriority
    {
        Low  = 0,  // 被弾ボイス等、頻発する低優先度
        Mid  = 1,  // ステージ開始 / キャラ選択 / 攻撃 / 破壊率達成等の通常ボイス
        High = 2,  // Ult発動 / クリア / ゲームオーバー等、絶対に最後まで聞かせたいボイス
    }

    private VoicePriority currentVoicePriority = VoicePriority.Low;
    private float currentVoiceEndTime = 0f;

    /// <summary>ボイスを再生する（旧API互換、優先度=Mid、音量=1）</summary>
    public void PlayVoice(AudioClip clip)
    {
        PlayVoice(clip, 1f, VoicePriority.Mid);
    }

    /// <summary>ボイスを再生する（旧API互換、優先度=Mid）</summary>
    public void PlayVoice(AudioClip clip, float volumeScale)
    {
        PlayVoice(clip, volumeScale, VoicePriority.Mid);
    }

    /// <summary>
    /// ボイスを再生する（音量倍率＋優先度指定）。
    /// 動作仕様:
    ///   ・高優先度が再生中 → 低優先度はスキップ（保護）
    ///   ・低/中優先度が再生中に高優先度 → Stop() で中断して再生（割り込み）
    ///   ・同優先度同士     → Stop() せずオーバーレイ再生（両方鳴る）
    /// CharacterData.voiceVolumeMultiplier を volumeScale に渡すことでキャラ別補正可能。
    /// </summary>
    /// <summary>全ボイス共通の底上げ倍率（BGMに対して聞き取りやすくする）</summary>
    const float VoiceBoost = 2.0f;

    public void PlayVoice(AudioClip clip, float volumeScale, VoicePriority priority)
    {
        if (clip == null || voiceSource == null) return;

        // unscaledTime で経過判定（ポーズ中でも正しく測定）
        bool isCurrentStillPlaying = Time.unscaledTime < currentVoiceEndTime;

        // (1) 高優先度が再生中なら低優先度はスキップ
        if (isCurrentStillPlaying && priority < currentVoicePriority)
            return;

        // (2) 高優先度が低優先度を割り込む場合のみ Stop で中断
        //     同優先度の時は Stop しない → PlayOneShot がオーバーレイ再生される
        bool isHigherInterrupting = isCurrentStillPlaying && priority > currentVoicePriority;
        if (isHigherInterrupting)
        {
            voiceSource.Stop();
        }

        voiceSource.PlayOneShot(clip, Mathf.Clamp(volumeScale * VoiceBoost, 0.1f, 3f));

        // 状態更新：再生中の最大優先度＋最遅終了時刻を保持
        float newEndTime = Time.unscaledTime + (clip.length > 0f ? clip.length : 1f);
        if (priority > currentVoicePriority || !isCurrentStillPlaying)
        {
            // 上書き or 何も鳴っていなかった
            currentVoicePriority = priority;
            currentVoiceEndTime  = newEndTime;
        }
        else
        {
            // 同優先度オーバーレイ：終了時刻を遅い方に延長（保護期間を拡張）
            if (newEndTime > currentVoiceEndTime)
                currentVoiceEndTime = newEndTime;
        }
    }

    public void SetVoiceVolume(float volume)
    {
        if (voiceSource == null) return;
        voiceSource.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("VoiceVolume", volume);
    }

    public float VoiceVolume => voiceSource != null ? voiceSource.volume : 1f;

    // ---- 音量 ----

    // ユーザー設定の音量とシーン別補正を分離して管理する。
    // ステージ中は効果音の連打でBGMがかき消されるため、ゲームBGMだけ内部的に増幅する。
    float bgmUserVolume = 0.25f;
    float bgmSceneBoost = 1f;
    const float GameBgmBoost = 1.6f; // ステージBGMの増幅率（約+4dB相当）

    void ApplyBGMVolume()
    {
        if (bgmSource != null)
            bgmSource.volume = Mathf.Clamp01(bgmUserVolume * bgmSceneBoost);
    }

    /// <summary>ステージBGM用の増幅ON/OFF（PlayStageBGM/PlayBGMForScene から呼ばれる）</summary>
    void SetBGMBoost(bool gameScene)
    {
        bgmSceneBoost = gameScene ? GameBgmBoost : 1f;
        ApplyBGMVolume();
    }

    public void SetBGMVolume(float volume)
    {
        bgmUserVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("BGMVolume", bgmUserVolume);
        ApplyBGMVolume();
    }

    public void SetSEVolume(float volume)
    {
        seSource.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SEVolume", volume);
    }

    public float BGMVolume => bgmUserVolume;
    public float SEVolume  => seSource.volume;

    private void LoadSettings()
    {
        // 音量バランス調整(v3): 初期BGMがボイスに対して大きすぎたため既定値を 0.25 に引き下げ。
        // 既にプレイ済みの端末でも一度だけ新既定値を適用する（以降はユーザー設定を尊重）。
        if (!PlayerPrefs.HasKey("AudioDefaultsV3"))
        {
            PlayerPrefs.SetFloat("BGMVolume", 0.25f);
            PlayerPrefs.SetInt("AudioDefaultsV3", 1);
            PlayerPrefs.Save();
        }

        bgmUserVolume = PlayerPrefs.GetFloat("BGMVolume", 0.25f);
        ApplyBGMVolume();
        seSource.volume  = PlayerPrefs.GetFloat("SEVolume",  1f);
        if (voiceSource != null)
            voiceSource.volume = PlayerPrefs.GetFloat("VoiceVolume", 1f);
    }
}
