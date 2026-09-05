using UnityEngine;
using UnityEngine.UI;

namespace Border.Research
{
    // BreakShoot SlotMachine/ButtonClick: two particle layers, rendered in the overlay Canvas.
    // ParticleSystem owns simulation; the UI graphic only projects its local XY quads.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IgnitionClickParticles : MaskableGraphic
    {
        private readonly ParticleSystem.Particle[] particles = new ParticleSystem.Particle[64];
        private readonly UIVertex[] quad = new UIVertex[4];
        private ParticleSystem system;
        private Texture texture;
        private bool flipbook;
        private IgnitionClickParticles dots;
        private bool hadParticles;

        public override Texture mainTexture => texture != null ? texture : Texture2D.whiteTexture;

        public static IgnitionClickParticles Create(RectTransform button)
        {
            var burst = CreateLayer(button, "ButtonClickBurst", true);
            burst.dots = CreateLayer(button, "ButtonClickDots", false);
            return burst;
        }

        private static IgnitionClickParticles CreateLayer(RectTransform parent, string name, bool atlas)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 420f);
            var graphic = go.AddComponent<IgnitionClickParticles>();
            graphic.raycastTarget = false;
            graphic.flipbook = atlas;
            graphic.texture = Resources.Load<Texture2D>(atlas ? "Ignition/ButtonEffect" : "Ignition/Circle");
            graphic.system = go.AddComponent<ParticleSystem>();
            var ps = graphic.system;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.1f;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 64;
            main.startLifetime = new ParticleSystem.MinMaxCurve(atlas ? 0.3f : 0.4f, atlas ? 0.6f : 0.8f);
            main.startSpeed = atlas ? 240f : 300f;
            main.startSize = new ParticleSystem.MinMaxCurve(atlas ? 18f : 3f, atlas ? 42f : 7f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var emission = ps.emission;
            emission.rateOverTime = atlas ? 150f : 300f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = atlas ? 3f : 0.3f;
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0f;
            limit.dampen = 0.08f;
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1294f),
                    new GradientAlphaKey(1f, atlas ? 0.6118f : 0.45f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));
            // World-space rendering would be hidden behind ScreenSpaceOverlay UI.
            go.GetComponent<ParticleSystemRenderer>().enabled = false;
            return graphic;
        }

        public void Play()
        {
            if (system == null) return;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play();
            dots?.Play();
        }

        public void Stop()
        {
            if (system != null) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dots?.Stop();
            hadParticles = false;
            SetVerticesDirty();
        }

        private void LateUpdate()
        {
            bool alive = system != null && system.IsAlive();
            if (alive || hadParticles) SetVerticesDirty();
            hadParticles = alive;
        }

        protected override void OnDisable()
        {
            Stop();
            base.OnDisable();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (system == null) return;
            int count = system.GetParticles(particles);
            for (int i = 0; i < count; i++)
            {
                var particle = particles[i];
                float age = 1f - particle.remainingLifetime / particle.startLifetime;
                int frame = Mathf.Min(3, (int)(age * 4f));
                float cell = flipbook ? 0.5f : 1f;
                float u = flipbook ? frame % 2 * cell : 0f;
                float v = flipbook ? (1 - frame / 2) * cell : 0f;
                float half = particle.GetCurrentSize(system) * 0.5f;
                Quaternion rotation = Quaternion.Euler(0f, 0f, particle.rotation);
                for (int j = 0; j < 4; j++)
                {
                    float x = j == 1 || j == 2 ? 1f : 0f;
                    float y = j >= 2 ? 1f : 0f;
                    quad[j] = UIVertex.simpleVert;
                    quad[j].position = particle.position + rotation * new Vector3((x * 2f - 1f) * half, (y * 2f - 1f) * half, 0f);
                    quad[j].color = particle.GetCurrentColor(system);
                    quad[j].uv0 = new Vector2(u + x * cell, v + y * cell);
                }
                vh.AddUIVertexQuad(quad);
            }
        }
    }
}
