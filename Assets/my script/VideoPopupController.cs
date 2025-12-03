using UnityEngine;
using UnityEngine.Video;

public class VideoPopupController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject contentRoot; // ポップアップの表示/非表示を切り替えるルートオブジェクト

    void Start()
    {
        // 最初は非表示にしておく
        ClosePopup();
    }

    // 外部から呼ばれる：URLを受け取って再生
    public void OpenAndPlay(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        contentRoot.SetActive(true);
        videoPlayer.url = url;
        videoPlayer.Prepare();
        
        // 準備ができたら再生するイベント登録
        videoPlayer.prepareCompleted += (source) => 
        {
            videoPlayer.Play();
        };
    }

    // 閉じるボタンから呼ばれる
    public void ClosePopup()
    {
        videoPlayer.Stop();
        contentRoot.SetActive(false);
    }
}