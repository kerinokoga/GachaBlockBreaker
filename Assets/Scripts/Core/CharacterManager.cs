using UnityEngine;
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
    public float UltDamageMultiplier { get; private set; } = 1f;

    // Penetrate 奥義中フラグ：BallController から参照される
    public bool IsPenetrating { get; private set; } = false;

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
        UltDamageMultiplier = 1f;
        IsPenetrating = false;
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

        // 奥義ボイス再生（SE は switch 前に再生済み）— High優先度で他ボイスから保護
        if (cd.voiceUlt != null)
            AudioManager.Instance?.PlayVoice(cd.voiceUlt, cd.voiceVolumeMultiplier, AudioManager.VoicePriority.High);

        Debug.Log($"[CharacterManager] 奥義発動: {cd.characterName} - {cd.ultimateType}");

        // チュートリアル用に発動を通知
        OnUltUsed?.Invoke(slotIndex);
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

        UltDamageMultiplier = 1f;

        // 貫通中なら全ボールにも解除を反映
        if (IsPenetrating)
        {
            foreach (var ball in FindObjectsOfType<BallController>())
                ball.SetPenetrate(false);
        }
        IsPenetrating = false;

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
        UltDamageMultiplier = multiplier;
        Debug.Log($"[CharacterManager] PowerBurst開始 倍率={multiplier}");
        yield return new WaitForSeconds(duration);
        UltDamageMultiplier = 1f;
        Debug.Log("[CharacterManager] PowerBurst終了");
    }

    private IEnumerator PenetrateCoroutine(float duration)
    {
        IsPenetrating = true;
        // 全ボールに貫通を適用（分裂ボール含む）
        foreach (var ball in FindObjectsOfType<BallController>())
            ball.SetPenetrate(true);
        // Ready状態なら発射まで待つ（待機中は時間消費しない）
        yield return StartCoroutine(WaitForPlaying());
        Debug.Log($"[CharacterManager] 貫通開始 {duration}秒");
        yield return new WaitForSeconds(duration);
        IsPenetrating = false;
        // 全ボールの貫通を解除
        foreach (var ball in FindObjectsOfType<BallController>())
            ball.SetPenetrate(false);
        Debug.Log("[CharacterManager] 貫通終了");
    }
}
