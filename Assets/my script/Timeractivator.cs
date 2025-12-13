using UnityEngine;
using System.Collections;
using TMPro;

public class Timeractivator : MonoBehaviour
{
    [Header("Timer Duration")]
    [Tooltip("起動時の初期設定時間 (分)")]
    public int defaultMinutes = 1; 
    
    [Tooltip("設定可能な最小時間 (秒)")]
    public int minSeconds = 30;
    [Tooltip("設定可能な最大時間 (秒)")]
    public int maxSeconds = 3600; // 60分

    [Tooltip("残り時間を表示するためのTextMeshProコンポーネント")]
    public TextMeshPro timerTextDisplay;

    [Header("Alarm Settings")]
    public AudioSource audioSource;
    public AudioClip alarmSound;
    public Color endColor = Color.red;     // 終了時の文字色
    public float flashInterval = 0.5f;     // 点滅の間隔

    // 内部状態変数
    private int setupSeconds = 60;         // 現在セットされている設定時間（秒）
    private int currentRemainingSeconds = 0; // 動作中の残り時間（秒）

    // 状態フラグ
    private bool isTimerRunning = false;
    private bool isAlarming = false;

    void Start()
    {
        // 初期時間を設定（分→秒変換）
        setupSeconds = defaultMinutes * 60;

        if (timerTextDisplay != null)
        {
            timerTextDisplay.gameObject.SetActive(true);
            UpdateSetTimeDisplay(); // 初期表示 (例: 01:00)
        }
    }

    /// <summary>
    /// スタート・一時停止・再開・アラーム停止を制御するボタン用メソッド
    /// </summary>
    public void StartTimer()
    {
        // 1. アラーム中なら停止してリセット
        if (isAlarming)
        {
            ResetTimer();
            return;
        }

        // 2. タイマーが動いているなら「一時停止」
        if (isTimerRunning)
        {
            StopAllCoroutines();
            isTimerRunning = false;
            Debug.Log($"⏸️ タイマー一時停止: 残り {currentRemainingSeconds}秒");
            return;
        }

        // 3. タイマー停止中（初回または一時停止中）ならスタート
        
        // 残り時間がなければ、セットされた時間から開始
        if (currentRemainingSeconds <= 0)
        {
            currentRemainingSeconds = setupSeconds;
            Debug.Log($"▶️ タイマー新規スタート: {setupSeconds}秒");
        }
        else
        {
            Debug.Log($"▶️ タイマー再開: 残り {currentRemainingSeconds}秒");
        }

        StartCoroutine(RunTimer());
    }

    public void ResetTimer()
    {
        StopAllCoroutines();
        isTimerRunning = false;
        isAlarming = false;
        currentRemainingSeconds = 0; // 残り時間をクリア

        // 音を止める
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // 表示をリセット（セットされている時間に戻す）
        if (timerTextDisplay != null)
        {
            timerTextDisplay.gameObject.SetActive(true);
            timerTextDisplay.color = Color.white; // 色を白に戻す
            timerTextDisplay.enabled = true;      // 点滅で消えている可能性があるので表示
            UpdateSetTimeDisplay();               // "01:30" などの表示に戻す
        }

        Debug.Log("🔄 タイマーをリセットしました。");
    }

    /// <summary>
    /// ★修正: 設定時間を30秒増やす（起動中なら残り時間もそのまま増やす）
    /// </summary>
    public void IncreaseTime()
    {
        // 1. まず設定時間（ベース）を増やす
        if (setupSeconds + 30 <= maxSeconds)
        {
            setupSeconds += 30;
        }

        // 2. 状況によって処理を分ける
        if (isTimerRunning)
        {
            // 起動中なら、今の残りカウントにそのまま30秒足す
            currentRemainingSeconds += 30;
            UpdateTimerDisplay(currentRemainingSeconds); // 即座に画面更新
            Debug.Log($"⏳ 時間延長: 残り {currentRemainingSeconds}秒");
        }
        else
        {
            // 停止中ならリセット扱いでOK（次のスタートで新しい時間が使われる）
            currentRemainingSeconds = 0; 
            UpdateSetTimeDisplay();
        }
    }

    /// <summary>
    /// ★修正: 設定時間を30秒減らす（起動中なら残り時間もそのまま減らす）
    /// </summary>
    public void DecreaseTime()
    {
        // 1. まず設定時間（ベース）を減らす
        if (setupSeconds - 30 >= minSeconds)
        {
            setupSeconds -= 30;
        }

        // 2. 状況によって処理を分ける
        if (isTimerRunning)
        {
            // 起動中なら、今の残りカウントから30秒引く
            currentRemainingSeconds -= 30;

            // もし引いた結果 0秒以下になったら、0で止める（次のループで終了処理に入る）
            if (currentRemainingSeconds < 0) currentRemainingSeconds = 0;

            UpdateTimerDisplay(currentRemainingSeconds); // 即座に画面更新
            Debug.Log($"⏳ 時間短縮: 残り {currentRemainingSeconds}秒");
        }
        else
        {
            // 停止中
            currentRemainingSeconds = 0;
            UpdateSetTimeDisplay();
        }
    }

    private IEnumerator RunTimer()
    {
        isTimerRunning = true;

        // カウントダウンループ
        while (currentRemainingSeconds > 0)
        {
            UpdateTimerDisplay(currentRemainingSeconds); // 時間を表示更新
            yield return new WaitForSeconds(1f);
            currentRemainingSeconds--;
        }

        // 0になった瞬間
        isTimerRunning = false;
        currentRemainingSeconds = 0;
        
        // 「終了」表示に変更
        if (timerTextDisplay != null)
        {
            timerTextDisplay.text = "終了";
            timerTextDisplay.color = endColor; // 赤くする
        }

        Debug.Log("🔔 タイマー終了！");
        
        // 点滅と音の演出を開始
        StartCoroutine(FlashAndPlaySound());
    }

    // --- 表示・演出系 ---

    /// <summary>
    /// セット中の時間（待機状態）を表示する
    /// </summary>
    private void UpdateSetTimeDisplay()
    {
        if (timerTextDisplay != null)
        {
            int m = setupSeconds / 60;
            int s = setupSeconds % 60;
            timerTextDisplay.text = $"{m:D2}:{s:D2}";
            timerTextDisplay.color = Color.white; // 設定中は白
        }
    }

    /// <summary>
    /// カウントダウン中の時間を表示する
    /// </summary>
    private void UpdateTimerDisplay(int seconds)
    {
        if (timerTextDisplay == null) return;

        int m = seconds / 60;
        int s = seconds % 60;
        timerTextDisplay.text = $"{m:D2}:{s:D2}";
        timerTextDisplay.color = Color.white;
    }

    /// <summary>
    /// 終了時の演出（音ループ＋文字点滅）
    /// </summary>
    private IEnumerator FlashAndPlaySound()
    {
        isAlarming = true;

        // 音を再生（ループ設定）
        if (audioSource != null && alarmSound != null)
        {
            audioSource.clip = alarmSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // 文字の点滅ループ（リセットされるまで無限）
        while (isAlarming)
        {
            if (timerTextDisplay != null)
            {
                // 表示・非表示を切り替えてチカチカさせる
                timerTextDisplay.enabled = !timerTextDisplay.enabled;
            }
            yield return new WaitForSeconds(flashInterval);
        }
    }

    // パネル表示切替（音声コマンド等用）
    public void TogglePanelVisibility()
    {
        bool isVisible = !gameObject.activeSelf;
        gameObject.SetActive(isVisible);

        if (isVisible)
        {
            UpdateSetTimeDisplay();
        }
        else
        {
            // パネルを消すときはタイマーを停止・リセットする
            ResetTimer();
        }
    }

    public void ForceRefresh(int value) { }
}