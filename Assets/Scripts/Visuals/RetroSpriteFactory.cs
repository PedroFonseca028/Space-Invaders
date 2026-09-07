using System.Collections.Generic;
using UnityEngine;

namespace SpaceInvaders
{
    // Generates every sprite the game uses from small hand-authored pixel
    // patterns, entirely at runtime. Nothing here depends on an imported
    // texture asset, so the game never breaks due to a missing/broken image.
    //
    // Each creature pattern is authored as its LEFT half only (including the
    // shared center column). Mirror() reflects it into the full symmetric
    // row, which guarantees perfectly symmetric aliens without having to
    // hand-count long strings.
    public static class RetroSpriteFactory
    {
        private const float PixelsPerUnit = 10f;
        private static readonly Color32 OutlineColor = new Color32(8, 8, 18, 255);

        private static string Mirror(string half)
        {
            int n = half.Length;
            char[] rev = new char[n - 1];
            for (int i = 0; i < n - 1; i++) rev[i] = half[n - 2 - i];
            return half + new string(rev);
        }

        private static string[] BuildRows(string[] halves)
        {
            var rows = new string[halves.Length];
            for (int i = 0; i < halves.Length; i++) rows[i] = Mirror(halves[i]);
            return rows;
        }

        private static Sprite FromRows(string name, string[] rows, Dictionary<char, Color32> palette)
        {
            int height = rows.Length;
            int width = rows[0].Length;
            int texW = width + 2;
            int texH = height + 2;
            var pixels = new Color32[texW * texH];

            for (int r = 0; r < height; r++)
            {
                string row = rows[r];
                int texY = height - r; // flip: pattern row 0 (top) -> near top of texture
                for (int c = 0; c < width; c++)
                {
                    char ch = row[c];
                    if (ch == '.') continue;
                    if (!palette.TryGetValue(ch, out var col)) col = new Color32(255, 0, 255, 255);
                    pixels[texY * texW + (c + 1)] = col;
                }
            }

            // Auto-outline: any transparent pixel touching an opaque one gets a dark rim.
            var basePixels = (Color32[])pixels.Clone();
            for (int y = 0; y < texH; y++)
            {
                for (int x = 0; x < texW; x++)
                {
                    int idx = y * texW + x;
                    if (basePixels[idx].a != 0) continue;
                    bool touchesOpaque =
                        (x > 0 && basePixels[idx - 1].a != 0) ||
                        (x < texW - 1 && basePixels[idx + 1].a != 0) ||
                        (y > 0 && basePixels[idx - texW].a != 0) ||
                        (y < texH - 1 && basePixels[idx + texW].a != 0);
                    if (touchesOpaque) pixels[idx] = OutlineColor;
                }
            }

            var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = name
            };
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.name = name;
            return sprite;
        }

        private static Sprite Build(string name, string[] halves, Dictionary<char, Color32> palette)
        {
            return FromRows(name, BuildRows(halves), palette);
        }

        private static Sprite BuildFull(string name, string[] fullRows, Dictionary<char, Color32> palette)
        {
            return FromRows(name, fullRows, palette);
        }

        private static readonly Color32 EyeColor = new Color32(255, 240, 90, 255);

        // ---------------------------------------------------------------
        // Aliens (2 animation frames each, left-half patterns)
        // ---------------------------------------------------------------

        private static readonly string[] CrawlerA = { "...XX", "..XXX", ".XX.E", "XXXXX", "XX.XX", "XXX..", "..X..", ".X..." };
        private static readonly string[] CrawlerB = { "...XX", "..XXX", ".XX.E", "XXXXX", "XX.XX", "..XXX", ".X...", "X...." };
        private static readonly string[] DrifterA = { "....X", "...XX", "..XXX", ".XX.E", "XXXXX", "..X.X", ".X.X.", "X.X.." };
        private static readonly string[] DrifterB = { "....X", "...XX", "..XXX", ".XX.E", "XXXXX", ".X.X.", "..X.X", ".X.X." };
        private static readonly string[] SentinelA = { "...XXX", "..XXXX", ".XXXXX", "XXX.E.", "XXXXXX", "...XX.", "..XX.X", ".XX..." };
        private static readonly string[] SentinelB = { "...XXX", "..XXXX", ".XXXXX", "XXX.E.", "XXXXXX", "..XX..", ".XX..X", "X.X..." };

        private static Sprite[] _alien1, _alien2, _alien3;

        public static Sprite[] GetAlienFrames(int type)
        {
            switch (type)
            {
                case 1:
                    if (_alien1 == null)
                    {
                        var p = new Dictionary<char, Color32> { { 'X', new Color32(64, 220, 120, 255) }, { 'E', EyeColor } };
                        _alien1 = new[] { Build("Alien1_A", CrawlerA, p), Build("Alien1_B", CrawlerB, p) };
                    }
                    return _alien1;
                case 2:
                    if (_alien2 == null)
                    {
                        var p = new Dictionary<char, Color32> { { 'X', new Color32(80, 190, 255, 255) }, { 'E', EyeColor } };
                        _alien2 = new[] { Build("Alien2_A", DrifterA, p), Build("Alien2_B", DrifterB, p) };
                    }
                    return _alien2;
                default:
                    if (_alien3 == null)
                    {
                        var p = new Dictionary<char, Color32> { { 'X', new Color32(220, 90, 230, 255) }, { 'E', EyeColor } };
                        _alien3 = new[] { Build("Alien3_A", SentinelA, p), Build("Alien3_B", SentinelB, p) };
                    }
                    return _alien3;
            }
        }

        // ---------------------------------------------------------------
        // Boss / mystery ship
        // ---------------------------------------------------------------

        private static readonly string[] BossA = { "........X", ".....XXXX", "...XXXXXX", ".XXXXXXXX", "XX.XX.XX.", "..X..X..X", "..X.....X" };
        private static readonly string[] BossB = { "........X", ".....XXXX", "...XXXXXX", ".XXXXXXXX", "XX.XX.XX.", "..X..X..X", ".....X..." };

        private static Sprite[] _boss;
        public static Sprite[] GetBossFrames()
        {
            if (_boss == null)
            {
                var p = new Dictionary<char, Color32> { { 'X', new Color32(255, 185, 60, 255) } };
                _boss = new[] { Build("Boss_A", BossA, p), Build("Boss_B", BossB, p) };
            }
            return _boss;
        }

        // ---------------------------------------------------------------
        // Player cannon
        // ---------------------------------------------------------------

        private static readonly string[] PlayerHalf =
            { "......X", "......X", ".....XX", "....XXX", "...XXXX", "..XXXXX", ".XXXXXX", "XXXXXXX" };

        private static Sprite _player;
        public static Sprite GetPlayerSprite()
        {
            if (_player == null)
            {
                var p = new Dictionary<char, Color32> { { 'X', new Color32(225, 235, 245, 255) } };
                _player = Build("Player", PlayerHalf, p);
            }
            return _player;
        }

        // ---------------------------------------------------------------
        // Bullets
        // ---------------------------------------------------------------

        private static readonly string[] PlayerBulletHalf = { ".X", "XX", "XX", "XX", ".X" };

        private static Sprite _playerBullet;
        public static Sprite GetPlayerBulletSprite()
        {
            if (_playerBullet == null)
            {
                var p = new Dictionary<char, Color32> { { 'X', new Color32(255, 240, 150, 255) } };
                _playerBullet = Build("PlayerBullet", PlayerBulletHalf, p);
            }
            return _playerBullet;
        }

        private static readonly string[][] AlienBulletFull =
        {
            new[] { "X..", ".X.", "..X", ".X.", "X..", ".X." },
            new[] { "..X", ".X.", "X..", ".X.", "..X", ".X." }
        };

        private static Sprite[] _alienBullet;
        public static Sprite[] GetAlienBulletFrames()
        {
            if (_alienBullet == null)
            {
                var p = new Dictionary<char, Color32> { { 'X', new Color32(255, 80, 70, 255) } };
                _alienBullet = new[]
                {
                    BuildFull("AlienBullet_A", AlienBulletFull[0], p),
                    BuildFull("AlienBullet_B", AlienBulletFull[1], p)
                };
            }
            return _alienBullet;
        }

        // ---------------------------------------------------------------
        // Explosion (impact flash -> fading sparks)
        // ---------------------------------------------------------------

        private static readonly string[] ExplosionFlashHalf = { "...X", "..XX", ".XXX", "XXXX" };
        private static readonly string[] ExplosionSparkHalf = { "...X", "..X.", ".X.X", "X..X" };

        private static Sprite[] _explosion;
        public static Sprite[] GetExplosionFrames()
        {
            if (_explosion == null)
            {
                var flashP = new Dictionary<char, Color32> { { 'X', new Color32(255, 230, 120, 255) } };
                var sparkP = new Dictionary<char, Color32> { { 'X', new Color32(230, 110, 30, 255) } };
                _explosion = new[]
                {
                    Build("Explosion_Flash", ExplosionFlashHalf, flashP),
                    Build("Explosion_Spark", ExplosionSparkHalf, sparkP)
                };
            }
            return _explosion;
        }

        // ---------------------------------------------------------------
        // Shield block
        // ---------------------------------------------------------------

        private static readonly string[] ShieldBlockFull = { "XXXX", "XOOX", "XOOX", "XXXX" };

        private static Sprite _shieldBlock;
        public static Sprite GetShieldBlockSprite()
        {
            if (_shieldBlock == null)
            {
                var p = new Dictionary<char, Color32>
                {
                    { 'X', new Color32(90, 220, 110, 255) },
                    { 'O', new Color32(60, 180, 85, 255) }
                };
                _shieldBlock = BuildFull("ShieldBlock", ShieldBlockFull, p);
            }
            return _shieldBlock;
        }

        // ---------------------------------------------------------------
        // Star (background twinkle)
        // ---------------------------------------------------------------

        private static readonly string[] StarFull = { ".X.", "XXX", ".X." };

        private static Sprite _star;
        public static Sprite GetStarSprite()
        {
            if (_star == null)
            {
                var p = new Dictionary<char, Color32> { { 'X', new Color32(235, 235, 255, 255) } };
                _star = BuildFull("Star", StarFull, p);
            }
            return _star;
        }
    }
}
