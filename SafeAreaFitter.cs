using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect safeAreaRect;
    private Rect lastSafeAreaRect;
    private ScreenOrientation lastScreenOrientation = ScreenOrientation.AutoRotation;

    IEnumerator Start()
    {
        rectTransform = GetComponent<RectTransform>();

        //wait renderer
        yield return new WaitForEndOfFrame();
        ApplySafeArea();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeAreaRect || Screen.orientation != lastScreenOrientation)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        safeAreaRect = Screen.safeArea;
        lastScreenOrientation = Screen.orientation;

        Vector2 anchorMin = safeAreaRect.position;
        Vector2 anchorMax = safeAreaRect.position + safeAreaRect.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
