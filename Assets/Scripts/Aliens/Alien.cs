using System.Collections;
using UnityEngine;

namespace SpaceInvaders
{
    public class Alien : MonoBehaviour
    {
        public int type;
        public int scoreValue;
        public int row;
        public int col;
        public SpriteRenderer sr;

        public bool IsDying { get; private set; }

        public readonly Vector2 halfSize = new Vector2(0.5f, 0.45f);
        public Bounds WorldBounds => new Bounds(transform.position, new Vector3(halfSize.x * 2f, halfSize.y * 2f, 1f));

        public static Alien Create(Transform parent, int type, int scoreValue, int row, int col, Vector3 localPos, Sprite initialSprite)
        {
            var go = new GameObject("Alien" + type + "_" + row + "_" + col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = initialSprite;
            sr.sortingOrder = 3;
            var alien = go.AddComponent<Alien>();
            alien.type = type;
            alien.scoreValue = scoreValue;
            alien.row = row;
            alien.col = col;
            alien.sr = sr;
            return alien;
        }

        public void SetFrame(Sprite s)
        {
            if (sr != null) sr.sprite = s;
        }

        public void Die(System.Action onComplete)
        {
            if (IsDying) return;
            IsDying = true;
            StartCoroutine(DieRoutine(onComplete));
        }

        private IEnumerator DieRoutine(System.Action onComplete)
        {
            var frames = RetroSpriteFactory.GetExplosionFrames();
            sr.sprite = frames[0];
            yield return new WaitForSeconds(0.07f);
            sr.sprite = frames[1];
            yield return new WaitForSeconds(0.07f);
            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}
