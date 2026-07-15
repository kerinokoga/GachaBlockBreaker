using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// キャラクター管理：パッシブ効果適用・奥義ゲージ・奥義発動
/// GameScene に配置するシングルトン（DontDestroyOnLoad しない）
/// </summary>
public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    private const int MaxGauge = 10;

    private CharacterData[] selectedChars = new CharacterData[3];
    private float[] gauges = new float[3];
    private bool barrierActive = false;

    // ExtraDamage パッシブ用：BlockBase から参照される
    public int BonusDamage { get; private set; } = 0;

    // 3キャラのパワー合計（基本ヒットダメージ）
    public float BasePower { get; private set; } = 1f;

    // BallDamageUp パッシブ倍率：BlockBase から参照される（1.0 = 等倍）
    public float PassiveDamageMultiplier { get; private set; } = 1f;

    // PowerBurst 奥義中の一時的ダメージ倍率（1.0 = 等倍）
    // 複数発動時は「増加分」を加算する方式（×2 と ×2.5 → 1 + 1 + 1.5 = ×3.5）
    public float UltDamageMultiplier => 1f + ultBurstBonus;
    float ultBurstBonus = 0f; // 発動中の PowerBurst の増加分（倍率-1）の合計

    // Penetrate 奥義中フラグ：BallController から参照される
    // 複数回発動（重ね掛け）に対応するため発動中の本数をカウントし、
    // 最後の1本が切れたときだけ解除する（先に発動した方の時間切れに巻き込まれない）
    public bool IsPenetrating => penetrateCasts > 0;
    int penetrateCasts = 0;

    // CriticalRangeUp パッシブ：クリティカル判定範囲の追加値（デフォルト0.03に加算）
    public float CriticalRangeBonus { get; private set; } = 0f;

    /// <summary>現在のダメージ値を計算して返す（UI表示用）</summary>
    public int GetCurrentDamage()
    {
        float baseDmg = BasePower + BonusDamage;
        float mul = PassiveDamageMultiplier * UltDamageMultiplier;
        return (int)System.Math.Ceiling(baseDmg * mul);
    }

    // UI が購読するイベント
    public System.Action<int, float> OnGaugeChanged; // slotIndex, 0.0〜1.0
    public System.Action<int>        OnUltReady;     // slotIndex（ゲージ満タン通知）
    public System.Action<int>        OnUltUsed;      // slotIndex（奥義発動通知、チュートリアル用）

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// ResultData からキャラデータを復元してパッシブを適用する
    /// GameManager.Start から呼ばれる
    /// </summary>
    public void Initialize(BallController ball, GameManager gm)
    {
        // ResultData の名前からアセットを復元
        var all = Resources.LoadAll<CharacterData>("Characters");
        for (int i = 0; i < 3; i++)
        {
            selectedChars[i] = null;
            if (i < ResultData.SelectedCharacterNames.Length)
            {
                string charName = ResultData.SelectedCharacterNames[i];
                foreach (var cd in all)
                    if (cd.characterName == charName) { selectedChars[i] = cd; break; }
            }
        }

        // パッシブ適用
        BonusDamage = 0;
        PassiveDamageMultiplier = 1f;
        ultBurstBonus = 0f;
        penetrateCasts = 0;
        CriticalRangeBonus = 0f;
        foreach (var cd in selectedChars)
        {
            if (cd == null) continue;
            // パッシブ1を適用
            ApplyPassive(cd.passiveType, cd.passiveValue, gm);
            // パッシブ2を適用（複合パッシブ対応）
            if (cd.passiveType2 != PassiveEffectType.None)
                ApplyPassive(cd.passiveType2, cd.passiveValue2, gm);
        }

        // 基本パワー計算（3キャラそれぞれのパワーを合計）
        float powerSum = 0f;
        for (int i = 0; i < 3; i++)
        {
            if (selectedChars[i] == null) continue;
            string cName = selectedChars[i].characterName;
            int lvl = OrbManager.GetEnhanceLevel(cName);
            bool awakened = OrbManager.IsAwakened(cName);
            // 各キャラのパワー = 1 + 0.2×Lv + (覚醒なら+0.5)
            powerSum += 1f + 0.2f * lvl + (awakened ? OrbManager.AwakenBonusMultiplier : 0f);
        }
        BasePower = Mathf.Max(powerSum, 1f); // 最低1

        Debug.Log($"[CharacterManager] Init完了 BasePower={BasePower:F1} BonusDamage={BonusDamage} CriticalRangeBonus={CriticalRangeBonus:F2}");
    }

    private void ApplyPassive(PassiveEffectType type, float value, GameManager gm)
    {
        switch (type)
        {
            case PassiveEffectType.BallDamageUp:
                PassiveDamageMultiplier *= value;
                break;
            case PassiveEffectType.ExtraDamage:
                BonusDamage += (int)value;
                break;
            case PassiveEffectType.ExtraStock:
                gm?.AddStock((int)value);
                break;
            case PassiveEffectType.CriticalRangeUp:
                CriticalRangeBonus += value / 100f; // 3% → 0.03
                break;
            // UltGaugeBoost はゲージ増加時に参照
        }
    }

    /// <summary>指定スロットの装備キャラを返す（未装備なら null）。UI 表示用。</summary>
    public CharacterData GetSelectedChar(int slot)
    {
        if (slot < 0 || slot >= 3) return null;
        return selectedChars[slot];
    }

    /// <summary>
    /// 指定キャラ名が装備されているスロット番号を返す。見つからなければ -1。
    /// チュートリアル等で特定キャラを参照したい場合に利用。
    /// </summary>
    public int FindSlotByCharacterName(string characterName)
    {
        if (string.IsNullOrEmpty(characterName)) return -1;
        for (int i = 0; i < 3; i++)
        {
            if (selectedChars[i] != null
                && selectedChars[i].characterName == characterName)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 指定スロットの奥義ゲージを強制的に満タンにする（OnUltReady も発火）。
    /// チュートリアル用：アリアの奥義（MassDestroy）を視覚デモするために使用。
    /// </summary>
    public void ForceFillGauge(int slot)
    {
        if (slot < 0 || slot >= 3) return;
        if (selectedChars[slot] == null) return;
        gauges[slot] = MaxGauge;
        OnGaugeChanged?.Invoke(slot, 1f);
        OnUltReady?.Invoke(slot);
        AudioManager.Instance?.PlayUltReadySE();
    }

    /// <summary>
    /// ブロックが破壊されるたびに GameManager から呼ばれる
    /// </summary>
    public void OnBlockDestroyed()
    {
        AddGauge(1f);
    }

    /// <summary>
    /// ブロックにダメージを与えるたびに呼ばれる（破壊に至らないヒットでも溜まる）。
    /// 高HPブロックやボスだけが残った状況でも奥義ゲージが回るようにする。
    /// </summary>
    public void OnBlockHit()
    {
        AddGauge(GaugePerHit);
    }

    /// <summary>ヒット1回あたりのゲージ増加量（破壊は 1.0）</summary>
    const float GaugePerHit = 0.25f;

    /// <summary>ゲージ加算の共通処理（UltGaugeBoost パッシブは加算量に乗算）</summary>
    void AddGauge(float baseAmount)
    {
        for (int i = 0; i < 3; i++)
        {
            if (selectedChars[i] == null) continue;

            if (gauges[i] >= MaxGauge) continue; // 既にMAXならスキップ

            float boost = 1f;
            if (selectedChars[i].passiveType == PassiveEffectType.UltGaugeBoost)
                boost = selectedChars[i].passiveValue;
            else if (selectedChars[i].passiveType2 == PassiveEffectType.UltGaugeBoost)
                boost = selectedChars[i].passiveValue2;

            gauges[i] = Mathf.Min(gauges[i] + baseAmount * boost, MaxGauge);
            OnGaugeChanged?.Invoke(i, gauges[i] / MaxGauge);

            if (gauges[i] >= MaxGauge)
            {
                OnUltReady?.Invoke(i);
                AudioManager.Instance?.PlayUltReadySE();
            }
        }
    }

    /// <summary>
    /// 奥義発動（GameUI の奥義ボタンから呼ばれる）
    /// </summary>
    public void TriggerUltimate(int slotIndex)
    {
        if (GameManager.Instance == null) return;
        var state = GameManager.Instance.CurrentState;
        if (state != GameManager.GameState.Playing && state != GameManager.GameState.Ready) return;
        if (selectedChars[slotIndex] == null) return;
        if (gauges[slotIndex] < MaxGauge) return;

        var cd = selectedChars[slotIndex];
        gauges[slotIndex] = 0f;
        OnGaugeChanged?.Invoke(slotIndex, 0f);
        AudioManager.Instance?.PlayUltSE();

        // 奥義ボイスは発動の瞬間に再生（アニメ中も聞こえるように）— High優先度で他ボイスから保護
        if (cd.voiceUlt != null)
            AudioManager.Instance?.PlayVoice(cd.voiceUlt, cd.voiceVolumeMultiplier, AudioManager.VoicePriority.High);

        // 奥義アニメ（ON かつ 動画がある場合のみ）：ゲームを一時停止して再生後に効果適用
        var animClip = UltAnimationManager.Enabled ? UltAnimationManager.GetClipFor(cd) : null;
        if (animClip != null)
        {
            StartCoroutine(PlayUltAnimationThenApply(cd, slotIndex, animClip));
            return;
        }

        ApplyUltimate(cd, slotIndex);
    }

    /// <summary>奥義効果の適用本体（アニメなし時は即時、アニメあり時は再生後に呼ばれる）</summary>
    void ApplyUltimate(CharacterData cd, int slotIndex)
    {
        switch (cd.ultimateType)
        {
            case UltimateSkillType.PowerBurst:
                StartCoroutine(PowerBurstCoroutine(cd.ultimateValue, cd.ultimateDuration));
                break;

            case UltimateSkillType.MassDestroy:
                var blocks = FindObjectsOfType<BlockBase>();
                foreach (var b in blocks)
                    b.TakeDamage((int)cd.ultimateValue);
                break;

            case UltimateSkillType.StockRecover:
                GameManager.Instance.AddStock((int)cd.ultimateValue);
                break;

            case UltimateSkillType.BarrierShot:
                barrierActive = true;
                Debug.Log("[CharacterManager] バリア発動");
                break;

            case UltimateSkillType.Penetrate:
                StartCoroutine(PenetrateCoroutine(cd.ultimateDuration));
                break;

            case UltimateSkillType.BallSplit:
                GameManager.Instance?.TriggerBallSplit();
                break;
        }

        Debug.Log($"[CharacterManager] 奥義発動: {cd.characterName} - {cd.ultimateType}");

        // チュートリアル用に発動を通知
        OnUltUsed?.Invoke(slotIndex);
    }

    /// <summary>
    /// 奥義アニメーション再生コルーチン。
    /// ゲームを一時停止（Paused 中はボール落下がミス扱いされない）→ 全画面で動画再生
    /// → タップ or 再生終了で復帰し、奥義効果を適用する。
    /// </summary>
    IEnumerator PlayUltAnimationThenApply(CharacterData cd, int slotIndex, VideoClip clip)
    {
        // Playing 中のみポーズ（Ready 中はボールが動いていないのでそのまま）
        bool paused = false;
        if (GameManager.Instance != null
            && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            GameManager.Instance.Pause();
            paused = true;
        }

        // 専用オーバーレイ Canvas（既存UIより手前・入力もここで受ける）
        var canvasGo = new GameObject("UltAnimCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        canvasGo.AddComponent<GraphicRaycaster>();

        // 黒背景（全画面。縦長端末での黒帯＋タップスキップの判定を兼ねる）
        var bgGo = new GameObject("MovieBg");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = Color.black;
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        bool skipped = false;
        var skipBtn = bgGo.AddComponent<Button>();
        skipBtn.transition = Selectable.Transition.None;
        skipBtn.onClick.AddListener(() => skipped = true);

        // 動画表示面（アスペクト比を維持。9:16より縦長の画面では上下が黒帯になる）
        var movieGo = new GameObject("Movie");
        movieGo.transform.SetParent(canvasGo.transform, false);
        var raw = movieGo.AddComponent<RawImage>();
        raw.color = Color.black;
        raw.raycastTarget = false;
        var rrt = movieGo.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = rrt.offsetMax = Vector2.zero;
        var fitter = movieGo.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = (float)clip.width / clip.height;

        // スキップ案内
        var hintGo = new GameObject("SkipHint");
        hintGo.transform.SetParent(canvasGo.transform, false);
        var hint = hintGo.AddComponent<Text>();
        hint.text = "タップでスキップ";
        hint.fontSize = 26;
        hint.color = new Color(1f, 1f, 1f, 0.6f);
        hint.alignment = TextAnchor.MiddleCenter;
        hint.font = UIFont.Main; hint.verticalOverflow = VerticalWrapMode.Overflow;
        hint.raycastTarget = false;
        var hrt = hintGo.GetComponent<RectTransform>();
        hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.05f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(400f, 40f);

        var rtex = new RenderTexture(720, 1280, 0);
        var vp = canvasGo.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rtex;
        vp.audioOutputMode = VideoAudioOutputMode.None;
        vp.isLooping = false;
        vp.clip = clip;
        vp.Prepare();

        // 準備待ち（保険で5秒タイムアウト。timeScale=0 でも進むよう unscaled 時間）
        float pt = 0f;
        while (!vp.isPrepared && pt < 5f && !skipped)
        {
            pt += Time.unscaledDeltaTime;
            yield return null;
        }

        if (vp.isPrepared && !skipped)
        {
            raw.texture = rtex;
            raw.color = Color.white;

            bool ended = false;
            VideoPlayer.EventHandler onEnd = _ => ended = true;
            vp.loopPointReached += onEnd;
            vp.Play();
            while (!ended && !skipped) yield return null;
            vp.loopPointReached -= onEnd;
            vp.Stop();
        }

        Destroy(canvasGo);
        rtex.Release();
        Destroy(rtex);

        if (paused)
        {
            GameManager.Instance?.Resume();
            // スロー再開: アニメで目を離した後にボールを再捕捉できるよう、
            // 0.3倍速から約1秒かけて通常速度へ戻す
            StartCoroutine(SlowMotionResume(0.3f, 1.0f));
        }
        ApplyUltimate(cd, slotIndex);
    }

    /// <summary>
    /// 奥義アニメ後のスロー再開。startScale 倍速から duration 秒（実時間）で通常速度へ補間する。
    /// 途中でポーズ等の状態変化があったら介入をやめる（Paused 以外なら等倍に戻してから抜ける）。
    /// </summary>
    IEnumerator SlowMotionResume(float startScale, float duration)
    {
        Time.timeScale = startScale;
        float t = 0f;
        while (t < duration)
        {
            if (GameManager.Instance == null
                || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                // ポーズなら timeScale はポーズ側の管理に任せる。それ以外（ミス等）は等倍に戻す
                if (GameManager.Instance == null
                    || GameManager.Instance.CurrentState != GameManager.GameState.Paused)
                    Time.timeScale = 1f;
                yield break;
            }
            t += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(startScale, 1f, t / duration);
            yield return null;
        }
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 裏ステージ突入時などに、奥義ゲージ以外の一時効果をリセットする。
    /// - 進行中の PowerBurst / Penetrate コルーチンを停止
    /// - UltDamageMultiplier / IsPenetrating / barrierActive をリセット
    /// - gauges[]（奥義ゲージ）は維持
    /// - パッシブ由来の値（BonusDamage / BasePower / PassiveDamageMultiplier / CriticalRangeBonus）も維持
    /// </summary>
    public void ResetTemporaryEffects()
    {
        // PowerBurst / Penetrate などの進行中コルーチンを全停止
        StopAllCoroutines();

        ultBurstBonus = 0f;

        // 貫通中なら全ボールにも解除を反映
        if (IsPenetrating)
        {
            foreach (var ball in FindObjectsOfType<BallController>())
                ball.SetPenetrate(false);
        }
        penetrateCasts = 0;

        barrierActive = false;

        Debug.Log("[CharacterManager] 一時効果をリセット（奥義ゲージは維持）");
    }

    /// <summary>
    /// BarrierShot: ミス時に呼ばれる。バリアがあれば消費して true を返す
    /// </summary>
    public bool ConsumeBarrier()
    {
        if (!barrierActive) return false;
        barrierActive = false;
        Debug.Log("[CharacterManager] バリア消費");
        return true;
    }

    public CharacterData GetSelected(int slot) =>
        (slot >= 0 && slot < 3) ? selectedChars[slot] : null;

    public CharacterData[] GetSelectedCharacters() => selectedChars;

    /// <summary>Ready状態ならPlaying状態になるまで待機</summary>
    private System.Collections.IEnumerator WaitForPlaying()
    {
        while (GameManager.Instance != null &&
               GameManager.Instance.CurrentState == GameManager.GameState.Ready)
        {
            yield return null;
        }
    }

    private IEnumerator PowerBurstCoroutine(float multiplier, float duration)
    {
        // Ready状態なら発射まで待つ（待機中は時間消費しない）
        yield return StartCoroutine(WaitForPlaying());

        // 加算スタック方式: 複数の PowerBurst が重なったら「増加分（倍率-1）」を足し合わせる
        // （例: ×2 と ×2.5 の同時発動 = 1 + 1 + 1.5 = ×3.5。時間切れは各自が自分の増加分だけ引く）
        float bonus = multiplier - 1f;
        ultBurstBonus += bonus;
        Debug.Log($"[CharacterManager] PowerBurst開始 倍率={multiplier} 合計倍率={UltDamageMultiplier}");
        yield return new WaitForSeconds(duration);
        ultBurstBonus = Mathf.Max(0f, ultBurstBonus - bonus);
        Debug.Log($"[CharacterManager] PowerBurst終了 合計倍率={UltDamageMultiplier}");
    }

    private IEnumerator PenetrateCoroutine(float duration)
    {
        penetrateCasts++;
        // 全ボールに貫通を適用（分裂ボール含む）
        foreach (var ball in FindObjectsOfType<BallController>())
            ball.SetPenetrate(true);
        // Ready状態なら発射まで待つ（待機中は時間消費しない）
        yield return StartCoroutine(WaitForPlaying());
        Debug.Log($"[CharacterManager] 貫通開始 {duration}秒 発動中={penetrateCasts}本");
        yield return new WaitForSeconds(duration);

        penetrateCasts = Mathf.Max(0, penetrateCasts - 1);
        // 他の貫通がまだ発動中なら解除しない（重ね掛けの巻き添え防止）
        if (penetrateCasts > 0)
        {
            Debug.Log($"[CharacterManager] 貫通1本終了（残り{penetrateCasts}本は継続）");
            yield break;
        }

        // 全ボールの貫通を解除
        // ただしクリティカル中のボールはスキップ（クリティカル由来の貫通は
        // 「次のパドルヒットまで」が仕様のため、奥義の時間切れに巻き込まない）
        foreach (var ball in FindObjectsOfType<BallController>())
            if (!ball.IsCritical) ball.SetPenetrate(false);
        Debug.Log("[CharacterManager] 貫通終了");
    }
}
