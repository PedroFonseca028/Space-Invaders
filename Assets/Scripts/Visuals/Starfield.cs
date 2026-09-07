using UnityEngine;

namespace SpaceInvaders
{
    // Ambient parallax backdrop: a field of tiny twinkling stars that drift
    // downward and wrap back to the top. Purely decorative, never touches
    // gameplay state.
    public class Starfield : MonoBehaviour
    {
        private struct StarData
        {
            public Transform t;
            public SpriteRenderer sr;
            public float speed;
            public float phase;
            public float baseAlpha;
        }

        private StarData[] stars;

        public static Starfield Create(int count = 90)
        {
            var go = new GameObject("Starfield");
            var sf = go.AddComponent<Starfield>();
            sf.Build(count);
            return sf;
        }

        private void Build(int count)
        {
            stars = new StarData[count];
            var sprite = RetroSpriteFactory.GetStarSprite();
            float halfW = ScreenUtil.HalfWidth + 1f;
            float halfH = ScreenUtil.HalfHeight + 1f;

            for (int i = 0; i < count; i++)
            {
                var starGo = new GameObject("Star");
                starGo.transform.SetParent(transform, false);
                starGo.transform.position = new Vector3(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH), 0f);
                float scale = Random.Range(0.12f, 0.32f);
                starGo.transform.localScale = Vector3.one * scale;

                var sr = starGo.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = -10;
                float baseAlpha = Random.Range(0.35f, 1f);
                sr.color = new Color(1f, 1f, 1f, baseAlpha);

                stars[i] = new StarData
                {
                    t = starGo.transform,
                    sr = sr,
                    speed = Random.Range(0.3f, 1.2f) * scale * 3f,
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    baseAlpha = baseAlpha
                };
            }
        }

        private void Update()
        {
            float halfW = ScreenUtil.HalfWidth + 1f;
            float halfH = ScreenUtil.HalfHeight + 1f;
            float now = Time.time;

            for (int i = 0; i < stars.Length; i++)
            {
                var s = stars[i];
                Vector3 pos = s.t.position;
                pos.y -= s.speed * Time.deltaTime;
                if (pos.y < -halfH)
                {
                    pos.y = halfH;
                    pos.x = Random.Range(-halfW, halfW);
                }
                s.t.position = pos;

                float twinkle = 0.75f + 0.25f * Mathf.Sin(now * 2f + s.phase);
                var c = s.sr.color;
                c.a = s.baseAlpha * twinkle;
                s.sr.color = c;
            }
        }
    }
}
