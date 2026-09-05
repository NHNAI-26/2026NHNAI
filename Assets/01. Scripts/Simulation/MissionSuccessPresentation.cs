using System.Collections.Generic;
using Border.Audio;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Simulation
{
    [DisallowMultipleComponent]
    public sealed class MissionSuccessPresentation : MonoBehaviour
    {
        public const float CameraReleaseDuration = 1f;
        public const float DescentDuration = 6f;
        public const float Duration = CameraReleaseDuration + DescentDuration;
        [SerializeField] private GameObject parachutePrefab;
        [SerializeField] private AnimationClip swayClip;
        [SerializeField] private string parachuteSoundId = "parachute";
        [SerializeField, Min(0f)] private float parachuteSoundLeadSeconds = 0.15f;

        private GameObject rig;
        private Camera sourceCamera;
        private Camera presentationCamera;
        private Transform socket;
        private Animator animator;
        private Transform descent;
        private Transform rocketTransform;
        private Transform originalParent;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;
        private Vector3 startPosition;
        private Vector3 endPosition;
        private Vector3 releasePosition;
        private Vector3 releaseVelocity;
        private bool descentStarted;
        private bool parachuteSoundPlayed;
        private SoundHandle parachuteSound;
        private PlayableGraph graph;
        private AnimationClipPlayable sway;
        private readonly List<Behaviour> suspended = new();
        private readonly List<Renderer> hiddenRenderers = new();

        public bool IsPlaying { get; private set; }
        public Camera PresentationCamera => presentationCamera;

        public bool Begin(Camera sourceCamera, Vector3 flightVelocity = default)
        {
            if (IsPlaying) return true;
            Rocket rocket = GetComponentInParent<Rocket>();
            if (rocket == null || parachutePrefab == null || swayClip == null || sourceCamera == null)
            {
                Debug.LogWarning("Mission success presentation requires a parachute, sway clip and camera.", this);
                return false;
            }

            this.sourceCamera = sourceCamera;
            rocketTransform = rocket.transform;
            originalParent = rocketTransform.parent;
            originalPosition = rocketTransform.localPosition;
            originalRotation = rocketTransform.localRotation;
            originalScale = rocketTransform.localScale;
            releasePosition = rocketTransform.position;
            releaseVelocity = flightVelocity;
            rocket.StopFlight();

            rig = Instantiate(parachutePrefab, rocketTransform.position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(rig, gameObject.scene);
            descent = rig.transform.Find("DescentRoot");
            socket = rig.transform.Find("DescentRoot/SwayPivot/RocketSwingPivot/RocketSocket");
            animator = descent != null ? descent.GetComponent<Animator>() : null;
            if (socket == null || animator == null)
            {
                Debug.LogWarning("Mission success parachute has no socket or animator.", this);
                Destroy(rig);
                rig = null;
                return false;
            }

            rig.SetActive(false);
            descentStarted = false;
            parachuteSoundPlayed = false;
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (RocketBuilder builder in root.GetComponentsInChildren<RocketBuilder>()) Suspend(builder);
                foreach (CinemachineBrain brain in root.GetComponentsInChildren<CinemachineBrain>()) Suspend(brain);
                foreach (SkyEnvironment sky in root.GetComponentsInChildren<SkyEnvironment>()) Suspend(sky);
            }
            IsPlaying = true;
            SoundManager.Instance?.PlayBgm("parachuteBGM", 0.25f);
            return true;
        }

        private void BeginDescent()
        {
            descentStarted = true;
            rig.transform.position = rocketTransform.position;
            rig.SetActive(true);
            rocketTransform.rotation = Quaternion.identity;
            Bounds body = CalculateBounds(rocketTransform.gameObject, true);
            Vector3 attachment = new Vector3(body.center.x, body.max.y, body.center.z);
            rocketTransform.SetParent(socket, true);
            rocketTransform.position += socket.position - attachment;

            Bounds assembly = CalculateBounds(rig, false);
            float halfHeight = Mathf.Max(4f, assembly.size.y * 0.75f,
                assembly.size.x / Mathf.Max(0.1f, sourceCamera.aspect) * 0.8f);
            float distance = Mathf.Max(30f, assembly.size.z * 3f);
            float margin = Mathf.Max(2f, assembly.size.y * 0.3f);
            startPosition = descent.localPosition + Vector3.up *
                (assembly.center.y + halfHeight - assembly.min.y + margin);
            endPosition = descent.localPosition + Vector3.up *
                (assembly.center.y - halfHeight - assembly.max.y - margin);

            var cameraObject = new GameObject("Mission Success Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, gameObject.scene);
            presentationCamera = cameraObject.AddComponent<Camera>();
            presentationCamera.CopyFrom(sourceCamera);
            presentationCamera.targetTexture = null;
            presentationCamera.rect = new Rect(0f, 0f, 1f, 1f);
            presentationCamera.orthographic = true;
            presentationCamera.orthographicSize = halfHeight;
            presentationCamera.nearClipPlane = 0.1f;
            presentationCamera.farClipPlane = distance + assembly.size.z * 4f + 20f;
            presentationCamera.transform.SetPositionAndRotation(
                assembly.center + Vector3.back * distance, Quaternion.identity);
            foreach (Camera camera in Camera.allCameras)
                if (camera != presentationCamera && camera.targetTexture == null
                    && camera.targetDisplay == presentationCamera.targetDisplay)
                    presentationCamera.depth = Mathf.Max(presentationCamera.depth, camera.depth + 1f);
            presentationCamera.enabled = true;

            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (Camera camera in root.GetComponentsInChildren<Camera>())
                    if (camera != presentationCamera) Suspend(camera);
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>()) Suspend(canvas);
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled || renderer.transform.IsChildOf(rig.transform)) continue;
                    hiddenRenderers.Add(renderer);
                    renderer.enabled = false;
                }
            }

            graph = PlayableGraph.Create("Mission Success Sway");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            sway = AnimationClipPlayable.Create(graph, swayClip);
            var output = AnimationPlayableOutput.Create(graph, "Parachute", animator);
            output.SetSourcePlayable(sway);
            graph.Play();
        }

        public void Evaluate(float elapsedSeconds)
        {
            if (!IsPlaying) return;
            float elapsed = Mathf.Clamp(elapsedSeconds, 0f, Duration);
            if (!descentStarted)
            {
                rocketTransform.position = releasePosition + releaseVelocity * Mathf.Min(elapsed, CameraReleaseDuration);
                if (elapsed < CameraReleaseDuration) return;
                BeginDescent();
            }

            float descentElapsed = Mathf.Max(0f, elapsed - CameraReleaseDuration);
            if (!parachuteSoundPlayed && descentElapsed > 0f)
            {
                // Sample the approaching rocket so the cue follows its size and sway, even with attached engines.
                EvaluateDescent(Mathf.Min(DescentDuration, descentElapsed + parachuteSoundLeadSeconds));
                Bounds rocketBounds = CalculateBounds(rocketTransform.gameObject, false);
                if (presentationCamera.WorldToViewportPoint(rocketBounds.min).y <= 1f)
                {
                    parachuteSoundPlayed = true;
                    if (SoundManager.Instance != null && !string.IsNullOrEmpty(parachuteSoundId))
                        parachuteSound = SoundManager.Instance.PlaySfx(parachuteSoundId);
                }
            }
            EvaluateDescent(descentElapsed);
        }

        private void EvaluateDescent(float elapsed)
        {
            sway.SetTime(elapsed % Mathf.Max(0.001f, swayClip.length));
            graph.Evaluate(0f);
            descent.localPosition = Vector3.Lerp(startPosition, endPosition, elapsed / DescentDuration);
        }

        public void End()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            parachuteSound.Stop();
            parachuteSound = SoundHandle.Invalid;
            if (graph.IsValid()) graph.Destroy();
            // Detach the existing rocket before destroying the rig that temporarily owns it.
            if (rocketTransform != null)
            {
                rocketTransform.SetParent(originalParent, false);
                rocketTransform.SetLocalPositionAndRotation(originalPosition, originalRotation);
                rocketTransform.localScale = originalScale;
            }
            foreach (Behaviour behaviour in suspended)
                if (behaviour != null) behaviour.enabled = true;
            suspended.Clear();
            foreach (Renderer renderer in hiddenRenderers)
                if (renderer != null) renderer.enabled = true;
            hiddenRenderers.Clear();
            if (presentationCamera != null)
            {
                presentationCamera.enabled = false;
                Destroy(presentationCamera.gameObject);
            }
            if (rig != null) Destroy(rig);
            presentationCamera = null;
            sourceCamera = null;
            rig = null;
        }

        private void OnDisable() => End();

        private void Suspend(Behaviour behaviour)
        {
            if (!behaviour.enabled) return;
            suspended.Add(behaviour);
            behaviour.enabled = false;
        }

        private static Bounds CalculateBounds(GameObject root, bool bodyOnly)
        {
            var bounds = new Bounds(root.transform.position, Vector3.zero);
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)) continue;
                if (bodyOnly && renderer.GetComponentInParent<RocketPart>() != null) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }
    }
}
