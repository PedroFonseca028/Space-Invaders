using System.Collections.Generic;
using UnityEngine;

namespace SpaceInvaders
{
    // A destructible bunker made of small blocks. Any bullet (player or
    // alien) that touches a block destroys it; aliens marching through the
    // shield's row erode it the same way.
    public class Shield : MonoBehaviour
    {
        private const float BlockSize = 0.6f;
        private static readonly string[] Pattern =
        {
            "XXXXXXX",
            "XXXXXXX",
            "XXXXXXX",
            "XX...XX"
        };

        private readonly List<Transform> blocks = new List<Transform>();
        private int patternRows, patternCols;

        public Bounds ApproxBounds => new Bounds(transform.position, new Vector3(patternCols * BlockSize, patternRows * BlockSize, 1f));

        public static Shield Create(Vector3 centerPos)
        {
            var go = new GameObject("Shield");
            go.transform.position = centerPos;
            var shield = go.AddComponent<Shield>();
            shield.Build();
            return shield;
        }

        private void Build()
        {
            patternRows = Pattern.Length;
            patternCols = Pattern[0].Length;
            var sprite = RetroSpriteFactory.GetShieldBlockSprite();

            for (int r = 0; r < patternRows; r++)
            {
                for (int c = 0; c < patternCols; c++)
                {
                    if (Pattern[r][c] == '.') continue;
                    float x = (c - (patternCols - 1) / 2f) * BlockSize;
                    float y = -(r - (patternRows - 1) / 2f) * BlockSize;

                    var blockGo = new GameObject("Block");
                    blockGo.transform.SetParent(transform, false);
                    blockGo.transform.localPosition = new Vector3(x, y, 0f);
                    var sr = blockGo.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = 2;
                    blocks.Add(blockGo.transform);
                }
            }
        }

        public bool TryDamage(Bounds otherBounds)
        {
            if (blocks.Count == 0 || !ApproxBounds.Intersects(otherBounds)) return false;

            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (b == null) continue;
                var blockBounds = new Bounds(b.position, new Vector3(BlockSize, BlockSize, 1f));
                if (blockBounds.Intersects(otherBounds))
                {
                    Destroy(b.gameObject);
                    blocks.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
}
