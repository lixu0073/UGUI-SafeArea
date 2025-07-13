using UnityEngine;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.Threading;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaUGUI : MonoBehaviour
{
    private RectTransform rectTransform;
    private CancellationTokenSource cts;

    [Header("是否忽略安全区")]
    [SerializeField] private bool isUpAdjust = false;
    [SerializeField] private bool isDownAdjust = false;
    [SerializeField] private bool isLeftAdjust = false;
    [SerializeField] private bool isRightAdjust = false;
    [Header("安全区额外空白大小")]
    [SerializeField][Range(0, 1.0f)] private float topBlank = 0f;
    [SerializeField][Range(0, 1.0f)] private float downBlank = 0f;
    [SerializeField][Range(0, 1.0f)] private float leftBlank = 0f;
    [SerializeField][Range(0, 1.0f)] private float rightBlank = 0f;

    void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        cts = new CancellationTokenSource();

        UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cts.Token)
               .ContinueWith(ApplySafeAreaImmediate)
               .Forget();

        ObserveSafeAreaAsync().Forget();
    }

    void OnDisable()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    void Update()
    {
#if UNITY_EDITOR
        ApplySafeAreaImmediate();
#endif
    }

    private async UniTaskVoid ObserveSafeAreaAsync()
    {
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cts.Token);

        await ScreenObserver
            .ObserveScreenChanges(cts.Token)
            .ForEachAsync(_ => ApplySafeAreaImmediate(), cts.Token);
    }

    private void ApplySafeAreaImmediate()
    {
        ApplySafeArea(Screen.safeArea);
    }

    void ApplySafeArea(Rect safeArea)
    {
        if (rectTransform == null) return;
        if (Screen.width == 0 || Screen.height == 0) return;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMax.y = isUpAdjust ? Mathf.Lerp(safeArea.yMax / Screen.height, -1, topBlank) : safeArea.yMax / Screen.height;
        anchorMin.y = isDownAdjust ? Mathf.Lerp(safeArea.yMin / Screen.height, 0, downBlank) : safeArea.yMin / Screen.height;
        anchorMin.x = isLeftAdjust ? Mathf.Lerp(safeArea.xMin / Screen.width, 0, leftBlank) : safeArea.xMin / Screen.width;
        anchorMax.x = isRightAdjust ? Mathf.Lerp(safeArea.xMax / Screen.width, 1, rightBlank) : safeArea.xMax / Screen.width;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
