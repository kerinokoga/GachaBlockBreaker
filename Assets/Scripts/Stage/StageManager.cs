using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ステージのブロック生成・破壊数追跡・破壊率計算を担う
/// ボスブロックの攻撃処理（パドル縮小・ブロック降下）も管理
/// </summary>
public class StageManager : MonoBehaviour
{
    [Header("ステージデータ")]
    [SerializeField] private StageData stageData;

    [Header("ブロック Prefab（0:Normal 1:Durable 2:Explosion 3:Chain 4:Speed 5:Boss）")]
    [SerializeField] private GameObject[] blockPrefabs;

    [Header("ブロックの親 Transform")]
    [SerializeField] private Transform blockParent;

    private int totalBlockCount = 0;
    private int destroyedBlockCount = 0;

    // ボス攻撃用の参照（GameManager が設定する）
    [HideInInspector] public PaddleController paddleRef;
    private float originalPaddleScaleX = -1f;

    // 裏ステージ状態（Boss 復活後）
    private bool isTrueStage = false;
    public bool IsTrueStage => isTrueStage;

    // ボスターンカウント
    private BossBlock activeBoss;
    private CharacterData bossCharData; // ボスキャラのデータ（ボイス再生用）
    private int bossRemainingTurns = 0;

    // ボス被弾ボイスのクールダウン管理（連発抑制）
    private float lastBossDamagedVoiceTime = -100f;
    private const float BOSS_DAMAGED_VOICE_COOLDOWN = 3f; // 秒
    public bool HasBoss => activeBoss != null && activeBoss.IsAlive;
    public BossBlock ActiveBoss => activeBoss;
    public int BossRemainingTurns => bossRemainingTurns;
    public int BossMaxTurns { get; private set; }

    // ボスターン切れ通知（GameManager が購読）
    public System.Action OnBossTurnExpired;
    // ボスターン更新通知（UI更新用）
    public System.Action<int, int> OnBossTurnChanged; // (remaining, max)

    // GameManager が購読して破壊率チェックを行う
    public System.Action OnBlockDestroyedCallback;
    public System.Action<float> OnBossHPRatioChanged; // ボスHP割合 0.0〜1.0

    // ---- 外部公開 ----

    public float GetDestroyRate()
    {
        if (totalBlockCount == 0) return 0f;
        return (float)destroyedBlockCount / totalBlockCount;
    }

    public bool IsAllCleared() => destroyedBlockCount >= totalBlockCount && totalBlockCount > 0;

    // ---- エンドレス用 HP 倍率 ----

    float endlessHPMul = 1f;

    /// <summary>
    /// エンドレスモード用: 以降の BuildStage で耐久ブロック・ボスの HP に掛ける倍率を設定する。
    /// 通常ステージでは 1.0 のまま（GameManager が設定しない限り影響なし）。
    /// </summary>
    public void SetEndlessHPMultiplier(float mul) => endlessHPMul = Mathf.Max(0.1f, mul);

    // ---- エンドレス用 出現キャラ差し替え ----

    StageData charSourceOverride;

    /// <summary>
    /// エンドレスモード用: ボスのアイコン・ボイスを別ステージのキャラに差し替える。
    /// null なら通常どおりレイアウト元ステージのキャラを使用。
    /// </summary>
    public void SetEndlessCharSource(StageData src) => charSourceOverride = src;

    // ---- ステージ構築 ----

    public void BuildStage(StageData data = null)
    {
        if (data != null) stageData = data;
        if (stageData == null)
        {
            Debug.LogWarning("StageData が設定されていません。");
            return;
        }

        // 本ステージ構築（裏フラグは OFF）
        isTrueStage = false;
        BuildStageInternal(stageData.blocks);
    }

    /// <summary>
    /// 裏ステージとして再構築する（Boss 復活仕様）。
    /// - trueBlocks 配置でブロック再生成
    /// - Boss HP は trueBossHPMul 倍
    /// - 攻撃倍率・パドル縮小倍率は HandleBossAttack / ShrinkPaddle で適用
    /// </summary>
    public void RebuildAsTrueStage()
    {
        if (stageData == null || !stageData.hasTrueStage)
        {
            Debug.LogWarning("[TrueStage] hasTrueStage=false のため再構築しません。");
            return;
        }

        isTrueStage = true;
        var placements = (stageData.trueBlocks != null && stageData.trueBlocks.Count > 0)
            ? stageData.trueBlocks : stageData.blocks;
        BuildStageInternal(placements);
        Debug.Log($"[TrueStage] 裏ステージ再構築完了 (Stage {stageData.stageNumber})");
    }

    private void BuildStageInternal(List<BlockPlacementData> placements)
    {
        // 既存ブロックを削除
        foreach (Transform child in blockParent)
            Destroy(child.gameObject);

        totalBlockCount = 0;
        destroyedBlockCount = 0;
        activeBoss = null;

        // ---- 通常ブロックの列範囲を求めて左右中央揃えにする ----
        int minCol = int.MaxValue, maxCol = int.MinValue;
        foreach (var b in placements)
        {
            if (b.blockType == BlockType.Boss) continue;
            if (b.gridPosition.x < minCol) minCol = b.gridPosition.x;
            if (b.gridPosition.x > maxCol) maxCol = b.gridPosition.x;
        }
        // グリッド全体の幅を求め、画面中央(x=0)に揃える
        float gridWidth = (maxCol - minCol) * stageData.cellWidth;
        float centerOffsetX = -gridWidth / 2f;

        foreach (var blockData in placements)
        {
            int typeIndex = (int)blockData.blockType;
            if (typeIndex >= blockPrefabs.Length || blockPrefabs[typeIndex] == null)
            {
                Debug.LogWarning($"BlockPrefab[{typeIndex}] が未設定です。スキップします。");
                continue;
            }

            // グリッド座標 → ワールド座標に変換（左右中央揃え）
            Vector2 worldPos = new Vector2(
                centerOffsetX + (blockData.gridPosition.x - minCol) * stageData.cellWidth,
                stageData.originOffset.y - blockData.gridPosition.y * stageData.cellHeight
            );

            // ボスブロックは固定Y座標に配置
            if (blockData.blockType == BlockType.Boss)
            {
                worldPos.y = 5.5f;
            }

            GameObject blockGo = Instantiate(
                blockPrefabs[typeIndex],
                new Vector3(worldPos.x, worldPos.y, 0),
                Quaternion.identity,
                blockParent
            );

            // HP を設定
            BlockBase block = blockGo.GetComponent<BlockBase>();
            if (block != null)
            {
                // DurableBlock は HP を外部から設定できるよう公開している
                if (block is DurableBlock durable)
                    durable.SetHP(Mathf.Max(1, Mathf.RoundToInt(blockData.hp * endlessHPMul)));

                // SpeedBlock は速度倍率を設定
                if (block is SpeedBlock speedBlock)
                    speedBlock.SetSpeedMultiplier(blockData.speedMultiplier);

                // BossBlock は HP + 難度 + 攻撃コールバック + ターン制限設定
                if (block is BossBlock boss)
                {
                    // 裏ステージなら HP を trueBossHPMul 倍にする
                    int bossHP = blockData.hp;
                    if (isTrueStage && stageData.hasTrueStage)
                        bossHP = Mathf.Max(1, Mathf.RoundToInt(bossHP * stageData.trueBossHPMul));
                    // エンドレスのスケーリング倍率を適用
                    bossHP = Mathf.Max(1, Mathf.RoundToInt(bossHP * endlessHPMul));
                    boss.SetHP(bossHP);
                    boss.difficulty = (stageData.stageNumber - 1) / 5 + 1; // 1~4
                    boss.OnBossAttack += HandleBossAttack;

                    // キャラアイコン + ボスキャラデータを設定
                    // （エンドレスでは抽選キャラに差し替え）
                    string bossCharName = charSourceOverride != null
                        ? charSourceOverride.characterName : stageData.characterName;
                    if (!string.IsNullOrEmpty(bossCharName))
                    {
                        var allChars = Resources.LoadAll<CharacterData>("Characters");
                        foreach (var cd in allChars)
                        {
                            if (cd.characterName == bossCharName)
                            {
                                bossCharData = cd;
                                if (cd.icon != null) boss.SetIcon(cd.icon);
                                break;
                            }
                        }
                    }

                    // HP50ごと減少通知
                    boss.OnBossHPMilestone += HandleBossHPMilestone;
                    // HP変動をGameManagerへ中継
                    boss.OnBossHPChanged += (ratio) => OnBossHPRatioChanged?.Invoke(ratio);

                    // ステージごとのターン数（全ステージボス仕様）
                    int[] turnsPerStage = {
                        40, 40, 40, 45, 40,   // Stage 1-5
                        50, 50, 50, 50, 65,   // Stage 6-10
                        65, 60, 70, 60, 60,   // Stage 11-15
                        60, 65, 70, 80, 65    // Stage 16-20
                    };
                    int stageIdx = stageData.stageNumber - 1;
                    boss.maxTurns = (stageIdx >= 0 && stageIdx < turnsPerStage.Length)
                        ? turnsPerStage[stageIdx] : 50;
                    activeBoss = boss;
                    bossRemainingTurns = boss.maxTurns;
                    BossMaxTurns = boss.maxTurns;
                }

                // 破壊イベントを購読
                block.OnBlockDestroyed += HandleBlockDestroyed;
                totalBlockCount++;
            }
        }

        Debug.Log($"ステージ構築完了: {totalBlockCount} ブロック");
    }

    // ---- イベントハンドラ ----

    private void HandleBlockDestroyed(BlockBase block)
    {
        destroyedBlockCount++;

        // ボス撃破時はターンカウントを無効化
        if (block is BossBlock && activeBoss != null)
        {
            Debug.Log("[Boss] ボス撃破！");
            activeBoss = null;
        }

        OnBlockDestroyedCallback?.Invoke();
    }

    // ---- デバッグ用：テストステージを生成 ----

    public void BuildTestStage()
    {
        if (blockPrefabs == null || blockPrefabs.Length == 0 || blockPrefabs[0] == null)
        {
            Debug.LogWarning("NormalBlock Prefab が設定されていません。");
            return;
        }

        foreach (Transform child in blockParent)
            Destroy(child.gameObject);

        totalBlockCount = 0;
        destroyedBlockCount = 0;

        // 5列 × 3行 のテスト配置
        float startX = -2.0f;
        float startY = 4.0f;
        float spacingX = 1.05f;
        float spacingY = 0.55f;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                Vector3 pos = new Vector3(
                    startX + col * spacingX,
                    startY - row * spacingY,
                    0
                );
                GameObject blockGo = Instantiate(blockPrefabs[0], pos, Quaternion.identity, blockParent);
                BlockBase block = blockGo.GetComponent<BlockBase>();
                if (block != null)
                {
                    block.OnBlockDestroyed += HandleBlockDestroyed;
                    totalBlockCount++;
                }
            }
        }

        Debug.Log($"テストステージ構築完了: {totalBlockCount} ブロック");
    }

    /// <summary>
    /// 全ブロック種を混ぜたテストステージ
    /// </summary>
    public void BuildMixedTestStage()
    {
        if (blockPrefabs == null) return;

        foreach (Transform child in blockParent)
            Destroy(child.gameObject);

        totalBlockCount = 0;
        destroyedBlockCount = 0;

        float startX = -2.0f;
        float startY = 4.0f;
        float spacingX = 1.05f;
        float spacingY = 0.55f;

        // ブロック種別パターン（行ごと）
        // 0:Normal 1:Durable 2:Explosion 3:Chain
        int[,] pattern = {
            { 0, 0, 1, 0, 0 }, // 行1: 通常×4 + 耐久×1
            { 0, 2, 0, 2, 0 }, // 行2: 爆発ブロック2個
            { 3, 3, 3, 3, 3 }, // 行3: 連鎖ブロック5個
        };

        for (int row = 0; row < pattern.GetLength(0); row++)
        {
            for (int col = 0; col < pattern.GetLength(1); col++)
            {
                int typeIndex = pattern[row, col];
                if (typeIndex >= blockPrefabs.Length || blockPrefabs[typeIndex] == null) continue;

                Vector3 pos = new Vector3(
                    startX + col * spacingX,
                    startY - row * spacingY,
                    0
                );

                GameObject blockGo = Instantiate(blockPrefabs[typeIndex], pos, Quaternion.identity, blockParent);
                BlockBase block = blockGo.GetComponent<BlockBase>();
                if (block != null)
                {
                    if (block is DurableBlock d) d.SetHP(3);
                    block.OnBlockDestroyed += HandleBlockDestroyed;
                    totalBlockCount++;
                }
            }
        }

        Debug.Log($"混合テストステージ構築完了: {totalBlockCount} ブロック");
    }

    // ==== ボスターンカウント ====

    /// <summary>
    /// パドルヒット時に呼ばれる。ボスが生存中ならターンを消費する。
    /// </summary>
    public void OnPaddleHit()
    {
        if (activeBoss == null || !activeBoss.IsAlive) return;

        bossRemainingTurns--;
        OnBossTurnChanged?.Invoke(bossRemainingTurns, BossMaxTurns);

        if (bossRemainingTurns <= 0)
        {
            Debug.Log("[Boss] ターン切れ！ゲームオーバー！");
            OnBossTurnExpired?.Invoke();
        }
    }

    /// <summary>
    /// 段階5-E（チュートリアル）用：ボスの残り打数を補充する。
    /// BossMaxTurns も同時に増やして UI 表示が破綻しないようにする。
    /// </summary>
    public void RefillBossTurns(int amount)
    {
        if (amount <= 0) return;
        bossRemainingTurns += amount;
        BossMaxTurns = Mathf.Max(BossMaxTurns, bossRemainingTurns);
        OnBossTurnChanged?.Invoke(bossRemainingTurns, BossMaxTurns);
    }

    // ==== ボス攻撃処理 ====

    /// <summary>
    /// ボスのHP閾値を越えたときに呼ばれる攻撃ハンドラ
    /// phase: 1=75%, 2=50%, 3=25%
    /// </summary>
    /// <summary>
    /// ボスHP50ごとの減少通知ハンドラ — ボスダメージボイスを再生
    /// </summary>
    private void HandleBossHPMilestone(BossBlock boss)
    {
        if (bossCharData == null || bossCharData.voiceBossDamaged == null) return;

        // 高火力 / 連撃で被弾ボイスが連発しないよう 3秒クールダウン
        if (Time.time - lastBossDamagedVoiceTime < BOSS_DAMAGED_VOICE_COOLDOWN) return;
        lastBossDamagedVoiceTime = Time.time;

        // ボス被弾は頻発するため Low 優先度（Ultやクリア時のセリフを邪魔しない）
        AudioManager.Instance?.PlayVoice(bossCharData.voiceBossDamaged, bossCharData.voiceVolumeMultiplier, AudioManager.VoicePriority.Low);
    }

    private void HandleBossAttack(BossBlock boss, int phase)
    {
        Debug.Log($"[Boss] 攻撃フェーズ{phase} 発動! 難度={boss.difficulty} true={isTrueStage}");

        // ボス攻撃ボイス再生（Mid優先度：通常イベント）
        if (bossCharData != null && bossCharData.voiceBossAttack != null)
            AudioManager.Instance?.PlayVoice(bossCharData.voiceBossAttack, bossCharData.voiceVolumeMultiplier, AudioManager.VoicePriority.Mid);

        // 裏ステージ倍率
        bool tru = isTrueStage && stageData != null && stageData.hasTrueStage;
        float paddleMul = tru ? stageData.truePaddleShrinkMul : 1f;
        float atkMul    = tru ? stageData.trueAttackMul        : 1f;

        // 通知メッセージを組み立てて GameUI に表示
        var actions = new System.Collections.Generic.List<string>();

        // フェーズ1: パドル一時縮小
        if (phase >= 1)
        {
            float dur = 5f * paddleMul;
            StartCoroutine(ShrinkPaddleTemporary(0.6f, dur));
            actions.Add($"パドル縮小 {dur:F0}秒");
        }

        // フェーズ2: 上からブロック（Durable）を降らせる
        if (phase >= 2)
        {
            int durableCount = Mathf.Max(1, Mathf.RoundToInt((boss.difficulty + 1) * atkMul));
            StartCoroutine(DropBlocks(boss.difficulty, false, durableCount));
            actions.Add($"追加ブロック落下 ×{durableCount}");
        }

        // フェーズ3: スピードブロックを降らせる（ステージごとの個数・裏でも同数）
        if (phase >= 3)
        {
            // ステージ番号ごとのSpeedBlock落下数
            int[] speedDropCounts = {
                0, 0, 2, 2, 2,   // Stage 1-5
                3, 3, 3, 3, 3,   // Stage 6-10
                3, 3, 3, 3, 4,   // Stage 11-15
                4, 4, 4, 4, 4    // Stage 16-20
            };
            int stageIdx = stageData.stageNumber - 1;
            int dropCount = (stageIdx >= 0 && stageIdx < speedDropCounts.Length)
                ? speedDropCounts[stageIdx] : 0;
            if (dropCount > 0)
            {
                StartCoroutine(DropBlocks(boss.difficulty, true, dropCount));
                actions.Add($"スピードブロック落下 ×{dropCount}");
            }
        }

        // 裏ステージ専用: フェーズ 1〜3 のどれでもボール透明化を仕掛ける
        if (tru && phase >= 1)
        {
            StartCoroutine(TriggerBallTransparency());
            actions.Add("ボール透明化 5秒");
        }

        // GameUI に通知（キャラアイコン上に表示）
        if (actions.Count > 0)
        {
            string body = string.Join("\n", actions);
            var gameUI = FindObjectOfType<GameUI>();
            if (gameUI != null)
                gameUI.ShowBossAction(body);
        }
    }

    /// <summary>
    /// 裏ステージ専用：全ボールを 5秒間、0.1秒間隔で点滅的に透明化する
    /// </summary>
    IEnumerator TriggerBallTransparency()
    {
        var balls = FindObjectsOfType<BallController>();
        foreach (var b in balls)
        {
            if (b != null) b.BeginTransparencyPulse(5f, 0.1f);
        }
        yield break;
    }

    /// <summary>
    /// 進行中の Boss 攻撃効果（パドル縮小・ブロック降下など）をすべてキャンセルし、
    /// パドルのスケールを元サイズに復元する。裏ステージ突入時に使用。
    /// </summary>
    public void ResetBossAttackEffects()
    {
        // 進行中のコルーチン（ShrinkPaddleTemporary / DropBlocks / BossWarningFlash 等）を全停止
        StopAllCoroutines();

        // パドルスケールを元に戻す
        if (paddleRef != null && originalPaddleScaleX > 0f)
        {
            Vector3 scale = paddleRef.transform.localScale;
            scale.x = originalPaddleScaleX;
            paddleRef.transform.localScale = scale;
            Debug.Log($"[TrueStage] パドルスケール復元 → {originalPaddleScaleX:F2}");
        }
    }

    /// <summary>
    /// パドルを一時的に縮小させる
    /// </summary>
    IEnumerator ShrinkPaddleTemporary(float shrinkRatio, float duration)
    {
        if (paddleRef == null) yield break;
        if (shrinkRatio <= 0f) yield break; // 0除算保護

        // 初回に元のスケールを記録
        if (originalPaddleScaleX < 0f)
            originalPaddleScaleX = paddleRef.transform.localScale.x;

        // 現在スケールに shrinkRatio を掛ける（重ね掛け対応）
        Vector3 scale = paddleRef.transform.localScale;
        float beforeX = scale.x;
        scale.x *= shrinkRatio;
        paddleRef.transform.localScale = scale;

        Debug.Log($"[Boss] パドル縮小: {beforeX:F2} → {scale.x:F2} ({duration}秒間) ratio={shrinkRatio}");

        // 画面フラッシュ（警告演出）
        StartCoroutine(BossWarningFlash());

        // 発射待機（Ready）中はタイマーを進めない（奥義の WaitForPlaying と同じ挙動）
        float remaining = duration;
        while (remaining > 0f)
        {
            yield return null;
            if (GameManager.Instance != null
                && GameManager.Instance.CurrentState == GameManager.GameState.Ready)
                continue; // 待機中は時間消費しない（パドルは縮小したまま）
            remaining -= Time.deltaTime;
        }

        // 復帰：自分が掛けた shrinkRatio をそのまま割って戻す（差分復帰）
        // これにより他のコルーチンとの重ね掛け・復帰順序に依存せず、
        // 全コルーチンが終われば必ず元のスケールに戻る
        if (paddleRef != null)
        {
            scale = paddleRef.transform.localScale;
            scale.x /= shrinkRatio;
            paddleRef.transform.localScale = scale;
            Debug.Log($"[Boss] パドル復帰: /{shrinkRatio} → {scale.x:F2}");
        }
    }

    /// <summary>
    /// 上からブロックを降らせる攻撃
    /// </summary>
    IEnumerator DropBlocks(int difficulty, bool useSpeedBlock, int overrideCount = -1)
    {
        int count = (overrideCount > 0) ? overrideCount : difficulty + 1;
        float dropY = 6f; // 画面上から
        float minX = -4f;
        float maxX = 4f;

        // 警告表示のための短い待機
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < count; i++)
        {
            int prefabIndex;
            if (useSpeedBlock && blockPrefabs.Length > 4 && blockPrefabs[4] != null)
                prefabIndex = 4; // SpeedBlock
            else if (blockPrefabs.Length > 1 && blockPrefabs[1] != null)
                prefabIndex = 1; // DurableBlock
            else
                continue;

            float xPos = Random.Range(minX, maxX);
            Vector3 spawnPos = new Vector3(xPos, dropY, 0f);

            GameObject blockGo = Instantiate(blockPrefabs[prefabIndex], spawnPos, Quaternion.identity, blockParent);
            BlockBase block = blockGo.GetComponent<BlockBase>();
            if (block != null)
            {
                // 降ってくるブロックのHP（難度に応じて）
                if (block is DurableBlock durable)
                    durable.SetHP(difficulty * 3 + 2);
                if (block is SpeedBlock speedBlock)
                    speedBlock.SetSpeedMultiplier(1.15f);

                block.OnBlockDestroyed += HandleBlockDestroyed;
                totalBlockCount++;

                // 降下アニメーション
                StartCoroutine(AnimateBlockDrop(blockGo, spawnPos, difficulty));
            }

            yield return new WaitForSeconds(0.3f); // 少しずつ降らせる
        }
    }

    /// <summary>
    /// ブロック降下アニメーション
    /// </summary>
    IEnumerator AnimateBlockDrop(GameObject blockGo, Vector3 startPos, int difficulty)
    {
        if (blockGo == null) yield break;

        // 降下先Y座標（画面中央～やや下）
        float targetY = Random.Range(0f, 3f);
        float dropSpeed = 3f + difficulty * 0.5f;
        Vector3 targetPos = new Vector3(startPos.x, targetY, 0f);

        float t = 0f;
        while (t < 1f && blockGo != null)
        {
            t += Time.deltaTime * dropSpeed / (startPos.y - targetY);
            blockGo.transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        // 着地時に少し揺れ
        if (blockGo != null)
            blockGo.transform.position = targetPos;
    }

    /// <summary>
    /// ボス攻撃時の警告フラッシュ演出
    /// </summary>
    IEnumerator BossWarningFlash()
    {
        var go = new GameObject("BossFlash");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        var img = go.AddComponent<UnityEngine.UI.Image>();
        Color flashColor = new Color(1f, 0f, 0f, 0.3f);
        img.color = flashColor;
        img.raycastTarget = false;

        // 2回点滅
        for (int i = 0; i < 2; i++)
        {
            img.color = flashColor;
            yield return new WaitForSeconds(0.15f);
            img.color = Color.clear;
            yield return new WaitForSeconds(0.1f);
        }

        Destroy(go);
    }
}
