using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using Microsoft.MixedReality.OpenXR; 
using Microsoft.MixedReality.OpenXR.ARSubsystems;

public class SpiceManager : MonoBehaviour
{
    [Header("Basic Settings")]
    public ARMarkerManager MarkerManager;
    public List<SpiceData> seasoningList;

    [Header("Optional Settings")]
    [Tooltip("指から出すビームのプレハブ (空欄ならビームなし)")]
    public GameObject BeamPrefab;

    private GameObject activeBeamInstance;
    private BeamController activeBeamController;

    void Start()
    {
        if (MarkerManager == null) MarkerManager = FindObjectOfType<ARMarkerManager>();
        
        if (MarkerManager != null)
        {
            MarkerManager.markersChanged += OnARMarkersChanged;
        }

        TurnOffAllHighlights();
    }

    void OnDestroy()
    {
        if (MarkerManager != null) MarkerManager.markersChanged -= OnARMarkersChanged;
    }

    // ----------------------------------------------------------------
    // 1. QRコード検出処理
    // ----------------------------------------------------------------
    private void OnARMarkersChanged(ARMarkersChangedEventArgs args)
    {
        foreach (var marker in args.added) ProcessMarker(marker);
        foreach (var marker in args.updated) ProcessMarker(marker);
    }

    private void ProcessMarker(ARMarker marker)
    {
        string text = marker.GetDecodedString();
        if (string.IsNullOrEmpty(text)) return;

        SpiceData data = seasoningList.Find(d => d.QrCodeData == text);

        if (data != null && !data.IsAnchorRegistered)
        {
            RegisterAnchorForSpice(marker, data);
        }
    }

    private void RegisterAnchorForSpice(ARMarker marker, SpiceData data)
    {
        // 1. 空のアンカーオブジェクトを作る（これがQRの位置に固定される）
        GameObject anchorRoot = new GameObject($"Anchor_{data.SeasoningName}");
        anchorRoot.transform.SetPositionAndRotation(marker.transform.position, marker.transform.rotation);
        anchorRoot.AddComponent<ARAnchor>();

        if (data.HighlightObject != null)
        {
            // 2. ピン（HighlightObject）をアンカーの子にする
            data.HighlightObject.transform.SetParent(anchorRoot.transform, true);
            
            // 3. ローカル座標をリセット（これで親の位置＝QRの位置に来る）
            // ※ピン自体のY座標などはピン側のInspectorで調整済みとする
            data.HighlightObject.transform.localPosition = Vector3.zero;
            data.HighlightObject.transform.localRotation = Quaternion.identity;
            
            // 4. 登録成功の合図（3秒ピカッ）
            StartCoroutine(FlashHighlight(data.HighlightObject, 3.0f));
        }

        data.IsAnchorRegistered = true;
        Debug.Log($"✅ QR登録完了: {data.SeasoningName}");

        // ★追加: 全員揃ったかチェックして、揃っていたら認識を止める
        CheckAndStopMarkerDetection();
    }

    private void CheckAndStopMarkerDetection()
    {
        bool allRegistered = true;
        foreach (var spice in seasoningList)
        {
            if (!spice.IsAnchorRegistered)
            {
                allRegistered = false;
                break;
            }
        }

        if (allRegistered)
        {
            if (MarkerManager != null)
            {
                MarkerManager.enabled = false;
                Debug.Log("🏁 全調味料の登録完了。マーカー認識を停止します。");
            }
        }
    }

    private IEnumerator FlashHighlight(GameObject obj, float duration)
    {
        obj.SetActive(true);
        yield return new WaitForSeconds(duration);
        obj.SetActive(false);
    }

    // ----------------------------------------------------------------
    // 2. レシピ連携 & ビーム制御
    // ----------------------------------------------------------------
    public void HighlightSeasoning(string requiredSeasoningName, bool show)
    {
        SpiceData data = seasoningList.Find(d => d.SeasoningName == requiredSeasoningName);

        // ▼ エラー診断 ▼
        if (data == null)
        {
            Debug.LogError($"❌ エラー: '{requiredSeasoningName}' がリストにありません。Inspectorの名前を確認してください。");
            return;
        }
        if (data.HighlightObject == null)
        {
            Debug.LogError($"❌ エラー: '{requiredSeasoningName}' のHighlight Objectが空です。");
            return;
        }
        if (!data.IsAnchorRegistered)
        {
            Debug.LogWarning($"⚠️ 待機中: '{requiredSeasoningName}' のQRコードをまだ読んでいません。");
            return;
        }

        // ▼ 表示処理 ▼
        if (show)
        {
            Debug.Log($"✨ ハイライトON: {requiredSeasoningName}");
            data.HighlightObject.SetActive(true);
            if (BeamPrefab != null) ControlBeam(data, true);
        }
        else
        {
            data.HighlightObject.SetActive(false);
        }
    }

    public void TurnOffAllHighlights()
    {
        // ビーム停止
        if (activeBeamInstance != null)
        {
            activeBeamInstance.SetActive(false);
            if (activeBeamController != null) activeBeamController.StopBeam();
        }

        // 全アイコン消灯
        foreach (var data in seasoningList)
        {
            if (data.HighlightObject != null)
            {
                data.HighlightObject.SetActive(false);
            }
        }
    }

    private void ControlBeam(SpiceData data, bool show)
    {
        if (show)
        {
            if (activeBeamInstance == null)
            {
                activeBeamInstance = Instantiate(BeamPrefab);
                activeBeamController = activeBeamInstance.GetComponent<BeamController>();
            }
            
            if (activeBeamController != null)
            {
                // ★修正: 親(Anchor)ではなく、HighlightObjectそのものを狙う
                // これでピンの位置に正確にビームが向かいます
                activeBeamController.SetTarget(data.HighlightObject.transform);
                activeBeamInstance.SetActive(true);
            }
        }
        else
        {
            if (activeBeamInstance != null)
            {
                activeBeamInstance.SetActive(false);
                if(activeBeamController != null) activeBeamController.StopBeam();
            }
        }
    }
}