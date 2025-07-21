using UnityEngine;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.Threading;

/// <summary>
/// 屏幕安全区，作为所有UI的父类，RectTransform需设置为全屏
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    [Header("adjust safe area ?")]
    [SerializeField] private bool isUpAdjust = false;
    [SerializeField] private bool isDownAdjust = false;
    [SerializeField] private bool isLeftAdjust = false;
    [SerializeField] private bool isRightAdjust = false;

    [Header("safe area offset")]
    [SerializeField][Range(0, 1.0f)] private float topBlank = 0f;
    [SerializeField][Range(0, 1.0f)] private float downSize = 0f;
    [SerializeField][Range(0, 1.0f)] private float leftSize = 0f;
    [SerializeField][Range(0, 1.0f)] private float rightSize = 0f;


    private RectTransform _rectTransform;
    private CancellationTokenSource _cts;
    private Rect _lastSafeArea;

#if UNITY_EDITOR
    private float _lastUpdateTime;
    private const float EDITOR_UPDATE_INTERVAL = 0.1f;
#endif

    void OnEnable()
    {
        //init
        _rectTransform = GetComponent<RectTransform>();
        _cts = new CancellationTokenSource();
        _lastSafeArea = new Rect();

        UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, _cts.Token)
               .ContinueWith(ApplySafeAreaImmediate)
               .Forget();

        ObserveSafeAreaAsync().Forget();
    }

    void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Time.realtimeSinceStartup - _lastUpdateTime > EDITOR_UPDATE_INTERVAL)
        {
            ApplySafeAreaImmediate();
            _lastUpdateTime = Time.realtimeSinceStartup;
        }
    }
#endif

    private async UniTaskVoid ObserveSafeAreaAsync()
    {
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, _cts.Token);

        await ScreenObserver
            .ObserveScreenChanges(_cts.Token)
            .ForEachAsync(screenData =>
                {
                    //只在安全区域真正变化时才应用
                    if (screenData.safeArea != _lastSafeArea)
                    {
                        ApplySafeAreaImmediate();
                        _lastSafeArea = screenData.safeArea;
                    }
                }, _cts.Token);
    }

    private void ApplySafeAreaImmediate()
    {
        if (_rectTransform == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        ApplySafeArea(Screen.safeArea);
    }

    void ApplySafeArea(Rect safeArea)
    {
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMax.y = isUpAdjust ? Mathf.Lerp(safeArea.yMax / Screen.height, -1, topBlank) : safeArea.yMax / Screen.height;
        anchorMin.y = isDownAdjust ? Mathf.Lerp(safeArea.yMin / Screen.height, 0, downSize) : safeArea.yMin / Screen.height;
        anchorMin.x = isLeftAdjust ? Mathf.Lerp(safeArea.xMin / Screen.width, 0, leftSize) : safeArea.xMin / Screen.width;
        anchorMax.x = isRightAdjust ? Mathf.Lerp(safeArea.xMax / Screen.width, 1, rightSize) : safeArea.xMax / Screen.width;

        //应用锚点
        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;

        //重置偏移，让UI完全基于锚点
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
    }
}
