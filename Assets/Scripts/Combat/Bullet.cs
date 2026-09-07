using System.Collections.Generic;
using UnityEngine;

namespace SpaceInvaders
{
    // Simple transform-driven projectile. Collisions are resolved centrally by
    // GameManager via plain AABB checks against these static registries -
    // there is no Physics2D involved anywhere in this project.
    public class Bullet : MonoBehaviour
    {
        public enum Owner { Player, Alien }

        private Owner owner;
        private float speed = 12f;
        private Vector2 halfSize = new Vector2(0.15f, 0.3f);

        public static readonly List<Bullet> PlayerBullets = new List<Bullet>();
        public static readonly List<Bullet> AlienBullets = new List<Bullet>();

        private SpriteRenderer _sr;
        private Sprite[] _animFrames;
        private float _frameTime = 0.08f;
        private float _animTimer;
        private int _animIndex;

        public Bounds WorldBounds => new Bounds(transform.position, new Vector3(halfSize.x * 2f, halfSize.y * 2f, 1f));

        public static Bullet Spawn(Owner owner, Vector3 position, float speed, Sprite sprite, Vector2 halfSize)
        {
            var go = new GameObject(owner == Owner.Player ? "PlayerBullet" : "AlienBullet");
            go.transform.position = position;
            var bullet = go.AddComponent<Bullet>();
            bullet.owner = owner;
            bullet.speed = speed;
            bullet.halfSize = halfSize;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;
            bullet._sr = sr;
            return bullet;
        }

        public void SetAnimFrames(Sprite[] frames, float frameTime = 0.08f)
        {
            _animFrames = frames;
            _frameTime = frameTime;
        }

        private void OnEnable()
        {
            (owner == Owner.Player ? PlayerBullets : AlienBullets).Add(this);
        }

        private void OnDisable()
        {
            (owner == Owner.Player ? PlayerBullets : AlienBullets).Remove(this);
        }

        private void Update()
        {
            float dir = owner == Owner.Player ? 1f : -1f;
            transform.position += Vector3.up * (dir * speed * Time.deltaTime);

            if (_animFrames != null && _animFrames.Length > 1)
            {
                _animTimer += Time.deltaTime;
                if (_animTimer >= _frameTime)
                {
                    _animTimer = 0f;
                    _animIndex = (_animIndex + 1) % _animFrames.Length;
                    _sr.sprite = _animFrames[_animIndex];
                }
            }

            float halfH = ScreenUtil.HalfHeight;
            float y = transform.position.y;
            if (y > halfH + 1f || y < -halfH - 1f)
            {
                Destroy(gameObject);
            }
        }

        public static void DestroyAll()
        {
            foreach (var b in PlayerBullets.ToArray()) if (b != null) Destroy(b.gameObject);
            foreach (var b in AlienBullets.ToArray()) if (b != null) Destroy(b.gameObject);
            PlayerBullets.Clear();
            AlienBullets.Clear();
        }

        public static void DestroyAlienBullets()
        {
            foreach (var b in AlienBullets.ToArray()) if (b != null) Destroy(b.gameObject);
            AlienBullets.Clear();
        }
    }
}
