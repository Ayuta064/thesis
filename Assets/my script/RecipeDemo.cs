using UnityEngine;
using System.Collections.Generic;

public class RecipeDemo : MonoBehaviour
{
    // 1. SpiceManagerへの参照 (調味料をハイライトさせるため)
    public SpiceManager spiceManager;

    // 2. 現在のレシピの状態
    private int currentStep = 0;
    private List<string> requiredSeasonings = new List<string> { "塩", "砂糖", "醤油" }; // デモ用

    // デモボタンから呼ばれるメソッド
    public void GoToNextStep()
    {
        // 以前のハイライトをオフにする
        if (currentStep > 0)
        {
            // 以前の手順の調味料を非表示にするロジックをここに書く
        }

        currentStep++;

        // 3. ハイライトの実行
        if (currentStep == 1)
        {
            // ステップ1: 「塩」が必要
            spiceManager.HighlightSeasoning("塩", true); // 塩をハイライト
        }
        else if (currentStep == 2)
        {
            // ステップ2: 「砂糖」が必要
            spiceManager.HighlightSeasoning("砂糖", true); // 砂糖をハイライト
        }
        // ... (他のステップも同様に続く)
    }
}