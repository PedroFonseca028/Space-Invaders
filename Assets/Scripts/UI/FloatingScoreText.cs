using UnityEngine;

namespace SpaceInvaders
{
    // A small "+30" style popup that floats up and fades out where an
    // alien or the boss was destroyed.
    public class FloatingScoreText : MonoBehaviour
    {
        private const float Life = 0.8f;
        private float t;
        private TextMesh tm;
        private Color baseColor;

        public static void Create(Vector3 worldPos, string text, Color color)
        {
            var go = new GameObject("ScorePopup");
            go.transform.position = worldPos;

            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tm.font = font;
            tm.fontSize = 48;
            tm.characterSize = 0.09f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.text = text;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) mr = go.AddComponent<MeshRenderer>();
            if (font != null) mr.material = font.material;
            mr.sortingOrder = 20;

            var popup = go.AddComponent<FloatingScoreText>();
            popup.tm = tm;
            popup.baseColor = color;
        }

        private void Update()
        {
            t += Time.deltaTime;
            transform.position += Vector3.up * (1.2f * Time.deltaTime);

            float a = Mathf.Clamp01(1f - t / Life);
            var c = baseColor;
            c.a = a;
            tm.color = c;

            if (t >= Life) Destroy(gameObject);
        }
    }
}
