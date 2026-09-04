using UnityEngine;

namespace Border.Rendering
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class UberSpritePropertyBinder : MonoBehaviour
    {
        private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseSpriteUvRectId = Shader.PropertyToID("_BaseSpriteUVRect");
        private static readonly int SecondaryTextureId = Shader.PropertyToID("_SecondaryTex");
        private static readonly int SecondaryBlendAmountId = Shader.PropertyToID("_SecondaryBlendAmount");
        private static readonly int SecondaryUvRectId = Shader.PropertyToID("_SecondaryUVRect");
        private static readonly Vector4 FullUvRect = new Vector4(0f, 0f, 1f, 1f);

        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite secondarySprite;
        [SerializeField, Range(0f, 1f)] private float secondaryBlendAmount;

        private MaterialPropertyBlock propertyBlock;
        private Sprite appliedBaseSprite;
        private Sprite appliedSecondarySprite;
        private Texture appliedBaseTexture;
        private Texture appliedSecondaryTexture;
        private float appliedBlendAmount = -1f;
        private bool isDirty = true;

        public SpriteRenderer TargetRenderer
        {
            get => targetRenderer;
            set
            {
                if (targetRenderer == value)
                    return;

                targetRenderer = value;
                Invalidate();
                ApplyIfNeeded(true);
            }
        }

        public Sprite SecondarySprite
        {
            get => secondarySprite;
            set
            {
                if (secondarySprite == value)
                    return;

                secondarySprite = value;
                Invalidate();
                ApplyIfNeeded(true);
            }
        }

        public float SecondaryBlendAmount
        {
            get => secondaryBlendAmount;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(secondaryBlendAmount, clamped))
                    return;

                secondaryBlendAmount = clamped;
                isDirty = true;
                ApplyIfNeeded(true);
            }
        }

        public float EffectiveSecondaryBlendAmount =>
            secondarySprite == null ? 0f : Mathf.Clamp01(secondaryBlendAmount);

        public void Refresh()
        {
            Invalidate();
            ApplyIfNeeded(true);
        }

        private void Reset()
        {
            targetRenderer = GetComponent<SpriteRenderer>();
            Invalidate();
        }

        private void OnEnable()
        {
            ResolveRenderer();
            Refresh();
        }

        private void OnValidate()
        {
            secondaryBlendAmount = Mathf.Clamp01(secondaryBlendAmount);
            Invalidate();
            if (isActiveAndEnabled)
                ApplyIfNeeded(true);
        }

        private void OnDidApplyAnimationProperties()
        {
            isDirty = true;
            ApplyIfNeeded(true);
        }

        private void LateUpdate()
        {
            ResolveRenderer();
            ApplyIfNeeded(false);
        }

        private void ResolveRenderer()
        {
            if (targetRenderer == null)
                TryGetComponent(out targetRenderer);
        }

        private void Invalidate()
        {
            isDirty = true;
            appliedBaseSprite = null;
            appliedSecondarySprite = null;
            appliedBaseTexture = null;
            appliedSecondaryTexture = null;
            appliedBlendAmount = -1f;
        }

        private void ApplyIfNeeded(bool force)
        {
            ResolveRenderer();
            if (targetRenderer == null)
                return;

            Sprite baseSprite = targetRenderer.sprite;
            Texture baseTexture = baseSprite != null
                ? baseSprite.texture
                : Texture2D.whiteTexture;
            Texture secondaryTexture = secondarySprite != null
                ? secondarySprite.texture
                : Texture2D.whiteTexture;
            float blendAmount = EffectiveSecondaryBlendAmount;

            if (!force && !isDirty && baseSprite == appliedBaseSprite &&
                secondarySprite == appliedSecondarySprite &&
                baseTexture == appliedBaseTexture &&
                secondaryTexture == appliedSecondaryTexture &&
                Mathf.Approximately(blendAmount, appliedBlendAmount))
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(MainTextureId, baseTexture);
            propertyBlock.SetVector(BaseSpriteUvRectId, GetSpriteUvRect(baseSprite));
            propertyBlock.SetTexture(SecondaryTextureId, secondaryTexture);
            propertyBlock.SetVector(SecondaryUvRectId,
                secondarySprite != null ? GetSpriteUvRect(secondarySprite) : FullUvRect);
            propertyBlock.SetFloat(SecondaryBlendAmountId, blendAmount);
            targetRenderer.SetPropertyBlock(propertyBlock);

            appliedBaseSprite = baseSprite;
            appliedSecondarySprite = secondarySprite;
            appliedBaseTexture = baseTexture;
            appliedSecondaryTexture = secondaryTexture;
            appliedBlendAmount = blendAmount;
            isDirty = false;
        }

        private static Vector4 GetSpriteUvRect(Sprite sprite)
        {
            if (sprite == null)
                return FullUvRect;

            Vector2[] uvs = sprite.uv;
            if (uvs == null || uvs.Length == 0)
                return FullUvRect;

            Vector2 minimum = uvs[0];
            Vector2 maximum = uvs[0];
            for (int index = 1; index < uvs.Length; ++index)
            {
                minimum = Vector2.Min(minimum, uvs[index]);
                maximum = Vector2.Max(maximum, uvs[index]);
            }

            Vector2 size = maximum - minimum;
            return new Vector4(minimum.x, minimum.y,
                Mathf.Max(size.x, 0.00001f), Mathf.Max(size.y, 0.00001f));
        }
    }
}
