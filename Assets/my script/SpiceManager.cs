using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.OpenXR.ARSubsystems;  // ARMarkerManager/ARMarker が定義されている可能性のある内部名前空間
//        // Editorフォルダ内の補助的な型を参照するため
// 🚨 QRコードのクラス定義のために、OpenXR関連のusingが必要


public class SpiceManager : MonoBehaviour
{
    [Tooltip("シーン内のARマーカーマネージャーコンポーネント (Inspectorで割り当て)")]
    public ARMarkerManager MarkerManager;

    [Tooltip("Inspectorで設定する、すべての調味料のデータリスト")]
    public List<SpiceData> seasoningList;

    void Start()
    {
        if (MarkerManager == null)
        {
            Debug.LogError("ARMarkerManagerが割り当てられていません。Inspectorを確認してください。");
            return;
        }
        
        // ARマーカーの変更イベントを購読し、OnARMarkersChanged を呼び出す
        MarkerManager.markersChanged += OnARMarkersChanged;

        // 初期状態でハイライトオブジェクトを非表示にしておく
        foreach (var data in seasoningList)
        {
            if (data.HighlightObject != null)
            {
                data.HighlightObject.SetActive(false);
            }
        }
    }

    void OnDestroy()
    {
        if (MarkerManager != null)
        {
            // アプリ終了時にイベントの購読を解除
            MarkerManager.markersChanged -= OnARMarkersChanged;
        }
    }
    
    // ----------------------------------------------------------------
    // 識別とアンカー登録のロジック
    // ----------------------------------------------------------------

    // ARMarkerManagerから呼び出されるイベントハンドラ
    private void OnARMarkersChanged(ARMarkersChangedEventArgs args)
    {
        // 新しく検出されたQRコードを処理
        foreach (var marker in args.added)
        {
            // QRコードにエンコードされたデータ（例: "SALT"）を取得
            string decodedData = marker.GetDecodedString();
            
            // データリスト内で一致する調味料を検索
            SpiceData data = seasoningList.Find(d => d.QrCodeData == decodedData);

            // データが見つかり、まだアンカーが登録されていなければ処理を実行
            if (data != null && !data.IsAnchorRegistered)
            {
                RegisterAnchorForSpice(marker, data);
            }
        }
        
        // 追跡を失ったマーカー（args.removed）に対するハイライト解除ロジックも、
        // 必要に応じてこのメソッド内に追加できます。
    }

    private void RegisterAnchorForSpice(ARMarker marker, SpiceData data)
    {
        // 1. マーカーの位置と姿勢を取得
        Transform markerTransform = marker.transform;
        
        // 2. マーカーの位置にアンカーのルートGameObjectを作成
        GameObject anchorRoot = new GameObject($"Anchor_{data.SeasoningName}");
        anchorRoot.transform.SetPositionAndRotation(markerTransform.position, markerTransform.rotation);

        // 3. 空間アンカーコンポーネント (ARAnchor) を追加
        //    これにより、マーカーが見えなくなってもホログラムが固定されます。
        ARAnchor anchor = anchorRoot.AddComponent<ARAnchor>(); 

        // 4. ハイライトオブジェクトをアンカーの子にする
        if (data.HighlightObject != null)
        {
            // ワールド座標を維持してアンカーの子にする
            data.HighlightObject.transform.SetParent(anchorRoot.transform, true); 
            data.HighlightObject.SetActive(true);
        }

        // 5. データ構造を更新
        data.IsAnchorRegistered = true;
        Debug.Log($"✅ アンカー登録完了: {data.SeasoningName}");
    }

    // ----------------------------------------------------------------
    // レシピとの連携メソッド (ハイライトのオン/オフ)
    // ----------------------------------------------------------------

    // レシピの工程で呼び出され、ハイライトの表示/非表示を切り替えるメソッド
    // 例: HighlightSeasoning("塩", true) でハイライト表示
    public void HighlightSeasoning(string requiredSeasoningName, bool shouldBeVisible)
    {
        SpiceData data = seasoningList.Find(d => d.SeasoningName == requiredSeasoningName);

        if (data != null && data.IsAnchorRegistered && data.HighlightObject != null)
        {
            data.HighlightObject.SetActive(shouldBeVisible);
        }
    }
}