using System.Collections;
using UnityEngine;

namespace SpaceInvaders
{
    // The "chefe" mystery ship: spawns top-left at a random interval,
    // crosses to the right at a constant speed and disappears, per the spec.
    public class BossShip : MonoBehaviour
    {
        public float speed = 4.5f;
        public int scoreValue = 50;

        private SpriteRenderer sr;
        private AudioSource hum;
        private float animTimer;
        private int frameIndex;

        public bool IsDying { get; private set; }

        public readonly Vector2 halfSize = new Vector2(0.95f, 0.45f);
        public Bounds WorldBounds => new Bounds(transform.position, new Vector3(halfSize.x * 2f, halfSize.y * 2f, 1f));

        public static BossShip Create(Vector3 startPos, float speed)
        {
            var go = new GameObject("BossShip");
            go.transform.position = startPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RetroSpriteFactory.GetBossFrames()[0];
            sr.sortingOrder = 6;

            var boss = go.AddComponent<BossShip>();
            boss.sr = sr;
            boss.speed = speed;

            var src = go.AddComponent<AudioSource>();
            src.clip = RetroAudio.GetBossHumClip();
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0.55f;
            src.Play();
            boss.hum = src;

            return boss;
        }

        private void Update()
        {
            if (IsDying) return;

            transform.position += Vector3.right * (speed * Time.deltaTime);

            animTimer += Time.deltaTime;
            if (animTimer >= 0.15f)
            {
                animTimer = 0f;
                frameIndex = 1 - frameIndex;
                sr.sprite = RetroSpriteFactory.GetBossFrames()[frameIndex];
            }

            if (transform.position.x > ScreenUtil.HalfWidth + 1.5f)
            {
                Destroy(gameObject);
            }
        }

        public void Explode(System.Action onComplete)
        {
            if (IsDying) return;
            IsDying = true;
            if (hum != null) hum.Stop();
            RetroAudio.PlayBossExplode();
            StartCoroutine(ExplodeRoutine(onComplete));
        }

        private IEnumerator ExplodeRoutine(System.Action onComplete)
        {
            var frames = RetroSpriteFactory.GetExplosionFrames();
            transform.localScale = Vector3.one * 1.5f;
            sr.sprite = frames[0];
            yield return new WaitForSeconds(0.1f);
            sr.sprite = frames[1];
            yield return new WaitForSeconds(0.1f);
            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}
