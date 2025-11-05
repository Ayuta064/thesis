using System;
using UnityEngine;

[Serializable]
public class SpiceData
{
    // マーカー/QRコードの内容 (例: "SALT", "SUGAR")
    public string QrCodeData; 
    
    // 調味料の名前 (Inspectorで表示される)
    public string SeasoningName; 
    
    // 追跡用のホログラムオブジェクト (ハイライト表示用)
    public GameObject HighlightObject; 

    // アンカーが登録済みであるかを示すフラグ
    [HideInInspector] // Inspectorに表示しない
    public bool IsAnchorRegistered = false; 
}