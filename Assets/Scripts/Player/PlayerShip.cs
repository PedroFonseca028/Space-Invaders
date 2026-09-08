using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceInvaders
{
    public class PlayerShip : MonoBehaviour
    {
        public float moveSpeed = 9f;
        public float bulletSpeed = 14f;
        public float fireCooldown = 0.25f;
        public readonly Vector2 halfSize = new Vector2(0.7f, 0.5f);

        private SpriteRenderer _sr;
        private float _cooldownTimer;
        private bool _invulnerable;
        private float _invulnTimer;
        private float _blinkTimer;
        private Bullet _activeBullet;

        public Bounds WorldBounds => new Bounds(transform.position, new Vector3(halfSize.x * 2f, halfSize.y * 2f, 1f));

        public static PlayerShip Create(Vector3 position)
        {
            var go = new GameObject("Player");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RetroSpriteFactory.GetPlayerSprite();
            sr.sortingOrder = 10;
            var ship = go.AddComponent<PlayerShip>();
            ship._sr = sr;
            return ship;
        }

        public void SetActive(bool active)
        {
            enabled = active;
        }

        private void Update()
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
            HandleMovement();
            HandleShooting();
            HandleInvulnerability();
        }

        private void HandleMovement()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            float dir = 0f;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir += 1f;

            float halfW = ScreenUtil.HalfWidth;
            float minX = -halfW + halfSize.x + 0.15f;
            float maxX = halfW - halfSize.x - 0.15f;
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x + dir * moveSpeed * Time.deltaTime, minX, maxX);
            transform.position = pos;
        }

        private void HandleShooting()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (_activeBullet == null && _cooldownTimer <= 0f && kb.spaceKey.isPressed)
            {
                Vector3 spawnPos = transform.position + Vector3.up * (halfSize.y + 0.3f);
                _activeBullet = Bullet.Spawn(Bullet.Owner.Player, spawnPos, bulletSpeed,
                    RetroSpriteFactory.GetPlayerBulletSprite(), new Vector2(0.12f, 0.25f));
                _cooldownTimer = fireCooldown;
                RetroAudio.PlayShoot();
            }
        }

        private void HandleInvulnerability()
        {
            if (!_invulnerable) return;
            _invulnTimer -= Time.deltaTime;
            _blinkTimer -= Time.deltaTime;
            if (_blinkTimer <= 0f)
            {
                _blinkTimer = 0.1f;
                _sr.enabled = !_sr.enabled;
            }
            if (_invulnTimer <= 0f)
            {
                _invulnerable = false;
                _sr.enabled = true;
            }
        }

        // Returns true if the hit actually registers (player wasn't already invulnerable).
        public bool TryTakeHit()
        {
            if (_invulnerable) return false;
            _invulnerable = true;
            _invulnTimer = 1.6f;
            _blinkTimer = 0f;
            return true;
        }

        public void ResetToStart()
        {
            float halfH = ScreenUtil.HalfHeight;
            transform.position = new Vector3(0f, -halfH + 1f, 0f);
            _invulnerable = true;
            _invulnTimer = 1.6f;
            _sr.enabled = true;
            _activeBullet = null;
        }
    }
}
