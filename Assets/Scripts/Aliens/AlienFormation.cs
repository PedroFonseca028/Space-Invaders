using System.Collections.Generic;
using UnityEngine;

namespace SpaceInvaders
{
    // Owns the whole grid of aliens and moves them together as one block
    // (the classic side-to-side sweep + one step down on every wall touch),
    // exactly like the "ChangeState flips velocity" pattern from the lesson,
    // just coordinated for the whole formation instead of a single ship.
    public class AlienFormation : MonoBehaviour
    {
        private const float AlienHalfWidth = 0.55f;
        private const float AlienHalfHeight = 0.5f;

        private int rows, cols;
        private float colSpacing, rowSpacing;
        private float speed;
        private float fireRatePerShooter;
        private float alienBulletSpeed;
        private int direction = 1;

        private Alien[,] grid;
        private int[] columnLowestAliveRow;
        private int minAliveCol, maxAliveCol, maxAliveRowOverall, aliveCount, initialAlienCount;

        private int stepIndexForAudio;
        private int animFrameIndex;
        private float animTimer;

        public bool IsCleared => aliveCount <= 0;

        // Classic escalation: the fewer aliens remain, the faster the whole
        // formation marches (up to ~2.6x the level's base speed).
        private float CurrentSpeed => speed * Mathf.Lerp(1f, 2.6f, 1f - (float)aliveCount / initialAlienCount);

        private float LeftEdgeWorldX => transform.position.x + LocalX(minAliveCol) - AlienHalfWidth;
        private float RightEdgeWorldX => transform.position.x + LocalX(maxAliveCol) + AlienHalfWidth;
        public float LowestWorldY => aliveCount > 0 ? transform.position.y + LocalY(maxAliveRowOverall) - AlienHalfHeight : transform.position.y;

        public static AlienFormation Create(int rows, int cols, float colSpacing, float rowSpacing,
            float speed, float fireRatePerShooter, float alienBulletSpeed, Vector3 topCenterWorldPos)
        {
            var go = new GameObject("AlienFormation");
            go.transform.position = topCenterWorldPos;
            var f = go.AddComponent<AlienFormation>();
            f.rows = rows;
            f.cols = cols;
            f.colSpacing = colSpacing;
            f.rowSpacing = rowSpacing;
            f.speed = speed;
            f.fireRatePerShooter = fireRatePerShooter;
            f.alienBulletSpeed = alienBulletSpeed;
            f.BuildGrid();
            return f;
        }

        private float LocalX(int c) => (c - (cols - 1) / 2f) * colSpacing;
        private float LocalY(int r) => -r * rowSpacing;

        private void BuildGrid()
        {
            grid = new Alien[rows, cols];
            columnLowestAliveRow = new int[cols];
            for (int r = 0; r < rows; r++)
            {
                int type = RowToType(r);
                int score = TypeScore(type);
                var frames = RetroSpriteFactory.GetAlienFrames(type);
                for (int c = 0; c < cols; c++)
                {
                    Vector3 localPos = new Vector3(LocalX(c), LocalY(r), 0f);
                    grid[r, c] = Alien.Create(transform, type, score, r, c, localPos, frames[0]);
                }
            }
            aliveCount = rows * cols;
            initialAlienCount = aliveCount;
            RecomputeBookkeeping();
        }

        private int RowToType(int r)
        {
            if (rows <= 1) return 1;
            float ratio = r / (float)(rows - 1); // 0 = top row, 1 = bottom row
            if (ratio < 0.2f) return 3;
            if (ratio < 0.6f) return 2;
            return 1;
        }

        private static int TypeScore(int type) => type == 3 ? 30 : type == 2 ? 20 : 10;

        private void RecomputeBookkeeping()
        {
            minAliveCol = int.MaxValue;
            maxAliveCol = int.MinValue;
            maxAliveRowOverall = -1;
            for (int c = 0; c < cols; c++)
            {
                columnLowestAliveRow[c] = -1;
                for (int r = 0; r < rows; r++)
                {
                    if (grid[r, c] == null) continue;
                    if (c < minAliveCol) minAliveCol = c;
                    if (c > maxAliveCol) maxAliveCol = c;
                    if (r > columnLowestAliveRow[c]) columnLowestAliveRow[c] = r;
                    if (r > maxAliveRowOverall) maxAliveRowOverall = r;
                }
            }
        }

        public IEnumerable<Alien> AliveAliens()
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (grid[r, c] != null)
                        yield return grid[r, c];
        }

        public void Kill(Alien alien)
        {
            if (alien == null) return;
            int r = alien.row, c = alien.col;
            if (r < 0 || r >= rows || c < 0 || c >= cols) return;
            if (grid[r, c] != alien) return;
            grid[r, c] = null;
            aliveCount--;
            alien.Die(null);
            RecomputeBookkeeping();
        }

        private void Update()
        {
            if (aliveCount <= 0) return;

            float currentSpeed = CurrentSpeed;
            transform.position += Vector3.right * (direction * currentSpeed * Time.deltaTime);

            float halfW = ScreenUtil.HalfWidth;
            const float margin = 0.5f;
            bool hitEdge = (direction > 0 && RightEdgeWorldX >= halfW - margin) ||
                           (direction < 0 && LeftEdgeWorldX <= -halfW + margin);

            if (hitEdge)
            {
                direction *= -1;
                transform.position += Vector3.down * 0.4f;
                stepIndexForAudio = (stepIndexForAudio + 1) % 4;
                RetroAudio.PlayStep(stepIndexForAudio);
            }

            animTimer += Time.deltaTime;
            float animInterval = Mathf.Max(0.12f, 0.55f - currentSpeed * 0.05f);
            if (animTimer >= animInterval)
            {
                animTimer = 0f;
                FlipAnimFrame();
            }

            HandleFiring();
        }

        private void FlipAnimFrame()
        {
            animFrameIndex = 1 - animFrameIndex;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var a = grid[r, c];
                    if (a != null) a.SetFrame(RetroSpriteFactory.GetAlienFrames(a.type)[animFrameIndex]);
                }
            }
        }

        private void HandleFiring()
        {
            for (int c = 0; c < cols; c++)
            {
                int r = columnLowestAliveRow[c];
                if (r < 0) continue;
                if (Random.value >= fireRatePerShooter * Time.deltaTime) continue;

                var shooter = grid[r, c];
                if (shooter == null) continue;
                Vector3 pos = shooter.transform.position + Vector3.down * 0.5f;
                var bulletFrames = RetroSpriteFactory.GetAlienBulletFrames();
                var b = Bullet.Spawn(Bullet.Owner.Alien, pos, alienBulletSpeed, bulletFrames[0], new Vector2(0.12f, 0.3f));
                b.SetAnimFrames(bulletFrames, 0.1f);
            }
        }
    }
}
