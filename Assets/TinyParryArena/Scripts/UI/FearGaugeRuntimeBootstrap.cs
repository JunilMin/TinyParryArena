using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyParryArena.UI
{
    public static class FearGaugeRuntimeBootstrap
    {
        private const string GaugeObjectName = "__TinyParryArena_FearGauge";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGaugeExists()
        {
            if (GameObject.Find(GaugeObjectName) != null)
            {
                return;
            }

            Canvas canvas = FindHudCanvas();

            if (canvas == null)
            {
                GameObject canvasObject = new GameObject(
                    "__TinyParryArena_HUD",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

                Object.DontDestroyOnLoad(canvasObject);

                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            CreateGauge(canvas.transform);
        }

        private static Canvas FindHudCanvas()
        {
#if UNITY_2023_1_OR_NEWER
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
#else
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
#endif
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return canvas;
                }
            }

            return null;
        }

        private static void CreateGauge(Transform parent)
        {
            GameObject root = new GameObject(
                GaugeObjectName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));

            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 64f);
            rootRect.sizeDelta = new Vector2(560f, 26f);

            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.02f, 0.02f, 0.02f, 0.92f);
            background.raycastTarget = false;

            GameObject inner = new GameObject(
                "Inner",
                typeof(RectTransform),
                typeof(Image));

            inner.transform.SetParent(root.transform, false);

            RectTransform innerRect = inner.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3f, 3f);
            innerRect.offsetMax = new Vector2(-3f, -3f);

            Image innerBackground = inner.GetComponent<Image>();
            innerBackground.color = new Color(0.09f, 0.09f, 0.09f, 1f);
            innerBackground.raycastTarget = false;

            GameObject fillObject = new GameObject(
                "Fill",
                typeof(RectTransform),
                typeof(Image));

            fillObject.transform.SetParent(inner.transform, false);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fill = fillObject.GetComponent<Image>();
            fill.color = new Color(0.45f, 0.45f, 0.45f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;

            GameObject flashObject = new GameObject(
                "Flash",
                typeof(RectTransform),
                typeof(Image));

            flashObject.transform.SetParent(inner.transform, false);

            RectTransform flashRect = flashObject.GetComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;

            Image flash = flashObject.GetComponent<Image>();
            flash.color = new Color(1f, 1f, 1f, 0f);
            flash.raycastTarget = false;

            FearGaugeUI gauge = root.AddComponent<FearGaugeUI>();
            gauge.Initialize(background, fill, flash, rootRect);
        }
    }
}
