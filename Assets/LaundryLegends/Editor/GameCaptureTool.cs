using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using LaundryLegends.NPC;
using LaundryLegends.Player;
using LaundryLegends.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LaundryLegends.Editor
{
    /// <summary>
    /// Renders the real Laundromat in play mode to 1280x720 PNGs for visual QA.
    /// Batch usage (rendering is required; never add -nographics):
    /// Unity.exe -batchmode -projectPath "PROJECT" -executeMethod
    /// LaundryLegends.Editor.GameCaptureTool.CaptureAll -logFile "TestResults\captures.log"
    ///
    /// The method opens Laundromat.unity, requests play mode so the real staff and
    /// ambient NPC coroutines run, pumps a frame-driven capture routine, renders
    /// each shot with a temporary camera into a RenderTexture, writes
    /// TestResults/captures/*.png, and exits the editor with 0 (all frames valid)
    /// or 1 (a frame was black/empty or a capture step failed).
    /// </summary>
    public static class GameCaptureTool
    {
        public const int CaptureWidth = 1280;
        public const int CaptureHeight = 720;
        public const string ScenePath = "Assets/LaundryLegends/Scenes/Laundromat.unity";
        public const string LooseLaundryPath =
            "Assets/LaundryLegends/Prefabs/Gameplay/Laundry/LooseLaundry.prefab";

        private const string PendingKey = "LaundryLegends.GameCapture.Pending";
        private const float PlayModeTimeoutSeconds = 90f;
        private const double CaptureBudgetSeconds = 240.0;

        private static readonly List<IEnumerator> s_stack = new List<IEnumerator>();
        private static readonly List<string> s_invalidFrames = new List<string>();
        private static readonly StringBuilder s_summary = new StringBuilder();

        private static bool s_running;
        private static bool s_finished;
        private static bool s_waitResult;
        private static double s_requestedAt;
        private static double s_captureStartedAt;
        private static RenderTexture s_renderTexture;
        private static Texture2D s_texture;

        public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static string CaptureFolder => Path.Combine(ProjectRoot, "TestResults", "captures");

        // ------------------------------------------------------------------
        // Entry points
        // ------------------------------------------------------------------

        [MenuItem("Laundry Legends/Capture/Game Captures (Play Mode)")]
        public static void CaptureAll()
        {
            s_stack.Clear();
            s_invalidFrames.Clear();
            s_summary.Clear();
            s_running = false;
            s_finished = false;

            try
            {
                Directory.CreateDirectory(CaptureFolder);
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CAPTURE] Could not prepare the capture scene: {exception}");
                Finish(1);
                return;
            }

            SessionState.SetBool(PendingKey, true);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= PlayModeWatchdog;
            EditorApplication.update += PlayModeWatchdog;
            s_requestedAt = EditorApplication.timeSinceStartup;

            Debug.Log($"[CAPTURE] Opened {ScenePath}; requesting play mode for the game capture.");

            if (EditorApplication.isPlaying)
            {
                BeginPumping();
            }
            else
            {
                EditorApplication.isPlaying = true;
            }
        }

        /// <summary>
        /// Re-arms the capture after the play-mode domain reload. SessionState
        /// survives the reload; static delegates do not.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Bootstrap()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (EditorApplication.isPlaying)
            {
                BeginPumping();
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            if (SessionState.GetBool(PendingKey, false))
            {
                BeginPumping();
            }
        }

        private static void PlayModeWatchdog()
        {
            if (s_running || s_finished)
            {
                EditorApplication.update -= PlayModeWatchdog;
                return;
            }

            if (EditorApplication.isPlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup - s_requestedAt < PlayModeTimeoutSeconds)
            {
                return;
            }

            EditorApplication.update -= PlayModeWatchdog;
            Debug.LogError(
                $"[CAPTURE] Play mode did not start within {PlayModeTimeoutSeconds:0}s; gameplay state cannot be captured.");
            Finish(3);
        }

        private static void BeginPumping()
        {
            if (s_running || s_finished)
            {
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            s_running = true;
            s_captureStartedAt = Time.realtimeSinceStartupAsDouble;
            s_stack.Clear();
            s_stack.Add(CaptureRoutine());
            EditorApplication.update -= PlayModeWatchdog;
            EditorApplication.update -= PumpCapture;
            EditorApplication.update += PumpCapture;
            Debug.Log("[CAPTURE] Play mode running; capture routine attached.");
        }

        // ------------------------------------------------------------------
        // Manual coroutine pump
        // ------------------------------------------------------------------

        private static void PumpCapture()
        {
            if (!s_running)
            {
                return;
            }

            if (Time.realtimeSinceStartupAsDouble - s_captureStartedAt > CaptureBudgetSeconds)
            {
                Debug.LogError($"[CAPTURE] Capture exceeded its {CaptureBudgetSeconds:0}s budget.");
                Finish(2);
                return;
            }

            try
            {
                if (!StepRoutine())
                {
                    Finish(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[CAPTURE] Capture failed: {exception}");
                Finish(1);
            }
        }

        /// <summary>
        /// Advances one frame. Nested <see cref="IEnumerator"/> yields are pushed so
        /// wait helpers compose without a MonoBehaviour coroutine host.
        /// </summary>
        private static bool StepRoutine()
        {
            while (s_stack.Count > 0)
            {
                IEnumerator current = s_stack[s_stack.Count - 1];
                if (!current.MoveNext())
                {
                    s_stack.RemoveAt(s_stack.Count - 1);
                    continue;
                }

                if (current.Current is IEnumerator nested)
                {
                    s_stack.Add(nested);
                    continue;
                }

                return true;
            }

            return false;
        }

        private static void Finish(int exitCode)
        {
            if (s_finished)
            {
                return;
            }

            s_finished = true;
            s_running = false;
            EditorApplication.update -= PumpCapture;
            EditorApplication.update -= PlayModeWatchdog;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SessionState.SetBool(PendingKey, false);

            WriteSummary(exitCode);

            if (s_renderTexture != null)
            {
                s_renderTexture.Release();
                Object.DestroyImmediate(s_renderTexture);
                s_renderTexture = null;
            }

            if (s_texture != null)
            {
                Object.DestroyImmediate(s_texture);
                s_texture = null;
            }

            Debug.Log($"[CAPTURE] Finished. exitCode={exitCode} folder={CaptureFolder}");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
            else
            {
                EditorApplication.isPlaying = false;
            }
        }

        // ------------------------------------------------------------------
        // Capture routine
        // ------------------------------------------------------------------

        private static IEnumerator CaptureRoutine()
        {
            Directory.CreateDirectory(CaptureFolder);
            Note($"capture-start batch={Application.isBatchMode} isPlaying={Application.isPlaying} " +
                 $"screen={Screen.width}x{Screen.height}");

            // Let Awake/Start and the first animator evaluation settle.
            yield return WaitSeconds(1f);

            LaundryStaff staff = null;
            yield return WaitUntil(
                () => (staff = Object.FindFirstObjectByType<LaundryStaff>()) != null,
                20f,
                "LaundryStaff in the Laundromat scene");

            if (!s_waitResult)
            {
                throw new InvalidOperationException("Laundromat did not load a LaundryStaff instance.");
            }

            CarrySocket socket = staff.GetComponent<CarrySocket>();
            NpcLocomotion locomotion = staff.GetComponent<NpcLocomotion>();
            if (socket == null)
            {
                throw new InvalidOperationException("LaundryStaff has no CarrySocket; carrying cannot be captured.");
            }

            Camera camera = CreateCamera();
            s_renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = "GameCaptureRT",
                antiAliasing = 1
            };
            s_texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false)
            {
                name = "GameCaptureTexture"
            };
            camera.targetTexture = s_renderTexture;

            Note($"scene={ScenePath}");
            Note($"staff={staff.name} pos={staff.transform.position} speed={staff.WalkSpeed:0.00} " +
                 $"carrying={staff.IsCarrying}");

            // 01 - outside, facing the shop front. Offset from the street NPC lane
            // so the customer does not stand between the camera and the facade.
            camera.clearFlags = CameraClearFlags.Skybox;
            RenderShot(camera, "01-storefront.png",
                new Vector3(6.5f, 2.2f, -13.0f),
                new Vector3(0f, 2.0f, -6.0f),
                50f,
                "outside, facing the shop front");

            // 02 - inside, wide view of the room.
            camera.clearFlags = CameraClearFlags.SolidColor;
            RenderShot(camera, "02-interior-wide.png",
                new Vector3(-6.6f, 2.6f, -4.8f),
                new Vector3(1.0f, 0.9f, 3.4f),
                65f,
                "inside, wide view of the room");

            // 03 - washer/dryer wall.
            RenderShot(camera, "03-machine-wall.png",
                new Vector3(0f, 1.65f, 0.7f),
                new Vector3(0f, 1.0f, 5.3f),
                60f,
                "washer/dryer wall with laundry");

            // 04 - eye height in front of the counter.
            RenderShot(camera, "04-player-counter.png",
                new Vector3(-3.6f, 1.62f, -0.6f),
                new Vector3(-4.5f, 1.05f, -2.6f),
                65f,
                "first-person height at the counter");

            // 05 - the standing attendant, framed by the same helper used for the
            // carry shot. The attendant works facing the counter, so it is turned
            // toward the room for the photo only and restored immediately after.
            LogActorBounds("05 actor", staff.transform);
            Quaternion originalRotation = staff.transform.rotation;
            staff.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            Vector3 idleCamera;
            Vector3 idleLook;
            PlaceAroundActor(staff.transform, 2.4f, 0.9f, 1.5f, out idleCamera, out idleLook);
            RenderShot(camera, "05-npc-idle.png", idleCamera, idleLook, 42f,
                $"standing NPC '{staff.name}'");
            staff.transform.rotation = originalRotation;

            // 06/07 need real gameplay state: put laundry in the counter drop-off
            // and let the staff's own loop walk over and pick it up.
            CarrySlot dropOff = null;
            GameObject dropOffObject = GameObject.Find("DropOffSlot");
            if (dropOffObject != null)
            {
                dropOff = dropOffObject.GetComponent<CarrySlot>();
            }

            if (dropOff == null)
            {
                throw new InvalidOperationException("DropOffSlot with a CarrySlot is missing from the Laundromat scene.");
            }

            staff.SetWalkSpeed(1.7f);
            Note($"staff walk speed raised to {staff.WalkSpeed:0.00}");

            if (dropOff.IsEmpty)
            {
                LaundryItem forced = SpawnLooseLaundry("CaptureForcedDropOff");
                forced.transform.position = dropOff.Anchor.position + Vector3.up * 0.4f;
                if (!dropOff.TryPlace(forced))
                {
                    Object.Destroy(forced.gameObject);
                    Note("WARN could not place the forced laundry into DropOffSlot");
                }
                else
                {
                    Note($"forced laundry placed into DropOffSlot at {forced.transform.position}");
                }
            }
            else
            {
                Note("DropOffSlot already occupied by the ambient customer's laundry; using it");
            }

            // 07 - staff visibly holding the item.
            yield return WaitUntil(() => socket.IsCarrying, 30f, "CarrySocket.IsCarrying");
            if (!s_waitResult)
            {
                throw new InvalidOperationException(
                    "Staff never picked the drop-off laundry up, so 07-staff-carrying cannot be captured.");
            }

            yield return WaitSeconds(0.3f);

            LaundryItem held = socket.Carried;
            Note($"carry state carrying={socket.IsCarrying} item={(held != null ? held.name : "null")} " +
                 $"parent={(held != null && held.transform.parent != null ? held.transform.parent.name : "null")} " +
                 $"itemWorld={(held != null ? held.transform.position.ToString() : "null")} " +
                 $"anchor={socket.Anchor.position} " +
                 $"anchorDistance={(held != null ? Vector3.Distance(held.transform.position, socket.Anchor.position) : -1f):0.000}");
            LogActorBounds("07 actor", staff.transform);

            Vector3 carryCamera;
            Vector3 carryLook;
            PlaceAroundActor(staff.transform, 1.9f, 1.0f, 1.4f, out carryCamera, out carryLook);
            RenderShot(camera, "07-staff-carrying.png", carryCamera, carryLook, 42f,
                $"staff carrying '{held?.name}'");

            // 06 - staff mid-walk. Wait for a continuous empty-handed walk so the
            // walk clip is actually playing and the staff is between stations.
            bool walkFound = false;
            bool walkTracked = false;
            float walkElapsed = 0f;
            Vector3 walkStart = staff.transform.position;
            while (walkElapsed < 40f)
            {
                bool moving = !staff.IsCarrying
                              && locomotion != null
                              && locomotion.Speed01 > 0.5f;

                if (!moving)
                {
                    walkTracked = false;
                }
                else if (!walkTracked)
                {
                    walkTracked = true;
                    walkStart = staff.transform.position;
                }
                else
                {
                    Vector3 delta = staff.transform.position - walkStart;
                    if (new Vector2(delta.x, delta.z).magnitude > 0.35f)
                    {
                        walkFound = true;
                        break;
                    }
                }

                walkElapsed += Time.deltaTime;
                yield return null;
            }

            if (!walkFound)
            {
                Note("WARN no continuous empty-handed walk was observed within 40s");
            }
            else
            {
                yield return WaitSeconds(0.1f);
            }

            Note($"walk capture speed01={(locomotion != null ? locomotion.Speed01 : -1f):0.00} " +
                 $"carrying={staff.IsCarrying} pos={staff.transform.position}");
            LogActorBounds("06 actor", staff.transform);

            Vector3 walkCamera;
            Vector3 walkLook;
            PlaceAroundActor(staff.transform, 2.6f, 1.4f, 1.5f, out walkCamera, out walkLook);
            RenderShot(camera, "06-staff-walking.png", walkCamera, walkLook, 40f,
                $"staff mid-walk speed01={(locomotion != null ? locomotion.Speed01 : -1f):0.00}");

            // 08 - wrapped bundle on the folding table. Force a wrap if the staff
            // has not reached the folding stage yet.
            FoldingStation folding = Object.FindFirstObjectByType<FoldingStation>();
            if (folding == null)
            {
                throw new InvalidOperationException("No FoldingStation in the Laundromat scene.");
            }

            if (folding.IsReady)
            {
                Note("folding station already holds a wrapped bundle");
            }
            else
            {
                LaundryItem bundle = SpawnLooseLaundry("CaptureBundle");
                bundle.SetStage(LaundryStage.Dried);
                if (!folding.WrapItem(bundle))
                {
                    Note("WARN FoldingStation.WrapItem rejected the forced bundle");
                }
                else
                {
                    Note($"forced wrap: ready={folding.IsReady} stage={folding.ReadyItem?.Stage}");
                }
            }

            yield return WaitSeconds(0.15f);

            Vector3 table = folding.transform.position;
            RenderShot(camera, "08-wrapped-bundle.png",
                table + new Vector3(-1.95f, 1.15f, -1.45f),
                table + new Vector3(0f, 0.62f, 0f),
                38f,
                $"wrapped bundle ready={folding.IsReady}");

            // 09 - supplementary UI check. The IMGUI prompt is drawn by OnGUI to
            // the screen, so it cannot appear in a RenderTexture capture; this
            // ScreenCapture shot is the only evidence that the prompt renders.
            yield return CaptureUiCheck(camera);

            Note($"invalidFrames={s_invalidFrames.Count}");
            if (s_invalidFrames.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Black/empty render for: {string.Join(", ", s_invalidFrames)}");
            }
        }

        // ------------------------------------------------------------------
        // Shots
        // ------------------------------------------------------------------

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("GameCaptureCamera");
            Object.DontDestroyOnLoad(cameraObject);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 120f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = true;
            return camera;
        }

        private static void RenderShot(
            Camera camera,
            string fileName,
            Vector3 position,
            Vector3 lookAt,
            float fieldOfView,
            string description)
        {
            string path = Path.Combine(CaptureFolder, fileName);

            Vector3 forward = lookAt - position;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            camera.transform.position = position;
            camera.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            camera.fieldOfView = fieldOfView;
            camera.aspect = (float)CaptureWidth / CaptureHeight;
            camera.targetTexture = s_renderTexture;

            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = s_renderTexture;
            s_texture.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0, false);
            s_texture.Apply(false, false);
            RenderTexture.active = previous;

            byte[] png = s_texture.EncodeToPNG();
            File.WriteAllBytes(path, png);

            string stats = DescribeFrame();
            if (stats == null)
            {
                s_invalidFrames.Add(fileName);
            }

            Debug.Log($"[CAPTURE] {path}");
            Note($"{fileName}|{png.Length}B|cam={position.ToString("0.00")}|" +
                 $"look={lookAt.ToString("0.00")}|fov={fieldOfView:0}|{stats}|{description}");
        }

        /// <summary>
        /// Samples the centre half of the frame and reports mean luminance and how
        /// much of it is lit. Returns null (and flags the frame) when the render is
        /// effectively black or empty.
        /// </summary>
        private static string DescribeFrame()
        {
            Color32[] pixels = s_texture.GetPixels32();
            int x0 = CaptureWidth / 4;
            int x1 = CaptureWidth * 3 / 4;
            int y0 = CaptureHeight / 4;
            int y1 = CaptureHeight * 3 / 4;

            double sum = 0.0;
            double sumSquares = 0.0;
            int lit = 0;
            int count = 0;
            for (int y = y0; y < y1; y += 2)
            {
                int row = y * CaptureWidth;
                for (int x = x0; x < x1; x += 2)
                {
                    Color32 pixel = pixels[row + x];
                    double luminance = (0.2126 * pixel.r + 0.7152 * pixel.g + 0.0722 * pixel.b) / 255.0;
                    sum += luminance;
                    sumSquares += luminance * luminance;
                    if (luminance > 0.08)
                    {
                        lit++;
                    }

                    count++;
                }
            }

            double mean = sum / count;
            double variance = Math.Max(0.0, sumSquares / count - mean * mean);
            double litFraction = (double)lit / count;
            bool valid = mean >= 0.03 && litFraction >= 0.10;
            string stats = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "mean={0:0.000} sd={1:0.000} lit={2:0.0}% {3}",
                mean,
                Math.Sqrt(variance),
                litFraction * 100.0,
                valid ? "OK" : "BLACK/EMPTY");
            return valid ? stats : null;
        }

        private static IEnumerator CaptureUiCheck(Camera captureCamera)
        {
            FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
            InteractionPromptView prompt = Object.FindFirstObjectByType<InteractionPromptView>();
            LaundryItem basket = null;
            foreach (LaundryItem item in Object.FindObjectsByType<LaundryItem>(FindObjectsSortMode.None))
            {
                if (item.Kind == LaundryItemKind.Basket && item.transform.parent == null)
                {
                    basket = item;
                    break;
                }
            }

            if (player == null || prompt == null || basket == null)
            {
                Note("UI check skipped: missing player, prompt view, or free basket");
                yield break;
            }

            captureCamera.targetTexture = s_renderTexture;

            Vector3 basketPosition = basket.transform.position;
            player.transform.position = new Vector3(basketPosition.x, 0.05f, basketPosition.z + 1.1f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            player.Look(new Vector2(0f, 100f));
            player.Look(new Vector2(0f, -(85f + 50f) / 2f));
            Physics.SyncTransforms();

            yield return WaitSeconds(0.4f);
            yield return null;

            Note($"UI check visible={prompt.IsVisible} label='{prompt.CurrentLabel}' " +
                 $"screen={Screen.width}x{Screen.height}");

            string path = Path.Combine(CaptureFolder, "09-ui-prompt.png");
            bool requested = false;
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                ScreenCapture.CaptureScreenshot(path);
                requested = true;
            }
            catch (Exception exception)
            {
                Note($"WARN UI capture request failed: {exception.Message}");
            }

            if (!requested)
            {
                yield break;
            }

            float waited = 0f;
            while (!File.Exists(path) && waited < 10f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (File.Exists(path))
            {
                Debug.Log($"[CAPTURE] {path}");
                Note($"09-ui-prompt.png|{new FileInfo(path).Length}B|screencapture|" +
                     $"promptVisible={prompt.IsVisible}");
            }
            else
            {
                Note("WARN ScreenCapture produced no file for the UI check");
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static IEnumerator WaitSeconds(float seconds)
        {
            float elapsed = 0f;
            int guard = 0;
            while (elapsed < seconds && guard++ < 2000000)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private static IEnumerator WaitUntil(Func<bool> predicate, float timeoutSeconds, string what)
        {
            float elapsed = 0f;
            int guard = 0;
            s_waitResult = predicate();
            while (!s_waitResult && elapsed < timeoutSeconds && guard++ < 2000000)
            {
                elapsed += Time.deltaTime;
                yield return null;
                s_waitResult = predicate();
            }

            if (s_waitResult)
            {
                Note($"wait ok after {elapsed:0.00}s: {what}");
            }
            else
            {
                Debug.LogWarning($"[CAPTURE] Timed out after {timeoutSeconds:0}s waiting for {what}.");
                Note($"WARN timeout after {elapsed:0.00}s: {what}");
            }
        }

        private static LaundryItem SpawnLooseLaundry(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LooseLaundryPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing laundry prefab: {LooseLaundryPath}");
            }

            GameObject instance = Object.Instantiate(prefab);
            instance.name = name;
            LaundryItem item = instance.GetComponent<LaundryItem>();
            if (item == null)
            {
                throw new InvalidOperationException($"{LooseLaundryPath} has no LaundryItem component.");
            }

            Note($"spawned {name} kind={item.Kind} at {instance.transform.position}");
            return item;
        }

        /// <summary>
        /// Picks a camera spot ahead of and to one side of an actor, biased toward
        /// the room interior, and rejects spots that leave the room or have
        /// geometry (other than the actor) between camera and target.
        /// </summary>
        private static void PlaceAroundActor(
            Transform actor,
            float forwardDistance,
            float sideDistance,
            float height,
            out Vector3 position,
            out Vector3 lookAt)
        {
            Vector3 origin = actor.position;
            Vector3 forward = actor.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            lookAt = origin + Vector3.up * 1.02f;

            float[] signs = { 1f, -1f };
            foreach (float sign in signs)
            {
                Vector3 candidate = origin
                                    + forward * forwardDistance
                                    + right * (sideDistance * sign)
                                    + Vector3.up * height;
                if (!InsideRoom(candidate))
                {
                    continue;
                }

                if (Physics.Linecast(candidate, lookAt, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider != null
                    && hit.collider.transform.root != actor.root)
                {
                    continue;
                }

                position = candidate;
                return;
            }

            position = origin + forward * forwardDistance + Vector3.up * height;
            Note($"WARN no clear camera spot around '{actor.name}'; fell back to {position}");
        }

        private static bool InsideRoom(Vector3 point)
        {
            return point.x > -7.4f && point.x < 7.4f
                && point.z > -5.4f && point.z < 5.4f
                && point.y > 0.2f && point.y < 3.1f;
        }

        private static void LogActorBounds(string label, Transform actor)
        {
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            int renderers = 0;
            foreach (Renderer renderer in actor.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                minY = Mathf.Min(minY, renderer.bounds.min.y);
                maxY = Mathf.Max(maxY, renderer.bounds.max.y);
                renderers++;
            }

            if (renderers == 0)
            {
                Note($"{label} bounds: no enabled renderers");
                return;
            }

            Note($"{label} bounds minY={minY:0.000} maxY={maxY:0.000} rootY={actor.position.y:0.000} " +
                 $"renderers={renderers}");
        }

        private static void Note(string line)
        {
            s_summary.AppendLine(line);
            Debug.Log($"[CAPTURE] {line}");
        }

        private static void WriteSummary(int exitCode)
        {
            try
            {
                Directory.CreateDirectory(CaptureFolder);
                string path = Path.Combine(CaptureFolder, "summary.txt");
                var text = new StringBuilder();
                text.AppendLine($"exitCode={exitCode}");
                text.AppendLine($"utc={DateTime.UtcNow:O}");
                text.AppendLine($"project={ProjectRoot}");
                text.AppendLine($"scene={ScenePath}");
                text.AppendLine($"invalidFrames={string.Join(",", s_invalidFrames)}");
                text.AppendLine("---");
                text.Append(s_summary);
                File.WriteAllText(path, text.ToString());
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[CAPTURE] Could not write the summary file: {exception.Message}");
            }
        }
    }
}
