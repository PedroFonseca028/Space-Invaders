using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceInvaders
{
    // Orchestrates the whole game: builds every object at runtime (camera
    // setup, background, HUD, player, alien formation, shields, boss timer),
    // owns the state machine and resolves all collisions centrally with
    // plain AABB checks. Nothing in this project relies on hand-edited scene
    // or prefab data, so the game is fully self-contained the moment the
    // project is opened in Unity.
    public class GameManager : MonoBehaviour
    {
        private enum GameState { MainMenu, Playing, Paused, LevelIntro, GameOver, Victory }

        private const int MaxLevel = 5;
        private const string HighScoreKey = "SpaceInvaders_HighScore";

        private GameState state;
        private int score;
        private int highScore;
        private int lives;
        private int level;

        private PlayerShip player;
        private AlienFormation formation;
        private HudUI hud;
        private ScreenShake shake;
        private readonly List<Shield> shields = new List<Shield>();
        private BossShip boss;
        private bool bossWasActive;
        private float bossTimer;
        private float levelIntroTimer;

        private float BottomThresholdY => -ScreenUtil.HalfHeight + 1.8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<GameManager>() != null) return;
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>().Init();
        }

        private void Init()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6.5f;
                cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f, 1f);
                ScreenUtil.Cam = cam;
                shake = cam.gameObject.AddComponent<ScreenShake>();
            }

            Starfield.Create();
            hud = HudUI.Create();

            highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            hud.SetHighScore(highScore);
            hud.SetScore(0);
            hud.SetLevel(1, MaxLevel);
            hud.SetLives(3);

            player = PlayerShip.Create(new Vector3(0f, -ScreenUtil.HalfHeight + 1f, 0f));
            player.SetActive(false);

            EnterMainMenu();
        }

        private void EnterMainMenu()
        {
            ClearBoard();
            state = GameState.MainMenu;
            player.SetActive(false);
            hud.SetLives(3);
            hud.ShowMessage(
                "SPACE INVADERS",
                "Defenda a Terra dos invasores alienigenas!\n<- -> / A D mover   ESPACO atirar   P pausa",
                "PRESSIONE ESPACO PARA COMECAR");
        }

        private void BeginCampaign()
        {
            RetroAudio.PlayUiBlip();
            score = 0;
            lives = 3;
            level = 1;
            hud.SetScore(0);
            hud.SetLives(lives);
            StartLevel(level);
        }

        private void StartLevel(int lvl)
        {
            ClearBoard();

            float halfH = ScreenUtil.HalfHeight;
            const int rows = 5;
            const int cols = 8;
            float baseSpeed = 1.3f + (lvl - 1) * 0.4f;
            float fireRate = 0.10f + (lvl - 1) * 0.045f;
            float alienBulletSpeed = 6.5f + (lvl - 1) * 0.3f;

            formation = AlienFormation.Create(rows, cols, 1.3f, 1.0f, baseSpeed, fireRate, alienBulletSpeed,
                new Vector3(0f, halfH - 1.6f, 0f));
            formation.enabled = false;

            BuildShields();

            player.ResetToStart();
            player.SetActive(false);

            bossTimer = Random.Range(30f, 50f);
            bossWasActive = false;

            levelIntroTimer = 1.6f;
            state = GameState.LevelIntro;
            hud.SetLevel(lvl, MaxLevel);
            hud.ShowMessage("WAVE " + lvl, "Prepare-se...", "");
        }

        private void BuildShields()
        {
            float halfH = ScreenUtil.HalfHeight;
            float halfW = ScreenUtil.HalfWidth;
            float shieldY = -halfH + 3.2f;
            float span = halfW * 1.2f;
            const int count = 4;
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(-span * 0.5f, span * 0.5f, (i + 0.5f) / count);
                shields.Add(Shield.Create(new Vector3(x, shieldY, 0f)));
            }
        }

        private void ClearBoard()
        {
            Bullet.DestroyAll();
            if (formation != null) Destroy(formation.gameObject);
            formation = null;
            if (boss != null) Destroy(boss.gameObject);
            boss = null;
            foreach (var s in shields) if (s != null) Destroy(s.gameObject);
            shields.Clear();
        }

        private void Update()
        {
            var kb = Keyboard.current;

            switch (state)
            {
                case GameState.MainMenu:
                    if (kb != null && kb.spaceKey.wasPressedThisFrame) BeginCampaign();
                    break;

                case GameState.LevelIntro:
                    levelIntroTimer -= Time.deltaTime;
                    if (levelIntroTimer <= 0f)
                    {
                        hud.HideMessage();
                        player.SetActive(true);
                        formation.enabled = true;
                        state = GameState.Playing;
                    }
                    break;

                case GameState.Playing:
                    if (kb != null && (kb.pKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                    {
                        Time.timeScale = 0f;
                        player.SetActive(false);
                        state = GameState.Paused;
                        hud.ShowMessage("PAUSADO", "", "PRESSIONE P PARA CONTINUAR");
                        break;
                    }
                    TickPlaying();
                    break;

                case GameState.Paused:
                    if (kb != null && (kb.pKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                    {
                        Time.timeScale = 1f;
                        player.SetActive(true);
                        hud.HideMessage();
                        state = GameState.Playing;
                    }
                    break;

                case GameState.GameOver:
                case GameState.Victory:
                    if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame))
                    {
                        RetroAudio.PlayUiBlip();
                        EnterMainMenu();
                    }
                    break;
            }
        }

        private void TickPlaying()
        {
            if (boss == null)
            {
                if (bossWasActive)
                {
                    bossWasActive = false;
                    bossTimer = Random.Range(30f, 50f);
                }
                bossTimer -= Time.deltaTime;
                if (bossTimer <= 0f) SpawnBoss();
            }
            else
            {
                bossWasActive = true;
            }

            CheckCollisions();
            CheckShieldErosion();

            if (formation != null && formation.IsCleared)
            {
                AdvanceLevel();
                return;
            }

            if (formation != null && formation.LowestWorldY <= BottomThresholdY)
            {
                TriggerGameOver();
            }
        }

        private void SpawnBoss()
        {
            float halfW = ScreenUtil.HalfWidth;
            float halfH = ScreenUtil.HalfHeight;
            boss = BossShip.Create(new Vector3(-halfW - 1f, halfH - 0.7f, 0f), 4.5f);
        }

        private void CheckCollisions()
        {
            foreach (var b in Bullet.PlayerBullets.ToArray())
            {
                if (b == null) continue;
                var bb = b.WorldBounds;
                bool consumed = false;

                foreach (var s in shields)
                {
                    if (s != null && s.TryDamage(bb)) { consumed = true; break; }
                }

                if (!consumed && formation != null)
                {
                    foreach (var alien in formation.AliveAliens())
                    {
                        if (bb.Intersects(alien.WorldBounds))
                        {
                            formation.Kill(alien);
                            AddScore(alien.scoreValue, alien.transform.position);
                            RetroAudio.PlayExplosion();
                            consumed = true;
                            break;
                        }
                    }
                }

                if (!consumed && boss != null && !boss.IsDying && bb.Intersects(boss.WorldBounds))
                {
                    AddScore(boss.scoreValue, boss.transform.position);
                    boss.Explode(null);
                    boss = null;
                    shake.Shake(0.25f, 0.18f);
                    consumed = true;
                }

                if (consumed) Destroy(b.gameObject);
            }

            foreach (var b in Bullet.AlienBullets.ToArray())
            {
                if (b == null) continue;
                var bb = b.WorldBounds;
                bool consumed = false;

                foreach (var s in shields)
                {
                    if (s != null && s.TryDamage(bb)) { consumed = true; break; }
                }

                if (!consumed && player != null && bb.Intersects(player.WorldBounds))
                {
                    consumed = true;
                    if (player.TryTakeHit()) PlayerHit();
                }

                if (consumed) Destroy(b.gameObject);
            }
        }

        private void CheckShieldErosion()
        {
            if (formation == null || shields.Count == 0) return;
            float shieldBandTop = -ScreenUtil.HalfHeight + 3.2f + 1.5f;
            if (formation.LowestWorldY > shieldBandTop) return;

            foreach (var alien in formation.AliveAliens())
            {
                var ab = alien.WorldBounds;
                foreach (var s in shields)
                {
                    if (s != null) s.TryDamage(ab);
                }
            }
        }

        private void AddScore(int amount, Vector3 worldPos)
        {
            score += amount;
            hud.SetScore(score);
            if (score > highScore)
            {
                highScore = score;
                hud.SetHighScore(highScore);
            }
            FloatingScoreText.Create(worldPos + Vector3.up * 0.3f, "+" + amount, new Color(1f, 0.9f, 0.4f));
        }

        private void PlayerHit()
        {
            lives--;
            hud.SetLives(Mathf.Max(lives, 0));
            RetroAudio.PlayPlayerHit();
            shake.Shake(0.3f, 0.22f);
            Bullet.DestroyAlienBullets();

            if (lives <= 0)
            {
                TriggerGameOver();
            }
            else
            {
                player.ResetToStart();
            }
        }

        private void AdvanceLevel()
        {
            RetroAudio.PlayLevelUp();
            level++;
            if (level > MaxLevel) TriggerVictory();
            else StartLevel(level);
        }

        private void EndRound()
        {
            Bullet.DestroyAll();
            if (boss != null) { Destroy(boss.gameObject); boss = null; }
            if (formation != null) formation.enabled = false;
            player.SetActive(false);
        }

        private void TriggerGameOver()
        {
            EndRound();
            state = GameState.GameOver;
            PersistHighScore();
            hud.ShowMessage("FIM DE JOGO", "A Terra foi invadida pelos alienigenas...\nPontuacao final: " + score,
                "PRESSIONE ESPACO PARA VOLTAR AO MENU");
            RetroAudio.PlayGameOver();
        }

        private void TriggerVictory()
        {
            EndRound();
            state = GameState.Victory;
            PersistHighScore();
            hud.ShowMessage("VITORIA!", "Os alienigenas foram expulsos da Terra!\nPontuacao final: " + score,
                "PRESSIONE ESPACO PARA VOLTAR AO MENU");
            RetroAudio.PlayVictory();
        }

        private void PersistHighScore()
        {
            if (score > highScore) highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
            hud.SetHighScore(highScore);
        }
    }
}
