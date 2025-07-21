using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;

/// <summary>
/// 观察屏幕变化的静态类
/// </summary>
public static class ScreenObserver
{
    #region 基础观察

    /// <summary>
    /// 观察屏幕安全区域和屏幕角度变化
    /// </summary>
    public static IUniTaskAsyncEnumerable<(Rect safeArea, ScreenOrientation orientation)> ObserveScreenChanges(CancellationToken token = default)
    {
        return UniTaskAsyncEnumerable.EveryUpdate()
            .Select(_ => (Screen.safeArea, Screen.orientation))
            .DistinctUntilChanged();
    }

    /// <summary>
    /// 观察屏幕安全区域变化
    /// </summary>
    public static IUniTaskAsyncEnumerable<Rect> ObserveSafeArea(CancellationToken token = default)
    {
        return ObserveScreenChanges(token = default)
            .Select(x => x.safeArea);
    }

    /// <summary>
    /// 观察屏幕角度变化
    /// </summary>
    public static IUniTaskAsyncEnumerable<ScreenOrientation> ObserveOrientation(CancellationToken token = default)
    {
        return ObserveScreenChanges(token = default)
            .Select(x => x.orientation);
    }

    /// <summary>
    /// 观察屏幕分辨率变化
    /// </summary>
    public static IUniTaskAsyncEnumerable<Vector2Int> ObserveResolution(CancellationToken token = default)
    {
        return UniTaskAsyncEnumerable.EveryUpdate()
            .Select(_ => new Vector2Int(Screen.width, Screen.height))
            .DistinctUntilChanged();
    }

    /// <summary>
    /// 观察屏幕宽高比变化
    /// </summary>
    public static IUniTaskAsyncEnumerable<float> ObserveAspectRatio(CancellationToken token = default)
    {
        return ObserveResolution(token = default)
            .Select(res => (float)res.x / res.y);
    }

    #endregion

    #region 高级观察

    /// <summary>
    /// 观察屏幕是否处于竖屏模式
    /// </summary>
    public static IUniTaskAsyncEnumerable<bool> ObserveIsPortrait(CancellationToken token = default)
    {
        return ObserveOrientation(token = default)
            .Select(orientation =>
                orientation == ScreenOrientation.Portrait ||
                orientation == ScreenOrientation.PortraitUpsideDown);
    }

    /// <summary>
    /// 观察屏幕是否处于横屏模式
    /// </summary>  
    public static IUniTaskAsyncEnumerable<bool> ObserveIsLandscape(CancellationToken token = default)
    {
        return ObserveOrientation(token = default)
            .Select(orientation =>
                orientation == ScreenOrientation.LandscapeLeft ||
                orientation == ScreenOrientation.LandscapeRight);
    }

    /// <summary>
    /// 观察屏幕安全区域与屏幕高度的比例（用于刘海屏适配）
    /// </summary>
    public static IUniTaskAsyncEnumerable<float> ObserveSafeAreaTopRatio(CancellationToken token = default)
    {
        return ObserveSafeArea(token = default)
            .Select(safeArea => safeArea.yMax / Screen.height);
    }

    #endregion

    #region 条件观察

    /// <summary>
    /// 等待直到屏幕进入竖屏模式
    /// </summary>
    public static UniTask WaitUntilPortrait(CancellationToken token = default)
    {
        return ObserveIsPortrait(token = default)
            .FirstOrDefaultAsync(isPortrait => isPortrait, token = default)
            .AsUniTask();
    }

    /// <summary>
    /// 等待直到屏幕进入横屏模式
    /// </summary>
    public static UniTask WaitUntilLandscape(CancellationToken token = default)
    {
        return ObserveIsLandscape(token = default)
            .FirstOrDefaultAsync(isLandscape => isLandscape, token = default)
            .AsUniTask();
    }

    /// <summary>
    /// 等待直到屏幕宽高比超过阈值
    /// </summary>
    public static UniTask WaitUntilAspectRatioGreaterThan(float threshold, CancellationToken token = default)
    {
        return ObserveAspectRatio(token = default)
            .FirstOrDefaultAsync(ratio => ratio > threshold, token = default)
            .AsUniTask();
    }

    #endregion

    #region 焦点观察

    /// <summary>
    /// 观察应用焦点状态变化（前台/后台）
    /// </summary>
    public static IUniTaskAsyncEnumerable<bool> ObserveAppFocus(CancellationToken token = default)
    {
        return UniTaskAsyncEnumerable.Create<bool>(async (writer, token) =>
        {
            bool? lastValue = null;

            await foreach (var _ in UniTaskAsyncEnumerable.EveryUpdate(PlayerLoopTiming.Update).WithCancellation(token))
            {
                var currentFocus = Application.isFocused;
                if (lastValue == null || lastValue != currentFocus)
                {
                    lastValue = currentFocus;
                    await writer.YieldAsync(currentFocus);
                }
            }
        });
    }

    /// <summary>
    /// 观察应用是否在前台（聚焦状态）
    /// </summary>
    public static IUniTaskAsyncEnumerable<bool> ObserveIsAppInForeground(CancellationToken token = default)
    {
        return ObserveAppFocus(token = default);
    }

    /// <summary>
    /// 观察应用是否在后台（失去焦点状态）
    /// </summary>
    public static IUniTaskAsyncEnumerable<bool> ObserveIsAppInBackground(CancellationToken token = default)
    {
        return ObserveAppFocus(token = default)
            .Select(focused => !focused);
    }

    /// <summary>
    /// 等待直到应用回到前台
    /// </summary>
    public static UniTask WaitUntilAppForeground(CancellationToken token = default)
    {
        return ObserveIsAppInForeground(token = default)
            .FirstOrDefaultAsync(isForeground => isForeground, token = default)
            .AsUniTask();
    }

    /// <summary>
    /// 等待直到应用进入后台
    /// </summary>
    public static UniTask WaitUntilAppBackground(CancellationToken token = default)
    {
        return ObserveIsAppInBackground(token = default)
            .FirstOrDefaultAsync(isBackground => isBackground, token = default)
            .AsUniTask();
    }

    #endregion

    #region 前台观察


    /// <summary>
    /// 观察当应用在前台时的    屏幕安全区域变化
    /// </summary>
    public static IUniTaskAsyncEnumerable<Rect> ObserveSafeAreaWhenForeground(CancellationToken token = default)
    {
        return ObserveSafeArea(token = default)
            .Where(_ => Application.isFocused);
    }

    /// <summary>
    /// 观察当应用在前台时的    屏幕方向变化
    /// </summary>
    public static IUniTaskAsyncEnumerable<ScreenOrientation> ObserveOrientationWhenForeground(CancellationToken token = default)
    {
        return ObserveOrientation(token = default)
            .Where(_ => Application.isFocused);
    }

    #endregion

    #region 屏幕信息

    /// <summary>
    /// 获取当前屏幕信息快照
    /// </summary>
    public static ScreenInfo GetCurrentScreenInfo()
    {
        return new ScreenInfo(
            Screen.safeArea,
            Screen.orientation,
            new Vector2Int(Screen.width, Screen.height),
            Application.isFocused,
            Application.isMobilePlatform);
    }

    /// <summary>
    /// 屏幕信息结构体
    /// </summary>
    public readonly struct ScreenInfo
    {
        public readonly Rect safeArea;
        public readonly ScreenOrientation orientation;
        public readonly Vector2Int resolution;
        public readonly bool isAppFocused;
        public readonly bool isMobile;

        public ScreenInfo(Rect safeArea, ScreenOrientation orientation, Vector2Int resolution, bool isAppFocused, bool isMobile)
        {
            this.safeArea = safeArea;
            this.orientation = orientation;
            this.resolution = resolution;
            this.isAppFocused = isAppFocused;
            this.isMobile = isMobile;
        }

        //屏幕宽高比
        public float AspectRatio => (float)resolution.x / resolution.y;
        //竖屏模式
        public bool IsPortrait => orientation == ScreenOrientation.Portrait ||
                                 orientation == ScreenOrientation.PortraitUpsideDown;
        //横屏模式
        public bool IsLandscape => orientation == ScreenOrientation.LandscapeLeft ||
                                  orientation == ScreenOrientation.LandscapeRight;
    }

    #endregion

}
