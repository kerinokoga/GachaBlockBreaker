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

    // UI が購読するイベント
    public System.Action<int, float> OnGaugeChanged; // slotIndex, 0.0〜1.0
    public System.Action<int>        OnUltReady;     // slotIndex（ゲージ満タン通知）

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
        foreach (var cd in selectedChars)
        {
            if (cd == null) continue;
            switch (cd.passiveType)
            {
                case PassiveEffectType.BallSpeedUp:
                    if (ball != null) ball.speed *= cd.passiveValue;
                    break;
                case PassiveEffectType.ExtraDamage:
                    BonusDamage += (int)cd.passiveValue;
                    break;
                case PassiveEffectType.ExtraStock:
                    gm?.AddStock((int)cd.passiveValue);
                    break;
                // UltGaugeBoost はゲージ増加時に参照
            }
        }

        Debug.Log($"[CharacterManager] Init完了 BonusDamage={BonusDamage}");
    }

    /// <summary>
    /// ブロックが破壊されるたびに GameManager から呼ばれる
    /// </summary>
    public void OnBlockDestroyed()
    {
        for (int i = 0; i < 3; i++)
        {
            if (selectedChars[i] == null) continue;

            float boost = 1f;
            if (selectedChars[i].passiveType == PassiveEffectType.UltGaugeBoost)
                boost = selectedChars[i].passiveValue;

            gauges[i] = Mathf.Min(gauges[i] + boost, MaxGauge);
            OnGaugeChanged?.Invoke(i, gauges[i] / MaxGauge);

            if (gauges[i] >= MaxGauge)
                OnUltReady?.Invoke(i);
        }
    }

    /// <summary>
    /// 奥義発動（GameUI の奥義ボタンから呼ばれる）
    /// </summary>
    public void TriggerUltimate(int slotIndex)
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (selectedChars[slotIndex] == null) return;
        if (gauges[slotIndex] < MaxGauge) return;

        var cd = selectedChars[slotIndex];
        gauges[slotIndex] = 0f;
        OnGaugeChanged?.Invoke(slotIndex, 0f);

        switch (cd.ultimateType)
        {
            case UltimateSkillType.SpeedBurst:
                var ball = FindObjectOfType<BallController>();
                if (ball != null) StartCoroutine(SpeedBurstCoroutine(ball, cd.ultimateValue, cd.ultimateDuration));
                break;

            case UltimateSkillType.MassDestroy:
                var blocks = FindObjectsOfType<BlockBase>();
                foreach (var b in blocks)
                    b.TakeDamage((int)cd.ultimateValue);
                break;

            case UltimateSkillType.StockRecover:
                GameManager.Instance.AddStock(1);
                break;

            case UltimateSkillType.BarrierShot:
                barrierActive = true;
                Debug.Log("[CharacterManager] バリア発動");
                break;
        }

        Debug.Log($"[CharacterManager] 奥義発動: {cd.characterName} - {cd.ultimateType}");
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

    private IEnumerator SpeedBurstCoroutine(BallController ball, float multiplier, float duration)
    {
        float original = ball.speed;
        ball.speed = original * multiplier;
        Debug.Log($"[CharacterManager] SpeedBurst開始 speed={ball.speed}");
        yield return new WaitForSeconds(duration);
        if (ball != null) ball.speed = original;
        Debug.Log($"[CharacterManager] SpeedBurst終了 speed={ball.speed}");
    }
}
