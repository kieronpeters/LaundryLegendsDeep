using System.IO;
using System.Text;
using LaundryLegends.Arcade;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Captures the cabinet's screens (PLAY/QUIT menu, a fight frame, the result) and
    /// the world positions of the room, cabinet and player, so the minigame can be
    /// inspected without playing the whole economy loop.
    /// </summary>
    public static class ArcadeCaptureTool
    {
        private const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";

        [MenuItem("Laundry Legends/Phase 8/Capture Arcade Screen")]
        public static void Capture()
        {            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            ArcadeCabinet cabinet = Object.FindFirstObjectByType<ArcadeCabinet>(FindObjectsInactive.Include);
            FightGameView view = Object.FindFirstObjectByType<FightGameView>(FindObjectsInactive.Include);
            if (cabinet == null || view == null)
            {
                Debug.LogError($"[ARCADE-CAP] cabinet={cabinet != null} fightView={view != null}");
                return;
            }

            var report = new StringBuilder();
            report.AppendLine("[ARCADE-CAP] positions");
            report.AppendLine($"  cabinet {cabinet.transform.position}");
            report.AppendLine($"  screen {(view.Screen != null ? view.Screen.name : "<none>")}");

            Directory.CreateDirectory("TestResults");

            var session = new FightSession();
            view.Show(session);
            Save(view.Texture, "TestResults/arcade-menu.png", report);

            session.Confirm();
            for (int i = 0; i < 60; i++)
            {
                session.Tick(0.1f);
            }

            view.Show(session);
            Save(view.Texture, "TestResults/arcade-fight.png", report);

            // Force a finish so the result banner can be captured.
            var finisher = new FightSession();
            finisher.Confirm();
            while (finisher.Screen == FightScreen.Fight)
            {
                finisher.Tick(0.25f);
            }

            view.Show(finisher);
            Save(view.Texture, "TestResults/arcade-result.png", report);

            view.Hide();
            Debug.Log(report.ToString());
            File.WriteAllText("TestResults/arcade-capture.txt", report.ToString());
        }

        /// <summary>
        /// Renders the two cabinet games to PNGs so per-machine variety has visual
        /// evidence without installing cabinets in a running build.
        /// </summary>
        [MenuItem("Laundry Legends/Phase 8/Capture Game Varieties")]
        public static void CaptureVarieties()
        {
            Directory.CreateDirectory("TestResults/phase12");
            var report = new StringBuilder();
            report.AppendLine("[ARCADE-CAP] game varieties");

            var mazeObject = new GameObject("MazeCapture");
            var mazeView = mazeObject.AddComponent<MazeGameView>();
            mazeView.Configure(null);
            var maze = new MazeGame(new MazeLayout());
            maze.TryMovePlayer(Vector2Int.up);
            mazeView.Show(maze);
            Save(mazeView.Texture, "TestResults/phase12/arcade-maze.png", report);
            Object.DestroyImmediate(mazeObject);

            var fightObject = new GameObject("FightCapture");
            var fightView = fightObject.AddComponent<FightGameView>();
            fightView.Configure(null);
            var session = new FightSession();
            session.SetNames("CIVIC", "TURBO");
            session.Confirm();
            fightView.SetTheme(
                new Color(1f, 0.84f, 0.22f),
                new Color(1f, 0.22f, 0.72f));
            fightView.Show(session);
            for (int i = 0; i < 20; i++)
            {
                session.Tick(0.05f);
            }

            fightView.Redraw();
            Save(fightView.Texture, "TestResults/phase12/arcade-fight-turbo.png", report);
            Object.DestroyImmediate(fightObject);

            Debug.Log(report.ToString());
        }

        private static void Save(Texture2D texture, string path, StringBuilder report)
        {
            if (texture == null)
            {
                report.AppendLine($"  {path}: NO TEXTURE");
                return;
            }

            File.WriteAllBytes(path, texture.EncodeToPNG());
            report.AppendLine($"  saved {path} ({texture.width}x{texture.height})");
        }
    }
}

