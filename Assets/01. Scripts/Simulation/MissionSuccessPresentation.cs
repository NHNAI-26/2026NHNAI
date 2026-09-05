using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Simulation
{
    [DisallowMultipleComponent]
    public sealed class MissionSuccessPresentation : MonoBehaviour
    {
        public const float Duration = 6f;
        [SerializeField] private GameObject parachutePrefab;
        [SerializeField] private AnimationClip swayClip;

        private GameObject rig;
        private Camera presentationCamera;
        private Transform descent;
        private Transform rocketTransform;
        private Transform originalParent;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;
        private Vector3 startPosition;
        private Vector3 endPosition;
        private PlayableGraph graph;
        private AnimationClipPlayable sway;
        private readonly List<Behaviour> suspended = new();
        private readonly List<Renderer> hiddenRenderers = new();

        public bool IsPlaying { get; private set; }
        public Camera PresentationCamera => presentationCamera;

        public bool Begin(Camera sourceCamera)
        {
            if (IsPlaying) return true;
            Rocket rocket = GetComponentInParent<Rocket>();
            if (rocket == null || parachutePrefab == null || swayClip == null || sourceCamera == null)
            {
                Debug.LogWarning("Mission success presentation requires a parachute, sway clip and camera.", this);
                return false;
            }

            rocketTransform = rocket.transform;
            originalParent = rocketTransform.parent;
            originalPosition = rocketTransform.localPosition;
            originalRotation = rocketTransform.localRotation;
            originalScale = rocketTransform.localScale;
            rocket.StopFlight();

            rig = Instantiate(parachutePrefab, rocketTransform.position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(rig, gameObject.scene);
            descent = rig.transform.Find("DescentRoot");
            Transform socket = rig.transform.Find("DescentRoot/SwayPivot/RocketSwingPivot/RocketSocket");
            Animator animator = descent != null ? descent.GetComponent<Animator>() : null;
            if (socket == null || animator == null)
            {
                Debug.LogWarning("Mission success parachute has no socket or animator.", this);
                Destroy(rig);
                rig = null;
                return false;
            }

            IsPlaying = true;
            rocketTransform.rotation = Quaternion.identity;
            Bounds body = CalculateBounds(rocket.gameObject, true);
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
            presentationCamera.enabled = true;

            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (Camera camera in root.GetComponentsInChildren<Camera>())
                    if (camera != presentationCamera) Suspend(camera);
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>()) Suspend(canvas);
                foreach (RocketBuilder builder in root.GetComponentsInChildren<RocketBuilder>()) Suspend(builder);
                foreach (SkyEnvironment sky in root.GetComponentsInChildren<SkyEnvironment>()) Suspend(sky);
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
            Evaluate(0f);
            return true;
        }

        public void Evaluate(float elapsedSeconds)
        {
            if (!IsPlaying) return;
            float elapsed = Mathf.Clamp(elapsedSeconds, 0f, Duration);
            sway.SetTime(elapsed % Mathf.Max(0.001f, swayClip.length));
            graph.Evaluate(0f);
            descent.localPosition = Vector3.Lerp(startPosition, endPosition, elapsed / Duration);
        }

        public void End()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
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
