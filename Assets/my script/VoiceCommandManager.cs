using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Windows.Speech; // 👈 これがキー

public class VoiceCommandManager : MonoBehaviour
{
    [Tooltip("音声で表示するTimer PanelのルートにあるTimeractivator")]
    // Timeractivatorのインスタンスを保持するための変数
    public Timeractivator timerPanelActivator;

    // 🚨 変更: KeywordRecognizerを直接使う
    private KeywordRecognizer keywordRecognizer;
    private string targetKeyword = "Timer"; // 追跡するキーワードを定義

    void Start()
    {
        // ターゲットとなるTimeractivatorが割り当てられているか確認
        if (timerPanelActivator == null)
        {
            Debug.LogError("Timer Panel ActivatorがInspectorで割り当てられていません。");
            return;
        }

    // 🚨 修正: システムがこの環境で利用可能か（サポートされているか）をチェック
        if (!PhraseRecognitionSystem.isSupported)
        {
            Debug.LogError("Windows Speech Recognition System is not supported on this device.");
            return;
        }
    
    // サポートされていれば、初期化と認識を開始する
        InitializeKeywordRecognizer();
    }

    private void InitializeKeywordRecognizer()
    {
        // キーワードを配列として定義
        string[] keywords = new string[] { targetKeyword };
        
        // Recognizerを初期化
        keywordRecognizer = new KeywordRecognizer(keywords);

        // キーワードが認識されたときのイベントを登録
        keywordRecognizer.OnPhraseRecognized += OnKeywordRecognized;

        // 認識を開始
        keywordRecognizer.Start();
        Debug.Log($"音声認識を開始しました。キーワード: {targetKeyword}");
    }

    private void OnKeywordRecognized(PhraseRecognizedEventArgs args)
    {
        // 認識されたテキストが「タイマー」と一致するかチェック
        if (args.text == targetKeyword)
        {
            // Timeractivatorのメソッドを呼び出し、パネルの表示を切り替える
            timerPanelActivator.TogglePanelVisibility(); 
            Debug.Log($"音声コマンド「{targetKeyword}」でパネルの表示を切り替えました。");
        }
    }

    void OnDestroy()
    {
        // 終了時にRecognizerをクリーンアップ
        if (keywordRecognizer != null)
        {
            keywordRecognizer.Dispose();
        }
    }
}