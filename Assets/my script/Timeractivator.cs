using UnityEngine;
using System.Collections;
using TMPro;

public class Timeractivator : MonoBehaviour
{
    [Header("Timer Duration")]
    [Tooltip("タイマーの初期設定時間 (分)")]
    public int timerMinutes = 1;
    public int minMinutes = 1;
    public int maxMinutes = 60;

    [Tooltip("残り時間を表示するためのTextMeshProコンポーネント")]
    public TextMeshPro timerTextDisplay;

    [Header("Alarm Settings")]
    public AudioSource audioSource;
    public AudioClip alarmSound;
    public Color flashColor = Color.red;
    public float flashDuration = 3f;
    public float flashInterval = 0.2f;

    // 状態管理フラグ
    private bool isTimerRunning = false;
    private bool isAlarming = false;
    private int currentRemainingSeconds = 0;

    void Start()
    {
        // 🚨 修正: ゲーム開始時から時間を表示しておく
        if (timerTextDisplay != null)
        {
            timerTextDisplay.gameObject.SetActive(true); // trueに変更
            UpdateSetTimeDisplay(); // 初期時間（01:00など）を表示
        }
    }

    /// <summary>
    /// スタート・一時停止・再開・アラーム停止を制御するメインメソッド
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
            Debug.Log($"⏸️ タイマーを一時停止しました。残り: {currentRemainingSeconds}秒");
            return;
        }

        // 3. タイマーが止まっている場合（初回または一時停止中）
        
        // 残り時間がなければ（0または初期状態）、設定時間からセットする
        if (currentRemainingSeconds <= 0)
        {
            currentRemainingSeconds = timerMinutes * 60;
            Debug.Log($"▶️ タイマーを新規スタート: {timerMinutes}分");
        }
        else
        {
            Debug.Log($"▶️ タイマーを再開: 残り {currentRemainingSeconds}秒");
        }

        StartCoroutine(RunTimer());
    }

    public void ResetTimer()
    {
        StopAllCoroutines();
        isTimerRunning = false;
        isAlarming = false;
        currentRemainingSeconds = 0;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // 🚨 修正: リセットしても非表示にせず、初期時間に戻して表示し続ける
        if (timerTextDisplay != null)
        {
            timerTextDisplay.color = Color.white;
            timerTextDisplay.gameObject.SetActive(true); // trueのまま
            UpdateSetTimeDisplay(); // "01:00" に戻す
        }

        Debug.Log("🔄 タイマーをリセットしました。");
    }

    public void IncreaseMinutes()
    {
        if (timerMinutes < maxMinutes)
        {
            timerMinutes++;
            currentRemainingSeconds = 0; // 設定変更時はリセット
            UpdateSetTimeDisplay();
        }
    }

    public void DecreaseMinutes()
    {
        if (timerMinutes > minMinutes)
        {
            timerMinutes--;
            currentRemainingSeconds = 0; // 設定変更時はリセット
            UpdateSetTimeDisplay();
        }
    }

    private IEnumerator RunTimer()
    {
        isTimerRunning = true;

        while (currentRemainingSeconds > 0)
        {
            UpdateTimerDisplay(currentRemainingSeconds);
            yield return new WaitForSeconds(1f);
            currentRemainingSeconds--;
        }

        isTimerRunning = false;
        currentRemainingSeconds = 0;
        UpdateTimerDisplay(0);
        
        Debug.Log("🔔 タイマー終了！");
        
        // 🚨 修正: ここで非表示にする処理を削除しました
        
        StartCoroutine(FlashAndPlaySound());
    }

    // --- 以下、表示・アラーム・パネル制御系 ---

    private void UpdateSetTimeDisplay()
    {
        if (timerTextDisplay != null)
        {
            // 実行中以外でも更新するように条件を緩和
            timerTextDisplay.text = $"{timerMinutes:D2}:00";
            timerTextDisplay.gameObject.SetActive(true);
        }
    }

    private IEnumerator FlashAndPlaySound()
    {
        isAlarming = true;
        Color originalColor = timerTextDisplay.color;

        if (audioSource != null && alarmSound != null)
        {
            audioSource.clip = alarmSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        while (isAlarming)
        {
            timerTextDisplay.color = flashColor;
            yield return new WaitForSeconds(flashInterval);
            timerTextDisplay.color = originalColor;
            yield return new WaitForSeconds(flashInterval);
        }
        
        if (audioSource != null) audioSource.Stop();
        // ここでの色戻しはResetTimerで行うので省略可
    }

    private void UpdateTimerDisplay(int seconds)
    {
        if (timerTextDisplay == null) return;

        int minutes = seconds / 60;
        int displaySeconds = seconds % 60;
        timerTextDisplay.text = string.Format("{0:00}:{1:00}", minutes, displaySeconds);

        if (seconds == 0)
        {
            timerTextDisplay.color = Color.red;
            timerTextDisplay.text = "終了";
        }
        else
        {
            timerTextDisplay.color = Color.white;
        }
    }

    // パネル自体の表示切替（音声コマンド用）
    public void TogglePanelVisibility()
    {
        GameObject panelRoot = this.gameObject;
        bool isVisible = !panelRoot.activeSelf;
        panelRoot.SetActive(isVisible);

        if (isVisible)
        {
            // パネルが出たときに現在の設定時間を表示
            UpdateSetTimeDisplay();
        }
        else
        {
            // パネルを消すときはタイマーもリセットして止める
            if (isTimerRunning || isAlarming)
            {
                 ResetTimer(); 
            }
        }
    }
    
    public void ForceRefresh(int value) { }
}