using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.OpenXR.ARSubsystems;

public class SpiceManager : MonoBehaviour
{
    [Header("Basic Settings")]
    [Tooltip("Hierarchy 上の ARMarkerManager（1つだけ）")]
    public ARMarkerManager MarkerManager;

    [Tooltip("調味料データ（QR文字列・ハイライト・状態）")]
    public List<SpiceData> seasoningList;

    [Header("Optional Settings")]
    [Tooltip("指から対象へ向かうビームのPrefab（任意）")]
    public GameObject BeamPrefab;

    // 内部状態
    private GameObject activeBeamInstance;
    private BeamController activeBeamController;

    // ★ 同じQRを二重処理しないためのガード
    private HashSet<string> processedQRCodes = new HashSet<string>();

    // ----------------------------------------------------------------
    // Unity Lifecycle
    // ----------------------------------------------------------------

    void Start()
    {
        // Inspector未設定時の保険
        if (MarkerManager == null)
        {
            MarkerManager = FindObjectOfType<ARMarkerManager>();
        }

        if (MarkerManager == null)
        {
            Debug.LogError("❌ ARMarkerManager が見つかりません。Hierarchy を確認してください。");
            return;
        }

        // ★ markersChanged 購読
        MarkerManager.markersChanged += OnARMarkersChanged;

        // 初期状態：全ハイライトOFF
        TurnOffAllHighlights();

        Debug.Log("🚀 SpiceManager 初期化完了");
    }

    void OnDestroy()
    {
        if (MarkerManager != null)
        {
            MarkerManager.markersChanged -= OnARMarkersChanged;
        }
    }

    // ----------------------------------------------------------------
    // 1. QRコード検出処理（★ added のみ使用）
    // ----------------------------------------------------------------

    private void OnARMarkersChanged(ARMarkersChangedEventArgs args)
    {
        // ★ updated / removed は絶対に触らない
        foreach (var marker in args.added)
        {
            ProcessMarker(marker);
        }
    }

    private void ProcessMarker(ARMarker marker)
    {
        string decodedText = marker.GetDecodedString();

        if (string.IsNullOrEmpty(decodedText))
            return;

        // ★ 同一QRの二重処理防止
        if (processedQRCodes.Contains(decodedText))
            return;

        processedQRCodes.Add(decodedText);

        Debug.Log($"📸 QR検出: {decodedText}");

        SpiceData data = seasoningList.Find(d => d.QrCodeData == decodedText);

        if (data == null)
        {
            Debug.LogWarning($"⚠ 未登録QR: {decodedText}");
            return;
        }

        if (!data.IsAnchorRegistered)
        {
            RegisterAnchorForSpice(marker, data);
        }
    }

    // ----------------------------------------------------------------
    // 2. Anchor 登録処理
    // ----------------------------------------------------------------

    private void RegisterAnchorForSpice(ARMarker marker, SpiceData data)
    {
        // ① アンカー用の空オブジェクト作成
        GameObject anchorRoot = new GameObject($"Anchor_{data.SeasoningName}");
        anchorRoot.transform.SetPositionAndRotation(
            marker.transform.position,
            marker.transform.rotation
        );

        // ② ARAnchor 付与（QRの位置を固定）
        anchorRoot.AddComponent<ARAnchor>();

        // ③ ハイライトをアンカーの子に
        if (data.HighlightObject != null)
        {
            data.HighlightObject.transform.SetParent(anchorRoot.transform, true);
            data.HighlightObject.transform.localPosition = Vector3.zero;
            data.HighlightObject.transform.localRotation = Quaternion.identity;

            // 登録完了の視覚フィードバック
            StartCoroutine(FlashHighlight(data.HighlightObject, 3.0f));
        }

        data.IsAnchorRegistered = true;

        Debug.Log($"✅ アンカー登録完了: {data.SeasoningName}");

        // ★ 全調味料登録済みならQR認識停止
        CheckAndStopMarkerDetection();
    }

    private void CheckAndStopMarkerDetection()
    {
        foreach (var spice in seasoningList)
        {
            if (!spice.IsAnchorRegistered)
                return;
        }

        // 全部揃った
        MarkerManager.enabled = false;
        Debug.Log("🏁 全調味料登録完了。QR認識を停止しました。");
    }

    private IEnumerator FlashHighlight(GameObject obj, float duration)
    {
        obj.SetActive(true);
        yield return new WaitForSeconds(duration);
        obj.SetActive(false);
    }

    // ----------------------------------------------------------------
    // 3. レシピ連携（外部から呼ばれる）
    // ----------------------------------------------------------------

    public void HighlightSeasoning(string seasoningName, bool show)
    {
        SpiceData data = seasoningList.Find(d => d.SeasoningName == seasoningName);

        if (data == null)
        {
            Debug.LogError($"❌ '{seasoningName}' が seasoningList に存在しません");
            return;
        }

        if (!data.IsAnchorRegistered)
        {
            Debug.LogWarning($"⚠ '{seasoningName}' はまだQR未登録です");
            return;
        }

        if (data.HighlightObject == null)
        {
            Debug.LogError($"❌ '{seasoningName}' の HighlightObject が未設定です");
            return;
        }

        data.HighlightObject.SetActive(show);

        if (BeamPrefab != null)
        {
            ControlBeam(data, show);
        }
    }

    public void TurnOffAllHighlights()
    {
        // ビーム停止
        if (activeBeamInstance != null)
        {
            activeBeamInstance.SetActive(false);
            if (activeBeamController != null)
                activeBeamController.StopBeam();
        }

        // 全ハイライトOFF
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
                // ★ ピンそのものを狙う
                activeBeamController.SetTarget(data.HighlightObject.transform);
                activeBeamInstance.SetActive(true);
            }
        }
        else
        {
            if (activeBeamInstance != null)
            {
                activeBeamInstance.SetActive(false);
                if (activeBeamController != null)
                    activeBeamController.StopBeam();
            }
        }
    }
}
