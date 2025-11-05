using UnityEngine;
using System.Collections;
using TMPro; // TextMeshProを使うために必要


/// <summary>
/// ボタンのOnClickイベントから呼び出されることを想定した、固定時間のタイマー起動スクリプト。
/// </summary>
public class Timeractivator : MonoBehaviour
{
    // Unity Eventで設定するタイマー時間を分単位で定義
    [Header("Timer Duration")]
    [Tooltip("タイマーの初期設定時間 (分)")]
    public int timerMinutes = 1;
    public int minMinutes = 1;  // 設定可能な最小時間
    public int maxMinutes = 60; // 設定可能な最大時間

    // インスペクターからTextMeshProコンポーネントをアタッチするための変数
    [Tooltip("残り時間を表示するためのTextMeshProコンポーネント")]
    public TextMeshPro timerTextDisplay;

    [Header("Alarm Settings")]
    public AudioSource audioSource;
    public AudioClip alarmSound;
    public Color flashColor = Color.red; // 点滅させる色（赤を設定）
    public float flashDuration = 3f;     // 点滅させる時間（3秒間）
    public float flashInterval = 0.2f;   // 点滅の間隔（0.2秒ごと）

    // 内部でタイマーが動作中かどうかを管理
    private bool isTimerRunning = false;

    void Start()
    {
        // ゲーム開始時 (Start) にタイマー表示を初期状態で非表示にする
        if (timerTextDisplay != null)
        {
            timerTextDisplay.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Unity Event (On Clicked) から呼び出されるメソッド。
    /// </summary>
    public void StartTimer()
    {
        // 🚨 修正: もしアラームが実行中なら、それを停止してリセットする
        if (isAlarming) // 新しく追加するフラグ
        {
            StopAlarm();
            return;
        }

        // タイマーが実行中でなければ、起動する
        ActivateTimer(timerMinutes);
    }

    // 🚨 追加: アラーム状態を追跡する新しいフラグ
    private bool isAlarming = false;

    /// <summary>
    /// アラームの停止と表示のリセットを行う
    /// </summary>
    public void StopAlarm()
    {
        // 実行中のすべてのアニメーション（点滅など）と音を停止
        StopAllCoroutines();

        // 状態フラグをリセット
        isTimerRunning = false;
        isAlarming = false;

        // 音源を停止
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // テキストを非表示に戻す
        if (timerTextDisplay != null)
        {
            // 色を白に戻し、非表示にする
            timerTextDisplay.color = Color.white;
            timerTextDisplay.gameObject.SetActive(false);
        }

        Debug.Log("🔔 アラームが解除されました。");
    }

    // 任意: 開発中にタイマーを強制停止したい場合
    public void StopTimer()
    {
        if (isTimerRunning)
        {
            StopAllCoroutines();
            isTimerRunning = false;
            // 非表示にする
            if (timerTextDisplay != null)
            {
                timerTextDisplay.gameObject.SetActive(false);
            }
            Debug.Log("タイマーを強制停止しました。");
        }
    }

    // ----------------------------------------------------------------------
    // メソッド本体（コンパイルエラー解消のため、クラス内に正しく定義されていることを確認）
    // ----------------------------------------------------------------------

    private void ActivateTimer(int minutes)
    {
        if (isTimerRunning)
        {
            Debug.LogWarning("タイマーは既に動作中です。");
            return;
        }

        if (minutes <= 0)
        {
            Debug.LogError("タイマー時間が無効です。1分以上の時間を設定してください。");
            return;
        }

        // タイマー起動時：タイマー表示を有効にする
        if (timerTextDisplay != null)
        {
            timerTextDisplay.gameObject.SetActive(true);
        }

        int seconds = minutes * 60;

        // UpdateTimerDisplay を呼び出し
        UpdateTimerDisplay(seconds);

        Debug.Log($"✅ {minutes} 分（{seconds}秒）のタイマーを起動しました！");
        StartCoroutine(RunTimer(seconds));
    }

    /// <summary>
    /// コルーチンで時間を計測し、毎秒テキストを更新します。
    /// </summary>
    private IEnumerator RunTimer(int totalSeconds)
    {
        isTimerRunning = true;
        int remainingSeconds = totalSeconds;

        while (remainingSeconds > 0)
        {
            // 残り時間をMM:SS形式にフォーマットして表示
            UpdateTimerDisplay(remainingSeconds);

            yield return new WaitForSeconds(1f); // 1秒待機
            remainingSeconds--;
        }

        // 終了時の処理
        isTimerRunning = false;
        // UpdateTimerDisplay を呼び出し
        UpdateTimerDisplay(0);

        Debug.Log($"🔔 タイマー終了！ {totalSeconds} 秒が経過しました。");

        // 🚨 修正: 点滅と音のコルーチンを起動し、ボタンが押されるのを待つ
        StartCoroutine(FlashAndPlaySound());

        // RunTimer コルーチン自体はここで終了
        yield break;
    }

    private IEnumerator FlashAndPlaySound()
    {
        isAlarming = true; // 🚨 アラーム状態に設定
        Color originalColor = timerTextDisplay.color;

        // 1. サウンドの再生設定（ループ設定はUnity Editorで行うか、ここで手動で制御する）
        if (audioSource != null && alarmSound != null)
        {
            audioSource.clip = alarmSound;
            audioSource.loop = true; // 🚨 ループ再生を有効化
            audioSource.Play();
        }

        // 2. 点滅ロジック (isAlarmingが false になるまで無限ループ)
        while (isAlarming)
        {
            // 点滅表示
            timerTextDisplay.color = flashColor;
            yield return new WaitForSeconds(flashInterval); // 0.2秒待つ

            // 点滅非表示（元の色に戻す）
            timerTextDisplay.color = originalColor;
            yield return new WaitForSeconds(flashInterval); // 0.2秒待つ
        }

        // 3. アラームが停止された後の処理（StopAlarm()が呼ばれたことでこのコルーチンは停止される）
    }

    /// <summary>
    /// 時間（秒）をMM:SS形式にフォーマットし、テキストに設定します。
    /// </summary>
    private void UpdateTimerDisplay(int seconds)
    {
        // TextMeshProへの参照がない場合は何もしない
        if (timerTextDisplay == null) return;

        int minutes = seconds / 60;
        int displaySeconds = seconds % 60;

        // "{0:00}:{1:00}" は、0埋め2桁の分と秒を意味します (例: 01:05)
        timerTextDisplay.text = string.Format("{0:00}:{1:00}", minutes, displaySeconds);

        // 終了時に色を変えるなど（オプション）
        if (seconds == 0)
        {
            timerTextDisplay.color = Color.red;
            timerTextDisplay.text = "終了";
        }
        else
        {
            timerTextDisplay.color = Color.white; // 通常の色に戻す
        }
    }

    // Temporarily added method to force Unity to recognize the script
    public void ForceRefresh(int value)
    {
        Debug.Log("Refresh check: " + value);
    }

    /// <summary>
    /// タイマー設定時間を1分増やします。
    /// </summary>
    public void IncreaseMinutes()
    {
        if (timerMinutes < maxMinutes)
        {
            timerMinutes++;
            UpdateSetTimeDisplay(); // 🚨 ステップ 1-3で作成する表示更新メソッドを呼び出す
            Debug.Log($"時間増加: {timerMinutes}分");
        }
    }

    /// <summary>
    /// タイマー設定時間を1分減らします。
    /// </summary>
    public void DecreaseMinutes()
    {
        if (timerMinutes > minMinutes)
        {
            timerMinutes--;
            UpdateSetTimeDisplay(); // 🚨 ステップ 1-3で作成する表示更新メソッドを呼び出す
            Debug.Log($"時間減少: {timerMinutes}分");
        }
    }

    /// <summary>
    /// タイマーパネル全体の表示/非表示を切り替えます。（音声認識用）
    /// </summary>
    public void ToggleTimerPanelVisibility(bool isVisible)
    {
        // このスクリプトがアタッチされているオブジェクト（Timer Panel）の表示を切り替える
        this.gameObject.SetActive(isVisible);

        if (isVisible)
        {
            // 出現時に現在の設定時間を表示
            UpdateSetTimeDisplay();
        }
    }
    private void UpdateSetTimeDisplay()
    {
        if (timerTextDisplay != null && !isTimerRunning) // 実行中でない場合のみ更新
        {
            // D2フォーマットで「01:00」のように表示
            timerTextDisplay.text = $"{timerMinutes:D2}:00";
            timerTextDisplay.gameObject.SetActive(true);
            // 🚨 オプション: 設定中の表示であることを示すため、テキストの色を薄くしても良い
        }
    }
    // Timeractivator.cs 内
    // Timeractivator.cs

/// <summary>
/// アラームの停止と表示のリセットを行う（外部のボタンやStartTimerから呼び出される）
/// </summary>
    public void ResetTimer()
    {
    // 実行中のすべてのアニメーション（点滅など）と音を停止
        StopAllCoroutines(); 

    // 状態フラグをリセット
        isTimerRunning = false;
        isAlarming = false; 

    // 音源を停止
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

    // テキストを非表示に戻す
        if (timerTextDisplay != null)
        {
        // 色を白に戻し、非表示にする
            timerTextDisplay.color = Color.white;
            timerTextDisplay.gameObject.SetActive(false);
        }

        Debug.Log("🔔 アラームが解除されました。タイマーがリセットされました。");
    }

/// <summary>
/// 音声認識ボタンから呼び出され、タイマーパネル全体を切り替える (引数なしに修正)
/// </summary>
    public void TogglePanelVisibility()
    {
        GameObject panelRoot = this.gameObject;
        bool isVisible = !panelRoot.activeSelf;
        panelRoot.SetActive(isVisible);

        if (isVisible)
        {
            UpdateSetTimeDisplay();
        }
        else
        {
        // 非表示にする際、もしタイマーが動いていたら停止させる
            if (isTimerRunning || isAlarming)
            {
                ResetTimer(); 
            }
        }
}
}