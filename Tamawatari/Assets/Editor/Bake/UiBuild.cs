using UnityEngine;
using UnityEngine.UI;

namespace Tamawatari.Bake
{
    /// <summary>
    /// UI プレハブを組み立てるための uGUI ヘルパ(エディタ専用)。
    /// もともとランタイムで画面を組んでいたコードをそのまま持ってきたもので、
    /// 出来上がりは <c>Assets/Prefabs/UI</c> のプレハブとして保存される。
    /// </summary>
    public static class UiBuild
    {
        /// <summary>焼くときに使うスプライトとフォント。<see cref="AssetBaker"/> が入れる。</summary>
        public static BakeSprites.Result Sprites;
        public static Font BodyFont;
        public static Font DisplayFont;

        public static Canvas CreateCanvas(string name, int sortingOrder = 100)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform CreateRect(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        /// <summary>display=true で見出し用フォント。</summary>
        public static Text CreateText(Transform parent, string content, int fontSize, TextAnchor anchor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color? color = null,
            bool display = false)
        {
            var rt = CreateRect(parent, "Text", anchorMin, anchorMax, offsetMin, offsetMax);
            var txt = rt.gameObject.AddComponent<Text>();
            txt.font = display ? DisplayFont : BodyFont;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = color ?? Color.white;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        public static Image CreateImage(Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            string name = "Image", Sprite sprite = null)
        {
            var rt = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null) img.sprite = sprite;
            return img;
        }

        /// <summary>親の中心を基準に、指定位置・サイズの矩形/円 Image を作る(UI パーツ組み立て用)。</summary>
        public static Image CreateBox(Transform parent, Color color, Vector2 center, Vector2 size,
            string name = "Box", bool circle = false, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = center;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null) img.sprite = sprite;
            else if (circle) img.sprite = Sprites.Circle;
            return img;
        }

        /// <summary>円形ゲージ(中心から時計回りに満ちる)にする。</summary>
        public static void MakeRadial(Image img)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = true;
            img.fillAmount = 0f;
        }
    }
}
