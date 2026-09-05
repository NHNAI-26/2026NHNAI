using System;
using System.Collections;
using System.Collections.Generic;
using Border.Research;
using UnityEngine;

namespace Simulation
{
    [DisallowMultipleComponent]
    public sealed class LaunchPhotoCapture : MonoBehaviour
    {
        private Rocket rocket;
        private Camera sourceCamera;
        private ResearchFlowSession session;
        private bool requested;
        public bool IsCapturing { get; private set; }

        public void Initialize(Rocket target, Camera camera, ResearchFlowSession owner)
        {
            rocket = target;
            sourceCamera = camera;
            session = owner;
            rocket.LaunchStarted += ResetCapture;
            rocket.ExplosionPhotoRequested += CaptureExplosion;
        }

        private void ResetCapture() => requested = false;

        public void CaptureOutcome()
        {
            if (requested || session == null || !session.HasActiveLaunch) return;
            requested = true;
            Capture(session.LaunchPhotoGeneration);
        }

        private void CaptureExplosion(bool hasEffect)
        {
            if (requested || session == null || !session.HasActiveLaunch) return;
            requested = true;
            if (hasEffect)
            {
                IsCapturing = true;
                StartCoroutine(CaptureNextFrame(session.LaunchPhotoGeneration));
            }
            else Capture(session.LaunchPhotoGeneration);
        }

        private IEnumerator CaptureNextFrame(int generation)
        {
            yield return null;
            try { Capture(generation); }
            finally { IsCapturing = false; }
        }

        private void Capture(int generation)
        {
            Texture2D photo = null;
            try
            {
                if (session == null || generation != session.LaunchPhotoGeneration) return;
                photo = RenderPhoto(sourceCamera, rocket.transform);
                session.SetLaunchPhoto(photo, generation);
                photo = null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Launch photograph unavailable: {exception.Message}", this);
            }
            finally
            {
                if (photo != null) DisposeObject(photo);
            }
        }

        public static Texture2D RenderPhoto(Camera source, Transform subject)
        {
            if (source == null || subject == null) return null;
            var hiddenCanvases = new List<Canvas>();
            var hiddenLines = new List<LineRenderer>();
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = null;
            GameObject cameraObject = null;
            Texture2D photo = null;
            try
            {
                cameraObject = new GameObject("Launch Photo Camera") { hideFlags = HideFlags.HideAndDontSave };
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.CopyFrom(source);
                camera.enabled = false;
                camera.rect = new Rect(0f, 0f, 1f, 1f);
                camera.aspect = 4f / 3f;
                camera.cullingMask &= ~(1 << 5);
                camera.nearClipPlane = 0.1f;
                Bounds bounds = new Bounds(subject.position, new Vector3(2f, 4f, 2f));
                bool found = false;
                foreach (Renderer renderer in subject.GetComponentsInChildren<Renderer>(true))
                {
                    if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                    if (!found) { bounds = renderer.bounds; found = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                float radius = Mathf.Max(1f, bounds.extents.magnitude);
                float distance = radius * 1.3f / Mathf.Sin(Mathf.Clamp(camera.fieldOfView, 10f, 120f) * Mathf.Deg2Rad * 0.5f);
                camera.transform.SetPositionAndRotation(bounds.center - source.transform.forward * distance, source.transform.rotation);
                camera.farClipPlane = Mathf.Max(source.farClipPlane, distance + radius * 4f);
                if (camera.orthographic) camera.orthographicSize = radius * 1.3f;
                target = RenderTexture.GetTemporary(1024, 768, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;

                // World-space canvases and editor guides can share world layers with the rocket.
                foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (canvas.enabled) { hiddenCanvases.Add(canvas); canvas.enabled = false; }
                foreach (LineRenderer line in FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
                    if (line.enabled) { hiddenLines.Add(line); line.enabled = false; }
                camera.Render();
                RenderTexture.active = target;
                photo = new Texture2D(1024, 768, TextureFormat.RGB24, false) { name = "Launch photograph" };
                photo.ReadPixels(new Rect(0, 0, 1024, 768), 0, 0, false);
                photo.Apply(false, false);
                Texture2D result = photo;
                photo = null;
                return result;
            }
            finally
            {
                foreach (Canvas canvas in hiddenCanvases) if (canvas != null) canvas.enabled = true;
                foreach (LineRenderer line in hiddenLines) if (line != null) line.enabled = true;
                RenderTexture.active = previous;
                if (cameraObject != null)
                {
                    cameraObject.GetComponent<Camera>().targetTexture = null;
                    DisposeObject(cameraObject);
                }
                if (target != null) RenderTexture.ReleaseTemporary(target);
                if (photo != null) DisposeObject(photo);
            }
        }

        private void OnDestroy()
        {
            IsCapturing = false;
            if (rocket == null) return;
            rocket.LaunchStarted -= ResetCapture;
            rocket.ExplosionPhotoRequested -= CaptureExplosion;
        }

        private static void DisposeObject(UnityEngine.Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
