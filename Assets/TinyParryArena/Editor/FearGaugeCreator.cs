#if UNITY_EDITOR
using TinyParryArena.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TinyParryArena.Editor
{
    public static class FearGaugeCreator
    {
        [MenuItem("GameObject/Tiny Parry Arena/Create Fear Gauge", false, 10)]
        public static void CreateFearGauge()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                GameObject canvasObject = new GameObject(
                    "HUD Canvas",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);

                Undo.RegisterCreatedObjectUndo(canvasObject, "Create HUD Canvas");
            }

            GameObject root = new GameObject(
                "Fear Gauge",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(FearGaugeUI));

            root.transform.SetParent(canvas.transform, false);
            Undo.RegisterCreatedObjectUndo(root, "Create Fear Gauge");

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 72f);
            rootRect.sizeDelta = new Vector2(520f, 24f);

            Image background = root.GetComponent<Image>();
            background.color = new Color(0.04f, 0.04f, 0.04f, 0.85f);
            background.raycastTarget = false;

            GameObject fillObject = new GameObject(
                "Fill",
                typeof(RectTransform),
                typeof(Image));

            fillObject.transform.SetParent(root.transform, false);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);

            Image fill = fillObject.GetComponent<Image>();
            fill.color = new Color(0.72f, 0.08f, 0.08f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;

            FearGaugeUI gauge = root.GetComponent<FearGaugeUI>();
            SerializedObject serializedGauge = new SerializedObject(gauge);
            serializedGauge.FindProperty("fillImage").objectReferenceValue = fill;
            serializedGauge.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }
    }
}
#endif
