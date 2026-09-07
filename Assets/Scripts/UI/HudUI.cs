using UnityEngine;
using UnityEngine.UI;

namespace SpaceInvaders
{
    // Builds the whole HUD (top bar, lives row, center message panel) purely
    // from code with plain uGUI Text/Image elements, so it needs no scene
    // wiring, no TextMeshPro import step and no prefabs.
    public class HudUI : MonoBehaviour
    {
        private const int MaxLifeIcons = 6;

        private Text scoreText;
        private Text highScoreText;
        private Text levelText;
        private Text titleText;
        private Text subtitleText;
        private Text promptText;
        private Image[] lifeIcons;
        private GameObject centerPanel;
        private float promptBlinkTimer;
        private Font font;

        public static HudUI Create()
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HudUI>();
            hud.Build();
            return hud;
        }

        private Font GetFont()
        {
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return font;
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            BuildTopBar();
            BuildBottomBar();
            BuildCenterPanel();
        }

        private Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor,
            Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var text = go.AddComponent<Text>();
            text.font = GetFont();
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.1f;
            return text;
        }

        private void BuildTopBar()
        {
            var bar = new GameObject("TopBar");
            bar.transform.SetParent(transform, false);
            var rt = bar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0, 70);
            rt.anchoredPosition = Vector2.zero;

            scoreText = CreateText(bar.transform, "Score", "SCORE\n00000", 20, TextAnchor.UpperLeft, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -10), new Vector2(260, 60));

            highScoreText = CreateText(bar.transform, "HighScore", "HI-SCORE\n00000", 20, TextAnchor.UpperCenter,
                new Color(1f, 0.85f, 0.3f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(260, 60));

            levelText = CreateText(bar.transform, "Level", "WAVE 1/5", 20, TextAnchor.UpperRight, Color.white,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -10), new Vector2(260, 60));
        }

        private void BuildBottomBar()
        {
            var bar = new GameObject("BottomBar");
            bar.transform.SetParent(transform, false);
            var rt = bar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0, 56);
            rt.anchoredPosition = Vector2.zero;

            var livesRowGo = new GameObject("LivesRow");
            livesRowGo.transform.SetParent(bar.transform, false);
            var rowRt = livesRowGo.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 0);
            rowRt.anchorMax = new Vector2(0, 0);
            rowRt.pivot = new Vector2(0, 0);
            rowRt.anchoredPosition = new Vector2(20, 12);
            rowRt.sizeDelta = new Vector2(300, 34);

            lifeIcons = new Image[MaxLifeIcons];
            var playerSprite = RetroSpriteFactory.GetPlayerSprite();
            for (int i = 0; i < MaxLifeIcons; i++)
            {
                var iconGo = new GameObject("Life" + i);
                iconGo.transform.SetParent(rowRt, false);
                var iconRt = iconGo.AddComponent<RectTransform>();
                iconRt.sizeDelta = new Vector2(26, 26);
                iconRt.anchorMin = new Vector2(0, 0);
                iconRt.anchorMax = new Vector2(0, 0);
                iconRt.pivot = new Vector2(0, 0);
                iconRt.anchoredPosition = new Vector2(i * 32, 0);
                var img = iconGo.AddComponent<Image>();
                img.sprite = playerSprite;
                img.preserveAspect = true;
                lifeIcons[i] = img;
            }

            CreateText(bar.transform, "ControlsHint", "<- -> / A D MOVER    ESPACO ATIRAR    P PAUSA", 14,
                TextAnchor.LowerRight, new Color(0.75f, 0.8f, 0.9f, 0.85f),
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 14), new Vector2(460, 26));
        }

        private void BuildCenterPanel()
        {
            centerPanel = new GameObject("CenterPanel");
            centerPanel.transform.SetParent(transform, false);
            var rt = centerPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bgGo = new GameObject("Backdrop");
            bgGo.transform.SetParent(centerPanel.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0.02f, 0.6f);

            titleText = CreateText(centerPanel.transform, "Title", "SPACE INVADERS", 52, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1000, 80));

            subtitleText = CreateText(centerPanel.transform, "Subtitle", "", 22, TextAnchor.MiddleCenter, new Color(0.8f, 0.85f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1000, 160));

            promptText = CreateText(centerPanel.transform, "Prompt", "PRESSIONE ESPACO", 22, TextAnchor.MiddleCenter,
                new Color(1f, 0.9f, 0.3f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(1000, 50));

            centerPanel.SetActive(false);
        }

        public void SetScore(int score) => scoreText.text = "SCORE\n" + score.ToString("D5");
        public void SetHighScore(int hs) => highScoreText.text = "HI-SCORE\n" + hs.ToString("D5");
        public void SetLevel(int level, int maxLevel) => levelText.text = "WAVE " + level + "/" + maxLevel;

        public void SetLives(int lives)
        {
            for (int i = 0; i < lifeIcons.Length; i++) lifeIcons[i].gameObject.SetActive(i < lives);
        }

        public void ShowMessage(string title, string subtitle, string prompt)
        {
            centerPanel.SetActive(true);
            titleText.text = title;
            subtitleText.text = subtitle;
            promptText.text = prompt;
            promptText.gameObject.SetActive(!string.IsNullOrEmpty(prompt));
            promptBlinkTimer = 0f;
        }

        public void HideMessage() => centerPanel.SetActive(false);

        private void Update()
        {
            if (centerPanel == null || !centerPanel.activeSelf || !promptText.gameObject.activeSelf) return;

            // Uses unscaled time so the "press space" prompt keeps blinking
            // even while gameplay is frozen (Time.timeScale = 0 on pause).
            promptBlinkTimer += Time.unscaledDeltaTime;
            float a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(promptBlinkTimer * 3f));
            var c = promptText.color;
            c.a = a;
            promptText.color = c;
        }
    }
}
