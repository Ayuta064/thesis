using UnityEngine;
using TMPro; // Canvas上のテキスト(TextMeshProUGUI)用
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

// データ構造の定義（Firestoreのフィールド名に対応）
public class StepData
{
    public string Instruction;
    public string SpiceID;
    public string VideoUrl;
}

public class RecipeViewer : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("レシピの工程を表示するCanvas上のテキスト")]
    public TextMeshProUGUI instructionText;
    
    [Tooltip("現在のステップ数 (例: 1/5)")]
    public TextMeshProUGUI counterText;

    [Header("Video Settings")]
    [Tooltip("「動画を見る」ボタンのGameObject（Canvas内のボタン）")]
    public GameObject watchVideoButton; 
    
    [Tooltip("シーンに配置した動画ポップアップのコントローラー")]
    public VideoPopupController videoPopup; 

    [Header("Database Settings")]
    [Tooltip("取得したいレシピのドキュメントID (例: omlet_cheese)")]
    public string targetRecipeID = "omlet_cheese";

    // 内部データ
    private List<StepData> steps = new List<StepData>();
    private int currentIndex = 0;
    private FirebaseFirestore db;

    void Start()
    {
        // 初期化表示
        instructionText.text = "レシピを読み込み中...";
        if (counterText != null) counterText.text = "-- / --";
        
        // 動画ボタンは最初は隠しておく
        if (watchVideoButton != null) watchVideoButton.SetActive(false);

        // Firestoreの初期化とロード
        db = FirebaseFirestore.DefaultInstance;
        LoadRecipeFromFirestore();
    }

    // ---------------------------------------------------------
    // 1. Firestoreからデータを取得・解析
    // ---------------------------------------------------------
    private void LoadRecipeFromFirestore()
    {
        db.Collection("recipes").Document(targetRecipeID).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                instructionText.text = "読み込みエラー";
                Debug.LogError($"Firestore Error: {task.Exception}");
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists)
            {
                Dictionary<string, object> data = snapshot.ToDictionary();
                
                // "steps" 配列があるか確認
                if (data.ContainsKey("steps"))
                {
                    List<object> stepList = data["steps"] as List<object>;
                    ParseSteps(stepList);
                    
                    // ロード完了後、最初のステップを表示
                    currentIndex = 0;
                    UpdateDisplay();
                }
                else
                {
                    instructionText.text = "手順データが見つかりません";
                }
            }
            else
            {
                instructionText.text = "レシピが見つかりません";
            }
        });
    }

    // 取得したデータをC#のリストに変換する
    private void ParseSteps(List<object> stepList)
    {
        steps.Clear();
        foreach (var item in stepList)
        {
            // FirestoreのMapはDictionaryとして扱われる
            var map = item as Dictionary<string, object>;
            
            if (map != null)
            {
                StepData newStep = new StepData();
                // 辞書から値を取り出し、なければ空文字を入れる安全策
                newStep.Instruction = map.ContainsKey("instruction") ? map["instruction"].ToString() : "";
                newStep.SpiceID = map.ContainsKey("spiceID") ? map["spiceID"].ToString() : "";
                newStep.VideoUrl = map.ContainsKey("video") ? map["video"].ToString() : "";
                
                steps.Add(newStep);
            }
        }
    }

    // ---------------------------------------------------------
    // 2. ボタン操作（Next / Previous / Watch Video）
    // ---------------------------------------------------------

    public void NextStep()
    {
        if (steps.Count == 0) return;

        if (currentIndex < steps.Count - 1)
        {
            currentIndex++;
            UpdateDisplay();
        }
    }

    public void PreviousStep()
    {
        if (steps.Count == 0) return;

        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateDisplay();
        }
    }

    // 「動画を見る」ボタンが押されたときに呼ばれる
    public void OnWatchVideoClicked()
    {
        if (steps.Count == 0) return;

        StepData currentStep = steps[currentIndex];
        
        // URLが有効ならポップアップを開く
        if (!string.IsNullOrEmpty(currentStep.VideoUrl) && videoPopup != null)
        {
            videoPopup.OpenAndPlay(currentStep.VideoUrl);
        }
    }

    // ---------------------------------------------------------
    // 3. 画面表示の更新ロジック
    // ---------------------------------------------------------
    private void UpdateDisplay()
    {
        if (steps.Count == 0) return;

        StepData currentStep = steps[currentIndex];

        // テキスト更新
        instructionText.text = currentStep.Instruction;
        
        // カウンター更新
        if (counterText != null)
        {
            counterText.text = $"{currentIndex + 1} / {steps.Count}";
        }

        // 🚨 動画ボタンの表示制御
        // URLがある場合だけボタンを表示する
        if (watchVideoButton != null)
        {
            bool hasVideo = !string.IsNullOrEmpty(currentStep.VideoUrl);
            watchVideoButton.SetActive(hasVideo);
        }

        // （将来的にここに調味料ハイライトの呼び出しを追加可能）
        // if (!string.IsNullOrEmpty(currentStep.SpiceID)) { ... }

        Debug.Log($"Displaying Step {currentIndex + 1}");
    }
}