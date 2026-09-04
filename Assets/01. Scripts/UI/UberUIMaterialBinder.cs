using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Border.UI
{
    /// <summary>
    /// Supplies atlas bounds to Shader/Uber/UI without changing the shared material.
    /// Outward outline/glow padding is intentionally limited to Simple Images with
    /// Use Sprite Mesh disabled; sliced, tiled, filled, tight-mesh, and non-Image
    /// graphics keep zero padding so their geometry cannot be distorted.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("UI/Uber UI Material Binder")]
    public sealed class UberUIMaterialBinder : UIBehaviour, IMaterialModifier
    {
        private const string TargetShaderName = "Shader/Uber/UI";
        private const float MaximumPaddingPixels = 8f;

        private static readonly Vector4 FullUVRect = new Vector4(0f, 0f, 1f, 1f);
        private static readonly int BaseSpriteUVRectId =
            Shader.PropertyToID("_BaseSpriteUVRect");
        private static readonly int OutlineMeshPaddingId =
            Shader.PropertyToID("_PixelOutlineMeshPadding");
        private static readonly int OutlineEnabledId =
            Shader.PropertyToID("_PixelOutlineEnabled");
        private static readonly int OutlineWidthId =
            Shader.PropertyToID("_PixelOutlineWidth");
        private static readonly int GlowWidthId =
            Shader.PropertyToID("_PixelGlowWidth");
        private static readonly int StencilCompId = Shader.PropertyToID("_StencilComp");
        private static readonly int StencilId = Shader.PropertyToID("_Stencil");
        private static readonly int StencilOpId = Shader.PropertyToID("_StencilOp");
        private static readonly int StencilWriteMaskId =
            Shader.PropertyToID("_StencilWriteMask");
        private static readonly int StencilReadMaskId =
            Shader.PropertyToID("_StencilReadMask");
        private static readonly int ColorMaskId = Shader.PropertyToID("_ColorMask");
        private static readonly int UseUIAlphaClipId =
            Shader.PropertyToID("_UseUIAlphaClip");

        private Graphic _graphic;
        private Image _image;
        private Material _sourceMaterial;
        private Material _modifiedMaterial;
        private Sprite _trackedSprite;
        private Material _trackedBaseMaterial;
        private int _trackedBaseMaterialCrc;
        private Vector4 _spriteUVRect = FullUVRect;
        private Vector4 _outlinePadding;

        private Graphic TargetGraphic
        {
            get
            {
                if (_graphic == null)
                    _graphic = GetComponent<Graphic>();

                return _graphic;
            }
        }

        private Image TargetImage
        {
            get
            {
                if (_image == null)
                    _image = GetComponent<Image>();

                return _image;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh(forceDirty: true);
        }

        protected override void OnDisable()
        {
            ReleaseModifiedMaterial();
            MarkMaterialDirty();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            ReleaseModifiedMaterial();
            base.OnDestroy();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            Refresh(forceDirty: true);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Refresh(forceDirty: false);
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            Refresh(forceDirty: true);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            Refresh(forceDirty: true);
        }
#endif

        private void LateUpdate()
        {
            Refresh(forceDirty: false);
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!CanModify(baseMaterial))
            {
                ReleaseModifiedMaterial();
                return baseMaterial;
            }

            if (_modifiedMaterial == null || _sourceMaterial != baseMaterial ||
                _modifiedMaterial.shader != baseMaterial.shader)
            {
                ReleaseModifiedMaterial();
                _sourceMaterial = baseMaterial;
                _modifiedMaterial = new Material(baseMaterial.shader)
                {
                    name = baseMaterial.name + " (Uber UI Bound)",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            // The incoming material may be uGUI's stencil material. Copying it
            // preserves the complete Mask state before applying binder values.
            _modifiedMaterial.CopyPropertiesFromMaterial(baseMaterial);
            _modifiedMaterial.enabledKeywords = baseMaterial.enabledKeywords;

            Material sourceMaterial = TargetGraphic.material;
            if (sourceMaterial != baseMaterial && sourceMaterial != null &&
                sourceMaterial.shader == baseMaterial.shader)
            {
                // StencilMaterial caches its clone, so refresh ordinary properties
                // from the live source and then restore uGUI-owned stencil state.
                _modifiedMaterial.CopyPropertiesFromMaterial(sourceMaterial);
                _modifiedMaterial.enabledKeywords = sourceMaterial.enabledKeywords;
                _modifiedMaterial.SetFloat(StencilCompId,
                    baseMaterial.GetFloat(StencilCompId));
                _modifiedMaterial.SetFloat(StencilId, baseMaterial.GetFloat(StencilId));
                _modifiedMaterial.SetFloat(StencilOpId, baseMaterial.GetFloat(StencilOpId));
                _modifiedMaterial.SetFloat(StencilWriteMaskId,
                    baseMaterial.GetFloat(StencilWriteMaskId));
                _modifiedMaterial.SetFloat(StencilReadMaskId,
                    baseMaterial.GetFloat(StencilReadMaskId));
                _modifiedMaterial.SetFloat(ColorMaskId, baseMaterial.GetFloat(ColorMaskId));
                _modifiedMaterial.SetFloat(UseUIAlphaClipId,
                    baseMaterial.GetFloat(UseUIAlphaClipId));

                if (baseMaterial.IsKeywordEnabled("UNITY_UI_ALPHACLIP"))
                    _modifiedMaterial.EnableKeyword("UNITY_UI_ALPHACLIP");
                else
                    _modifiedMaterial.DisableKeyword("UNITY_UI_ALPHACLIP");
            }

            _modifiedMaterial.SetVector(BaseSpriteUVRectId, _spriteUVRect);
            _modifiedMaterial.SetVector(OutlineMeshPaddingId, _outlinePadding);
            return _modifiedMaterial;
        }

        private void Refresh(bool forceDirty)
        {
            Graphic graphic = TargetGraphic;
            if (graphic == null)
                return;

            Sprite sprite = CurrentSprite();
            Vector4 uvRect = GetSpriteUVRect(sprite);
            Material baseMaterial = graphic.material;
            int baseMaterialCrc = baseMaterial != null ? baseMaterial.ComputeCRC() : 0;
            Vector4 padding = CalculateOutlinePadding(baseMaterial, sprite, uvRect);

            bool changed = forceDirty || sprite != _trackedSprite ||
                baseMaterial != _trackedBaseMaterial ||
                baseMaterialCrc != _trackedBaseMaterialCrc ||
                !Approximately(uvRect, _spriteUVRect) ||
                !Approximately(padding, _outlinePadding);

            _trackedSprite = sprite;
            _trackedBaseMaterial = baseMaterial;
            _trackedBaseMaterialCrc = baseMaterialCrc;
            _spriteUVRect = uvRect;
            _outlinePadding = padding;

            if (!changed)
                return;

            if (_modifiedMaterial != null)
            {
                _modifiedMaterial.SetVector(BaseSpriteUVRectId, _spriteUVRect);
                _modifiedMaterial.SetVector(OutlineMeshPaddingId, _outlinePadding);
            }

            graphic.SetMaterialDirty();
        }

        private Sprite CurrentSprite()
        {
            Image image = TargetImage;
            if (image == null)
                return null;

            return image.overrideSprite != null ? image.overrideSprite : image.sprite;
        }

        private Vector4 CalculateOutlinePadding(Material material, Sprite sprite,
            Vector4 uvRect)
        {
            Image image = TargetImage;
            Graphic graphic = TargetGraphic;
            if (material == null || graphic == null || sprite == null ||
                image == null || image.type != Image.Type.Simple || image.useSpriteMesh ||
                !material.HasProperty(OutlineMeshPaddingId) ||
                !material.HasProperty(OutlineEnabledId) ||
                material.GetFloat(OutlineEnabledId) <= 0.5f)
            {
                return Vector4.zero;
            }

            float outlineWidth = material.HasProperty(OutlineWidthId)
                ? material.GetFloat(OutlineWidthId)
                : 0f;
            float glowWidth = material.HasProperty(GlowWidthId)
                ? material.GetFloat(GlowWidthId)
                : 0f;
            float radius = Mathf.Clamp(
                Mathf.Max(outlineWidth, glowWidth), 0f, MaximumPaddingPixels);
            Texture texture = graphic.mainTexture;
            if (radius <= 0f || texture == null || texture.width <= 0 || texture.height <= 0)
                return Vector4.zero;

            Vector2 uvPadding = new Vector2(
                radius / texture.width,
                radius / texture.height);
            Vector2 drawSize = GetSimpleImageDrawSize(image, sprite);
            Vector2 localPadding = new Vector2(
                drawSize.x * uvPadding.x / Mathf.Max(uvRect.z, 0.00001f),
                drawSize.y * uvPadding.y / Mathf.Max(uvRect.w, 0.00001f));

            return new Vector4(
                localPadding.x, localPadding.y, uvPadding.x, uvPadding.y);
        }

        private static Vector2 GetSimpleImageDrawSize(Image image, Sprite sprite)
        {
            Vector2 drawSize = image.rectTransform.rect.size;
            if (!image.preserveAspect || sprite.rect.height <= 0f ||
                drawSize.x <= 0f || drawSize.y <= 0f)
            {
                return drawSize;
            }

            float spriteRatio = sprite.rect.width / sprite.rect.height;
            float rectRatio = drawSize.x / drawSize.y;
            if (spriteRatio > rectRatio)
                drawSize.y = drawSize.x / spriteRatio;
            else
                drawSize.x = drawSize.y * spriteRatio;

            return drawSize;
        }

        private static Vector4 GetSpriteUVRect(Sprite sprite)
        {
            if (sprite == null)
                return FullUVRect;

            Vector4 outerUV = DataUtility.GetOuterUV(sprite);
            float width = outerUV.z - outerUV.x;
            float height = outerUV.w - outerUV.y;
            if (width <= 0f || height <= 0f)
                return FullUVRect;

            return new Vector4(outerUV.x, outerUV.y, width, height);
        }

        private static bool CanModify(Material material)
        {
            return material != null && material.shader != null &&
                material.shader.name == TargetShaderName &&
                material.HasProperty(BaseSpriteUVRectId) &&
                material.HasProperty(OutlineMeshPaddingId);
        }

        private static bool Approximately(Vector4 left, Vector4 right)
        {
            return Mathf.Approximately(left.x, right.x) &&
                Mathf.Approximately(left.y, right.y) &&
                Mathf.Approximately(left.z, right.z) &&
                Mathf.Approximately(left.w, right.w);
        }

        private void MarkMaterialDirty()
        {
            if (_graphic != null)
                _graphic.SetMaterialDirty();
        }

        private void ReleaseModifiedMaterial()
        {
            if (_modifiedMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(_modifiedMaterial);
                else
                    DestroyImmediate(_modifiedMaterial);
            }

            _modifiedMaterial = null;
            _sourceMaterial = null;
        }
    }
}
