using UnityEditor;
using UnityEngine;
using TMPro;

public sealed class UberGroupDrawer : MaterialPropertyDrawer
{
    private readonly string group;

    public UberGroupDrawer(string group)
    {
        this.group = group;
    }

    public override void OnGUI(Rect position, MaterialProperty property,
        GUIContent label, MaterialEditor editor)
    {
        if (!LWGUI.Helper.IsVisible(group))
            return;

        bool indent = !string.IsNullOrEmpty(group) && group != "_";
        if (indent)
            ++EditorGUI.indentLevel;
        try
        {
            editor.DefaultShaderProperty(position, property, label.text);
        }
        finally
        {
            if (indent)
                --EditorGUI.indentLevel;
        }
    }

    public override float GetPropertyHeight(MaterialProperty property,
        string label, MaterialEditor editor)
    {
        return LWGUI.Helper.IsVisible(group)
            ? MaterialEditor.GetDefaultPropertyHeight(property)
            : 0.0f;
    }
}

internal static class UberDrawerLayout
{
    private const float PropertyLabelRatio = 0.42f;
    private const float MaximumPropertyLabelWidth = 340.0f;
    private const float ComponentSpacing = 6.0f;

    internal static Rect DrawPropertyLabel(Rect position, GUIContent label)
    {
        float labelWidth = Mathf.Max(position.width * PropertyLabelRatio, 120.0f);
        labelWidth = Mathf.Min(labelWidth, position.width * 0.55f,
            MaximumPropertyLabelWidth);
        Rect labelPosition = new Rect(position.x, position.y,
            Mathf.Max(labelWidth - ComponentSpacing, 0.0f), position.height);
        EditorGUI.LabelField(labelPosition, label);
        return new Rect(position.x + labelWidth, position.y,
            Mathf.Max(position.width - labelWidth, 0.0f), position.height);
    }

    internal static Vector4 DrawFloatComponents(Rect position,
        GUIContent[] labels, Vector4 values, int componentCount,
        float componentLabelWidth)
    {
        componentCount = Mathf.Min(Mathf.Clamp(componentCount, 0, 4),
            labels.Length);
        if (componentCount == 0)
            return values;

        float totalSpacing = ComponentSpacing * (componentCount - 1);
        float componentWidth = Mathf.Max(
            (position.width - totalSpacing) / componentCount, 0.0f);
        int indentLevel = EditorGUI.indentLevel;
        float labelWidth = EditorGUIUtility.labelWidth;
        float fieldWidth = EditorGUIUtility.fieldWidth;
        EditorGUI.indentLevel = 0;
        try
        {
            for (int index = 0; index < componentCount; ++index)
            {
                Rect componentPosition = new Rect(
                    position.x + index * (componentWidth + ComponentSpacing),
                    position.y, componentWidth, position.height);
                float visibleLabelWidth = Mathf.Min(componentLabelWidth,
                    componentPosition.width * 0.4f);
                EditorGUIUtility.labelWidth = visibleLabelWidth;
                EditorGUIUtility.fieldWidth = Mathf.Max(
                    componentPosition.width - visibleLabelWidth, 0.0f);
                values[index] = EditorGUI.FloatField(
                    componentPosition, labels[index], values[index]);
            }
        }
        finally
        {
            EditorGUI.indentLevel = indentLevel;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
        }

        return values;
    }
}

public sealed class UberVector2Drawer : LWGUI.SubDrawer
{
    private static readonly GUIContent[] ComponentLabels =
    {
        new GUIContent("X"),
        new GUIContent("Y"),
    };

    public UberVector2Drawer(string group) : base(group) { }

    protected override bool IsMatchPropType() =>
        prop.propertyType == UnityEngine.Rendering.ShaderPropertyType.Vector;

    public override void DrawProp(Rect position, MaterialProperty property,
        GUIContent label, MaterialEditor editor)
    {
        Vector4 storedValue = property.vectorValue;
        Rect valuePosition = UberDrawerLayout.DrawPropertyLabel(position, label);

        EditorGUI.showMixedValue = property.hasMixedValue;
        EditorGUI.BeginChangeCheck();
        Vector4 editedValue = UberDrawerLayout.DrawFloatComponents(
            valuePosition, ComponentLabels, storedValue, 2, 16.0f);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
            property.vectorValue = editedValue;
    }
}

public sealed class UberVector3Drawer : LWGUI.SubDrawer
{
    private static readonly GUIContent[] ComponentLabels =
    {
        new GUIContent("X"),
        new GUIContent("Y"),
        new GUIContent("Z"),
    };

    public UberVector3Drawer(string group) : base(group) { }

    protected override bool IsMatchPropType() =>
        prop.propertyType == UnityEngine.Rendering.ShaderPropertyType.Vector;

    public override void DrawProp(Rect position, MaterialProperty property,
        GUIContent label, MaterialEditor editor)
    {
        Vector4 storedValue = property.vectorValue;
        Rect valuePosition = UberDrawerLayout.DrawPropertyLabel(position, label);

        EditorGUI.showMixedValue = property.hasMixedValue;
        EditorGUI.BeginChangeCheck();
        Vector4 editedValue = UberDrawerLayout.DrawFloatComponents(
            valuePosition, ComponentLabels, storedValue, 3, 16.0f);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
            property.vectorValue = editedValue;
    }
}

public sealed class UberMinMaxVectorDrawer : LWGUI.SubDrawer
{
    private const float RowHeight = 18.0f, RowSpacing = 2.0f;
    private static readonly GUIContent[] ComponentLabels =
    {
        new GUIContent("Min"),
        new GUIContent("Max"),
    };
    private static readonly GUIContent CurrentBaseRadiusLabel =
        new GUIContent("Current Base Radius");
    private readonly string amountPropertyName;

    public UberMinMaxVectorDrawer(string group) : base(group) { }

    public UberMinMaxVectorDrawer(string group, string amountPropertyName)
        : base(group) => this.amountPropertyName = amountPropertyName;

    private bool HasAmountBinding => !string.IsNullOrEmpty(amountPropertyName) &&
        amountPropertyName != "_";

    protected override float GetVisibleHeight() => HasAmountBinding
        ? RowHeight * 2.0f + RowSpacing : base.GetVisibleHeight();

    protected override bool IsMatchPropType() =>
        prop.propertyType == UnityEngine.Rendering.ShaderPropertyType.Vector;

    public override void DrawProp(Rect position, MaterialProperty property,
        GUIContent label, MaterialEditor editor)
    {
        Rect rangePosition = HasAmountBinding ? new Rect(
            position.x, position.y, position.width, RowHeight) : position;
        Vector4 storedValue = property.vectorValue;
        Rect valuePosition = UberDrawerLayout.DrawPropertyLabel(rangePosition, label);

        EditorGUI.showMixedValue = property.hasMixedValue;
        EditorGUI.BeginChangeCheck();
        Vector4 editedValue = UberDrawerLayout.DrawFloatComponents(
            valuePosition, ComponentLabels, storedValue, 2, 32.0f);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
            property.vectorValue = editedValue;

        if (HasAmountBinding)
            DrawAmountPreview(position, property);
    }

    private void DrawAmountPreview(Rect position, MaterialProperty rangeProperty)
    {
        MaterialProperty amountProperty = LWGUI.LWGUI.FindProp(amountPropertyName,
            props, true);
        if (amountProperty == null)
            return;

        Rect row = new Rect(position.x, position.y + RowHeight + RowSpacing,
            position.width, RowHeight);
        Rect controls = UberDrawerLayout.DrawPropertyLabel(row,
            CurrentBaseRadiusLabel);
        const float spacing = 4.0f;
        float controlWidth = Mathf.Max((controls.width - spacing * 2.0f) / 3.0f, 0.0f);
        Rect currentRect = new Rect(controls.x, controls.y, controlWidth, controls.height);
        Rect minRect = new Rect(currentRect.xMax + spacing, controls.y,
            controlWidth, controls.height);
        Rect maxRect = new Rect(minRect.xMax + spacing, controls.y,
            controlWidth, controls.height);

        EditorGUI.showMixedValue = rangeProperty.hasMixedValue || amountProperty.hasMixedValue;
        using (new EditorGUI.DisabledScope(true))
            EditorGUI.FloatField(currentRect, CalculateCurrentBaseRadius(
                rangeProperty.vectorValue, amountProperty.floatValue));
        EditorGUI.showMixedValue = false;

        if (GUI.Button(minRect, "Preview Min"))
            SetPreviewAmount(amountProperty, 0.0f);
        if (GUI.Button(maxRect, "Preview Max"))
            SetPreviewAmount(amountProperty, 1.0f);
    }

    private static float CalculateCurrentBaseRadius(Vector4 range, float amount) =>
        Mathf.Lerp(range.x, range.y, Mathf.Clamp01(amount));

    private static void SetPreviewAmount(MaterialProperty amountProperty,
        float endpoint) => amountProperty.floatValue = endpoint;
}

public sealed class UberGradientDrawer : LWGUI.SubDrawer
{
    private const int MaximumKeyCount = 4;
    private static readonly string[] PackedPropertyNames =
    {
        "_DissolveEdgeGradientColor0",
        "_DissolveEdgeGradientColor1",
        "_DissolveEdgeGradientColor2",
        "_DissolveEdgeGradientColor3",
        "_DissolveEdgeGradientAlphas",
        "_DissolveEdgeGradientAlphaTimes",
        "_DissolveEdgeGradientMetadata",
    };

    public UberGradientDrawer(string group) : base(group) { }

    protected override float GetVisibleHeight() => 40.0f;
    protected override bool IsMatchPropType() =>
        prop.propertyType == UnityEngine.Rendering.ShaderPropertyType.Vector;

    public override void DrawProp(Rect position, MaterialProperty property,
        GUIContent label, MaterialEditor editor)
    {
        bool isEdgeGradient = property.name == PackedPropertyNames[0];
        string[] propertyNames = isEdgeGradient ? PackedPropertyNames :
            BuildPackedPropertyNames(ReadPropertyPrefix(property.name));
        MaterialProperty[] packed = FindPackedPropertiesByName(props,
            propertyNames);
        Rect fieldPosition = new Rect(position.x, position.y,
            position.width, 18.0f);
        Rect messagePosition = new Rect(position.x, position.y + 20.0f,
            position.width, 20.0f);
        if (packed == null)
        {
            EditorGUI.HelpBox(position, isEdgeGradient
                    ? "Edge Gradient properties are unavailable."
                    : "HDR Gradient properties are unavailable.",
                MessageType.Error);
            return;
        }

        Gradient gradient = ReadGradient(packed);
        EditorGUI.showMixedValue = HasMixedValue(packed);
        EditorGUI.BeginChangeCheck();
        Gradient edited = EditorGUI.GradientField(fieldPosition, label, gradient,
            true, ColorSpace.Linear);
        EditorGUI.showMixedValue = false;
        bool accepted = true;
        if (EditorGUI.EndChangeCheck())
            accepted = TryWriteGradientInternal(edited, packed, editor,
                isEdgeGradient ? "Edit Edge Gradient" : "Edit HDR Gradient");

        EditorGUI.HelpBox(messagePosition, accepted
                ? "Blend · HDR · maximum 4 color and 4 alpha keys."
                : "Maximum 4 finite color and alpha keys; change was not saved.",
            accepted ? MessageType.Info : MessageType.Error);
    }

    private static string ReadPropertyPrefix(string propertyName)
    {
        const string suffix = "Color0";
        return propertyName != null && propertyName.EndsWith(suffix,
            System.StringComparison.Ordinal)
            ? propertyName.Substring(0, propertyName.Length - suffix.Length)
            : "_DissolveEdgeGradient";
    }

    private static string[] BuildPackedPropertyNames(string propertyPrefix)
    {
        string prefix = string.IsNullOrEmpty(propertyPrefix)
            ? "_DissolveEdgeGradient" : propertyPrefix;
        return new[]
        {
            prefix + "Color0",
            prefix + "Color1",
            prefix + "Color2",
            prefix + "Color3",
            prefix + "Alphas",
            prefix + "AlphaTimes",
            prefix + "Metadata",
        };
    }

    private static MaterialProperty[] FindPackedProperties(
        MaterialProperty[] properties)
    {
        return FindPackedPropertiesByName(properties, PackedPropertyNames);
    }

    private static MaterialProperty[] FindPackedPropertiesByName(
        MaterialProperty[] properties, string[] propertyNames)
    {
        MaterialProperty[] packed = new MaterialProperty[propertyNames.Length];
        for (int index = 0; index < packed.Length; ++index)
        {
            packed[index] = LWGUI.LWGUI.FindProp(propertyNames[index],
                properties, true);
            if (packed[index] == null)
                return null;
        }
        return packed;
    }

    private static Gradient ReadGradient(MaterialProperty[] packed)
    {
        Vector4 metadata = packed[6].vectorValue;
        int colorCount = ReadKeyCount(metadata.x);
        int alphaCount = ReadKeyCount(metadata.y);
        GradientColorKey[] colors = new GradientColorKey[colorCount];
        for (int index = 0; index < colorCount; ++index)
        {
            Vector4 value = packed[index].vectorValue;
            colors[index] = new GradientColorKey(
                new Color(value.x, value.y, value.z, 1.0f),
                ReadTime(value.w, index, colorCount));
        }

        Vector4 alphaValues = packed[4].vectorValue;
        Vector4 alphaTimes = packed[5].vectorValue;
        GradientAlphaKey[] alphas = new GradientAlphaKey[alphaCount];
        for (int index = 0; index < alphaCount; ++index)
            alphas[index] = new GradientAlphaKey(alphaValues[index],
                ReadTime(alphaTimes[index], index, alphaCount));

        Gradient gradient = new Gradient { mode = GradientMode.Blend };
        gradient.SetKeys(colors, alphas);
        return gradient;
    }

    private static bool TryWriteGradient(Gradient gradient,
        MaterialProperty[] packed, MaterialEditor editor)
    {
        return TryWriteGradientInternal(gradient, packed, editor,
            "Edit Edge Gradient");
    }

    private static bool TryWriteGradientInternal(Gradient gradient,
        MaterialProperty[] packed, MaterialEditor editor, string changeUndoLabel)
    {
        GradientColorKey[] colors = gradient.colorKeys;
        GradientAlphaKey[] alphas = gradient.alphaKeys;
        if (!CanStore(colors, alphas))
            return false;

        Vector4[] values = new Vector4[packed.Length];
        for (int index = 0; index < MaximumKeyCount; ++index)
        {
            GradientColorKey key = colors[Mathf.Min(index, colors.Length - 1)];
            values[index] = new Vector4(key.color.r, key.color.g, key.color.b,
                index < colors.Length ? key.time : 1.0f);
        }
        values[4] = Vector4.one;
        values[5] = Vector4.one;
        for (int index = 0; index < alphas.Length; ++index)
        {
            values[4][index] = alphas[index].alpha;
            values[5][index] = alphas[index].time;
        }
        values[6] = new Vector4(colors.Length, alphas.Length, 0.0f, 0.0f);

        editor.RegisterPropertyChangeUndo(changeUndoLabel);
        for (int index = 0; index < packed.Length; ++index)
            packed[index].vectorValue = values[index];
        gradient.mode = GradientMode.Blend;
        return true;
    }

    private static bool CanStore(GradientColorKey[] colors,
        GradientAlphaKey[] alphas)
    {
        if (colors.Length < 1 || colors.Length > MaximumKeyCount ||
            alphas.Length < 1 || alphas.Length > MaximumKeyCount)
            return false;
        for (int index = 0; index < colors.Length; ++index)
            if (!IsFinite(colors[index].color) || !IsFinite(colors[index].time))
                return false;
        for (int index = 0; index < alphas.Length; ++index)
            if (!IsFinite(alphas[index].alpha) || !IsFinite(alphas[index].time))
                return false;
        return true;
    }

    private static bool HasMixedValue(MaterialProperty[] packed)
    {
        for (int index = 0; index < packed.Length; ++index)
            if (packed[index].hasMixedValue)
                return true;
        return false;
    }

    private static int ReadKeyCount(float value) => IsFinite(value)
        ? Mathf.Clamp(Mathf.RoundToInt(value), 1, MaximumKeyCount) : 2;

    private static float ReadTime(float value, int index, int count) =>
        IsFinite(value) ? Mathf.Clamp01(value) :
        (count > 1 ? (float)index / (count - 1) : 0.0f);

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);

    private static bool IsFinite(Color color) => IsFinite(color.r) &&
        IsFinite(color.g) && IsFinite(color.b) && IsFinite(color.a);
}

public sealed class UberParticleCurveDrawer : LWGUI.SubDrawer
{
    private const int MaximumKeyCount = 4;
    private static readonly string[] PackedPropertyNames =
    {
        "_BaseNoiseClipCurveValues",
        "_BaseNoiseClipCurveTimes",
        "_BaseNoiseClipCurveInTangents",
        "_BaseNoiseClipCurveOutTangents",
        "_BaseNoiseClipCurveMetadata",
    };

    public UberParticleCurveDrawer(string group) : base(group) { }

    protected override float GetVisibleHeight() => 40.0f;
    protected override bool IsMatchPropType() =>
        prop.propertyType == UnityEngine.Rendering.ShaderPropertyType.Vector;

    public override void DrawProp(Rect position, MaterialProperty property,
        GUIContent label, MaterialEditor editor)
    {
        MaterialProperty[] packed = FindPackedProperties(props);
        Rect fieldPosition = new Rect(position.x, position.y,
            position.width, 18.0f);
        Rect messagePosition = new Rect(position.x, position.y + 20.0f,
            position.width, 20.0f);
        if (packed == null)
        {
            EditorGUI.HelpBox(position,
                "Base Noise Clip curve properties are unavailable.",
                MessageType.Error);
            return;
        }

        AnimationCurve curve = ReadCurve(packed);
        EditorGUI.showMixedValue = HasMixedValue(packed);
        EditorGUI.BeginChangeCheck();
        AnimationCurve edited = EditorGUI.CurveField(fieldPosition, label,
            curve, new Color(0.2f, 0.85f, 1.0f, 1.0f),
            new Rect(0.0f, 0.0f, 1.0f, 1.0f));
        EditorGUI.showMixedValue = false;
        bool accepted = true;
        if (EditorGUI.EndChangeCheck())
            accepted = TryWriteCurve(edited, packed, editor);

        EditorGUI.HelpBox(messagePosition, accepted
                ? "Threshold remap · 0–1 · unweighted · maximum 4 keys."
                : "Use 1–4 finite, unweighted keys inside 0–1; change was not saved.",
            accepted ? MessageType.Info : MessageType.Error);
    }

    private static MaterialProperty[] FindPackedProperties(
        MaterialProperty[] properties)
    {
        MaterialProperty[] packed = new MaterialProperty[PackedPropertyNames.Length];
        for (int index = 0; index < packed.Length; ++index)
        {
            packed[index] = LWGUI.LWGUI.FindProp(PackedPropertyNames[index],
                properties, true);
            if (packed[index] == null)
                return null;
        }
        return packed;
    }

    private static AnimationCurve ReadCurve(MaterialProperty[] packed)
    {
        int keyCount = ReadKeyCount(packed[4].vectorValue.x);
        Vector4 values = packed[0].vectorValue;
        Vector4 times = packed[1].vectorValue;
        Vector4 inTangents = packed[2].vectorValue;
        Vector4 outTangents = packed[3].vectorValue;
        Keyframe[] keys = new Keyframe[keyCount];
        for (int index = 0; index < keyCount; ++index)
        {
            float fallbackTime = keyCount > 1
                ? (float)index / (keyCount - 1) : 0.0f;
            Keyframe key = new Keyframe(
                ReadNormalized(times[index], fallbackTime),
                ReadNormalized(values[index], fallbackTime),
                ReadFinite(inTangents[index], 0.0f),
                ReadFinite(outTangents[index], 0.0f))
            {
                weightedMode = WeightedMode.None,
            };
            keys[index] = key;
        }

        AnimationCurve curve = new AnimationCurve(keys)
        {
            preWrapMode = WrapMode.ClampForever,
            postWrapMode = WrapMode.ClampForever,
        };
        return curve;
    }

    private static bool TryWriteCurve(AnimationCurve curve,
        MaterialProperty[] packed, MaterialEditor editor)
    {
        Keyframe[] keys = curve != null ? curve.keys : null;
        if (!CanStore(keys))
            return false;

        Vector4 values = Vector4.zero;
        Vector4 times = Vector4.zero;
        Vector4 inTangents = Vector4.zero;
        Vector4 outTangents = Vector4.zero;
        for (int index = 0; index < MaximumKeyCount; ++index)
        {
            Keyframe key = keys[Mathf.Min(index, keys.Length - 1)];
            values[index] = key.value;
            times[index] = key.time;
            inTangents[index] = key.inTangent;
            outTangents[index] = key.outTangent;
        }

        editor.RegisterPropertyChangeUndo("Edit Base Noise Clip Curve");
        packed[0].vectorValue = values;
        packed[1].vectorValue = times;
        packed[2].vectorValue = inTangents;
        packed[3].vectorValue = outTangents;
        packed[4].vectorValue = new Vector4(keys.Length, 0.0f, 0.0f, 0.0f);
        return true;
    }

    private static bool CanStore(Keyframe[] keys)
    {
        if (keys == null || keys.Length < 1 || keys.Length > MaximumKeyCount)
            return false;
        float previousTime = -1.0f;
        for (int index = 0; index < keys.Length; ++index)
        {
            Keyframe key = keys[index];
            if (!IsFinite(key.time) || !IsFinite(key.value) ||
                !IsFinite(key.inTangent) || !IsFinite(key.outTangent) ||
                key.time < 0.0f || key.time > 1.0f || key.value < 0.0f ||
                key.value > 1.0f || key.time <= previousTime ||
                key.weightedMode != WeightedMode.None)
                return false;
            previousTime = key.time;
        }
        return true;
    }

    private static bool HasMixedValue(MaterialProperty[] packed)
    {
        for (int index = 0; index < packed.Length; ++index)
            if (packed[index].hasMixedValue)
                return true;
        return false;
    }

    private static int ReadKeyCount(float value) => IsFinite(value)
        ? Mathf.Clamp(Mathf.RoundToInt(value), 1, MaximumKeyCount) : 2;

    private static float ReadNormalized(float value, float fallback) =>
        IsFinite(value) ? Mathf.Clamp01(value) : Mathf.Clamp01(fallback);

    private static float ReadFinite(float value, float fallback) =>
        IsFinite(value) ? value : fallback;

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);
}

public sealed class UberParticleStreamDrawer : LWGUI.SubDrawer
{
    private static readonly GUIContent[] StreamLabels =
    {
        new GUIContent("TEXCOORD0.x"),
        new GUIContent("TEXCOORD0.y"),
        new GUIContent("TEXCOORD0.z"),
        new GUIContent("TEXCOORD0.w"),
        new GUIContent("TEXCOORD1.x"),
        new GUIContent("TEXCOORD1.y"),
        new GUIContent("TEXCOORD1.z"),
        new GUIContent("TEXCOORD1.w"),
        new GUIContent("TEXCOORD2.x"),
        new GUIContent("TEXCOORD2.y"),
        new GUIContent("TEXCOORD2.z"),
        new GUIContent("TEXCOORD2.w"),
        new GUIContent("TEXCOORD3.x"),
        new GUIContent("TEXCOORD3.y"),
        new GUIContent("TEXCOORD3.z"),
        new GUIContent("TEXCOORD3.w"),
    };

    public UberParticleStreamDrawer(string group) : base(group) { }

    protected override float GetVisibleHeight() => 60.0f;
    protected override bool IsMatchPropType() =>
        prop.propertyType == UnityEngine.Rendering.ShaderPropertyType.Float;

    public override void DrawProp(Rect position, MaterialProperty property,
        GUIContent label, MaterialEditor editor)
    {
        Rect fieldPosition = new Rect(position.x, position.y,
            position.width, 18.0f);
        Rect messagePosition = new Rect(position.x, position.y + 20.0f,
            position.width, 40.0f);
        int current = Mathf.Clamp(Mathf.RoundToInt(property.floatValue), 0,
            StreamLabels.Length - 1);

        EditorGUI.showMixedValue = property.hasMixedValue;
        EditorGUI.BeginChangeCheck();
        int selected = EditorGUI.Popup(fieldPosition, label, current,
            StreamLabels);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
            property.floatValue = selected;

        string streamDescription = property.name == "_BaseNoiseClipStream"
            ? "a normalized clip threshold"
            : "AgePercent";
        EditorGUI.HelpBox(messagePosition,
            "Custom Vertex Streams: route " + streamDescription +
            " to this component. Renderer > GPU Instancing must be disabled.",
            MessageType.Warning);
    }
}

public sealed class UberParticleNoiseChannelDrawer : LWGUI.SubDrawer
{
    private static readonly GUIContent[] ChannelLabels =
    {
        new GUIContent("R"),
        new GUIContent("G"),
        new GUIContent("B"),
        new GUIContent("A"),
        new GUIContent("Luminance"),
    };

    public UberParticleNoiseChannelDrawer(string group) : base(group) { }

    protected override bool IsMatchPropType() =>
        prop.propertyType == UnityEngine.Rendering.ShaderPropertyType.Float;

    public override void DrawProp(Rect position, MaterialProperty property,
        GUIContent label, MaterialEditor editor)
    {
        int current = Mathf.Clamp(Mathf.RoundToInt(property.floatValue), 0,
            ChannelLabels.Length - 1);
        EditorGUI.showMixedValue = property.hasMixedValue;
        EditorGUI.BeginChangeCheck();
        int selected = EditorGUI.Popup(position, label, current, ChannelLabels);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
            property.floatValue = selected;
    }
}

public sealed class UberPostFilterDrawer : LWGUI.SubDrawer
{
    private static readonly GUIContent[] Labels =
        UberShaderGUI.CreatePostFilterLabels();

    public UberPostFilterDrawer(string group) : base(group) { }

    public override void DrawProp(Rect position, MaterialProperty property,
        GUIContent label, MaterialEditor editor)
    {
        int mode = UberShaderGUI.NormalizePostFilterMode(property.floatValue);
        EditorGUI.showMixedValue = property.hasMixedValue;
        EditorGUI.BeginChangeCheck();
        int selectedMode = EditorGUI.Popup(position, label, mode, Labels);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
        {
            property.floatValue = selectedMode;
            foreach (Object target in property.targets)
            {
                if (target is Material material)
                    UberShaderGUI.SynchronizePostFilter(material);
            }
        }

        UberShaderGUI.SetPostFilterVisibility(
            property.hasMixedValue ? 0 : selectedMode);
    }

    public override void Apply(MaterialProperty property)
    {
        base.Apply(property);
        foreach (Object target in property.targets)
        {
            if (target is Material material)
                UberShaderGUI.SynchronizePostFilter(material);
        }
    }
}

public sealed class UberAsciiFontDrawer : LWGUI.SubDrawer
{
    public UberAsciiFontDrawer(string group) : base(group) { }
    protected override float GetVisibleHeight() => 40.0f;

    public override void DrawProp(Rect position, MaterialProperty property,
        GUIContent label, MaterialEditor editor)
    {
        TMP_FontAsset font = null;
        string status = "Multiple font selections · " + UberShaderGUI.AsciiRamp;
        if (!property.hasMixedValue && property.targets.Length > 0 &&
            property.targets[0] is Material material)
            status = UberShaderGUI.SynchronizeAsciiFont(
                material, null, false, out font, false);
        EditorGUI.showMixedValue = property.hasMixedValue;
        EditorGUI.BeginChangeCheck();
        TMP_FontAsset selected = EditorGUI.ObjectField(
            new Rect(position.x, position.y, position.width, 18.0f), label, font,
            typeof(TMP_FontAsset), false) as TMP_FontAsset;
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
            status = UberShaderGUI.AssignAsciiFont(property.targets, selected);
        EditorGUI.HelpBox(new Rect(position.x, position.y + 20.0f,
                position.width, 20.0f), status,
            status.StartsWith("Ready") ? MessageType.Info : MessageType.Warning);
    }
}

public sealed class UberShaderGUI : LWGUI.LWGUI
{
    private readonly struct PostFilterOption
    {
        internal readonly string Label;
        internal readonly string Keyword;

        internal PostFilterOption(string label, string keyword)
        {
            Label = label;
            Keyword = keyword;
        }
    }

    private const string ObjectShader = "Shader/Uber/3D Object";
    private const string SpriteShader = "Shader/Uber/2D Sprite";
    private const string UIShader = "Shader/Uber/UI";
    private const string PostShader = "Shader/Uber/Post Processing";
    private const string ParticleShader = "Shader/Uber/Particle";
    private const string AsciiAtlasProperty = "_AsciiFontAtlas";
    internal const string AsciiRamp = ".,:;ij?7IodS$#@";

    private const int CustomQueueControl = 1;

    private static readonly PostFilterOption[] PostFilterOptions =
    {
        new PostFilterOption("None", null),
        new PostFilterOption("Pixelation", "_PIXELATION_ON"),
        new PostFilterOption("Color Adjustment", "_COLOR_ADJUST_ON"),
        new PostFilterOption("Color Screen Blend", "_COLOR_SCREEN_BLEND_ON"),
        new PostFilterOption("Ordered Dithering", "_ORDERED_DITHER_ON"),
        new PostFilterOption("Color Quantization", "_COLOR_QUANTIZATION_ON"),
        new PostFilterOption("Gradient Map", "_GRADIENT_MAP_ON"),
        new PostFilterOption("Old Film", "_OLD_FILM_ON"),
        new PostFilterOption("Edge Detection / Ink Outline", "_EDGE_FILTER_ON"),
        new PostFilterOption("ASCII Filter", "_ASCII_FILTER_ON"),
        new PostFilterOption("CRT", "_CRT_FILTER_ON"),
    };

    // Structural choices are declared with multi_compile_local by their owning shader.
    private static readonly KeywordBinding[] StructuralKeywords =
    {
        new KeywordBinding("_Surface", "_SURFACE_TYPE_TRANSPARENT", 1),
        new KeywordBinding("_Blend", "_ALPHAMODULATE_ON", 3),
        new KeywordBinding("_AlphaClip", "_ALPHATEST_ON", 1),
        new KeywordBinding("_LightingMode", "_UNLIT_ON", 1),
        new KeywordBinding("_NormalMapEnabled", "_NORMALMAP", 1),
        new KeywordBinding("_MetallicMapEnabled", "_METALLICMAP", 1),
        new KeywordBinding("_SmoothnessMapEnabled", "_SMOOTHNESSMAP", 1),
        new KeywordBinding("_ReceiveShadows", "_RECEIVE_SHADOWS_OFF", 0),
        new KeywordBinding("_UberQuality", "_UBER_QUALITY_LOW", 1),
        new KeywordBinding("_GlitchSpace", "_GLITCH_OBJECT_SPACE", 1,
            "_GlitchEnabled"),
        new KeywordBinding("_GlitchSpace", "_GLITCH_WORLD_SPACE", 2,
            "_GlitchEnabled"),
        new KeywordBinding("_UseUIAlphaClip", "UNITY_UI_ALPHACLIP", 1),
    };

    // Visual choices are declared with shader_feature_local by their consuming passes.
    // Their order mirrors the fixed surface/post processing contract.
    private static readonly KeywordBinding[] EffectKeywords =
    {
        new KeywordBinding("_SecondaryLayerEnabled", "_SECONDARY_LAYER_ON", 1),
        new KeywordBinding("_MaskEnabled", "_MASK_ON", 1),
        new KeywordBinding("_UVDistortionEnabled", "_UV_DISTORTION_ON", 1),
        new KeywordBinding("_ColorAdjustEnabled", "_COLOR_ADJUST_ON", 1),
        new KeywordBinding("_RGBOverrideEnabled", "_RGB_OVERRIDE_ON", 1),
        new KeywordBinding("_UVFadeEnabled", "_UV_FADE_ON", 1),
        new KeywordBinding("_DissolveEnabled", "_DISSOLVE_ON", 1),
        new KeywordBinding("_DissolveSpace", "_DISSOLVE_OBJECT_SPACE", 1,
            "_DissolveEnabled"),
        new KeywordBinding("_DissolveMode", "_DISSOLVE_RADIAL", 1,
            "_DissolveEnabled"),
        new KeywordBinding("_DissolveMode", "_DISSOLVE_SWIPE", 2,
            "_DissolveEnabled"),
        new KeywordBinding("_DissolveEdgeColorMode", "_DISSOLVE_EDGE_GRADIENT", 1,
            "_DissolveEnabled"),
        new KeywordBinding("_LightSweepEnabled", "_LIGHT_SWEEP_ON", 1),
        new KeywordBinding("_LightSweepMode", "_LIGHT_SWEEP_SHARP", 1,
            "_LightSweepEnabled"),
        new KeywordBinding("_LightSweepBlendMode", "_LIGHT_SWEEP_MULTIPLY", 1,
            "_LightSweepEnabled"),
        new KeywordBinding("_DitherFadeEnabled", "_DITHER_FADE_ON", 1),
        new KeywordBinding("_PixelOutlineEnabled", "_PIXEL_OUTLINE_ON", 1),
        new KeywordBinding("_StencilOutlineEnabled", "_STENCIL_OUTLINE_ON", 1),
        new KeywordBinding("_HeightFadeEnabled", "_HEIGHT_FADE_ON", 1),
        new KeywordBinding("_EmissionEnabled", "_EMISSION", 1),
        new KeywordBinding("_RimEnabled", "_RIM_ON", 1),
        new KeywordBinding("_RimBlendMode", "_RIM_MULTIPLY", 1,
            "_RimEnabled"),
        new KeywordBinding("_RimMode", "_RIM_RADIAL_UV", 1,
            "_RimEnabled"),
        new KeywordBinding("_VertexOffsetEnabled", "_VERTEX_OFFSET_ON", 1),
        new KeywordBinding("_CustomDataEnabled", "_CUSTOM_DATA_ON", 1),
        new KeywordBinding("_GlassGlowEnabled", "_GLASS_GLOW_ON", 1),
        new KeywordBinding("_HologramEnabled", "_HOLOGRAM_ON", 1),
        new KeywordBinding("_GlitchEnabled", "_GLITCH_ON", 1),
        new KeywordBinding("_HologramSpace", "_HOLOGRAM_WORLD_SPACE", 1,
            "_HologramEnabled"),
        new KeywordBinding("_HologramSpace", "_HOLOGRAM_SCREEN_SPACE", 2,
            "_HologramEnabled"),
        new KeywordBinding("_FlipbookBlending", "_FLIPBOOKBLENDING_ON", 1),
        new KeywordBinding("_SoftParticlesEnabled", "_SOFTPARTICLES_ON", 1),
        new KeywordBinding("_CameraFadingEnabled", "_FADING_ON", 1),
    };

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        if (materialEditor.target is Material material && IsUberShader(material.shader))
            SeedKeywords(properties);

        base.OnGUI(materialEditor, properties);

        foreach (Object target in materialEditor.targets)
        {
            if (target is Material selectedMaterial)
                ValidateMaterial(selectedMaterial, false);
        }
    }

    public override void ValidateMaterial(Material material) => ValidateMaterial(material, true);

    private static void ValidateMaterial(Material material, bool synchronizeAscii)
    {
        if (material == null || !IsUberShader(material.shader))
            return;

        SynchronizeSurface(material);
        SynchronizeParticleRenderState(material);
        SynchronizeKeywords(material, StructuralKeywords);
        SynchronizeKeywords(material, EffectKeywords);
        SynchronizePostFilter(material);
        if (synchronizeAscii)
        {
            TMP_FontAsset ignored;
            SynchronizeAsciiFont(material, null, false, out ignored);
        }
        SynchronizePasses(material);
    }

    internal static int NormalizePostFilterMode(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return 0;

        return Mathf.Clamp(Mathf.RoundToInt(value), 0,
            PostFilterOptions.Length - 1);
    }

    internal static GUIContent[] CreatePostFilterLabels()
    {
        GUIContent[] labels = new GUIContent[PostFilterOptions.Length];
        for (int index = 0; index < labels.Length; ++index)
            labels[index] = new GUIContent(PostFilterOptions[index].Label);
        return labels;
    }

    internal static void SynchronizePostFilter(Material material)
    {
        if (material == null || material.shader == null ||
            material.shader.name != PostShader ||
            !material.HasProperty("_ScreenFilterMode"))
        {
            return;
        }

        int mode = NormalizePostFilterMode(material.GetFloat("_ScreenFilterMode"));
        material.SetFloat("_ScreenFilterMode", mode);
        for (int index = 1; index < PostFilterOptions.Length; ++index)
            SetKeyword(material, PostFilterOptions[index].Keyword, index == mode);
    }

    internal static void SetPostFilterVisibility(int mode)
    {
        int normalizedMode = Mathf.Clamp(mode, 0, PostFilterOptions.Length - 1);
        for (int index = 1; index < PostFilterOptions.Length; ++index)
        {
            string keyword = PostFilterOptions[index].Keyword;
            LWGUI.GUIData.keyWord[keyword] = index == normalizedMode;
        }
    }

    internal static string AssignAsciiFont(Object[] targets, TMP_FontAsset selectedFont)
    {
        Undo.RecordObjects(targets, "Set ASCII Font Asset");
        string status = "Fallback · " + AsciiRamp + " · No material selected";
        foreach (Object target in targets)
            if (target is Material material)
                status = SynchronizeAsciiFont(material, selectedFont, true,
                    out selectedFont);
        return status;
    }

    internal static string SynchronizeAsciiFont(Material material,
        TMP_FontAsset selectedFont, bool replaceAtlas, out TMP_FontAsset font,
        bool applyData = true)
    {
        font = selectedFont;
        if (material == null || !material.HasProperty(AsciiAtlasProperty))
            return "Fallback · ASCII font properties unavailable";
        Texture2D atlas = material.GetTexture(AsciiAtlasProperty) as Texture2D;
        string error = null;
        if (replaceAtlas)
        {
            Texture2D[] atlases = font == null ? null : font.atlasTextures;
            atlas = atlases != null && atlases.Length > 0 ? atlases[0] : null;
            if (applyData && material.GetTexture(AsciiAtlasProperty) != atlas)
                material.SetTexture(AsciiAtlasProperty, atlas);
        }
        else
            font = ResolveAsciiFont(atlas, out error);
        Texture2D derivedAtlas;
        Vector4[] glyphUVs, placements;
        if (font == null || !TryGetAsciiFontData(font, out derivedAtlas,
                out glyphUVs, out placements, out error))
        {
            if (applyData)
                SetAsciiFontData(material, null, null);
            return "Fallback · " + AsciiRamp + " · " +
                (error ?? "No Font Asset selected");
        }
        if (applyData)
        {
            if (material.GetTexture(AsciiAtlasProperty) != derivedAtlas)
                material.SetTexture(AsciiAtlasProperty, derivedAtlas);
            SetAsciiFontData(material, glyphUVs, placements);
        }
        return "Ready · " + AsciiRamp + " · " + font.name;
    }

    private static bool TryGetAsciiFontData(TMP_FontAsset font, out Texture2D atlas,
        out Vector4[] glyphUVs, out Vector4[] placements, out string error)
    {
        atlas = null;
        glyphUVs = new Vector4[AsciiRamp.Length];
        placements = new Vector4[AsciiRamp.Length];
        error = null;
        if (font.atlasPopulationMode != AtlasPopulationMode.Static)
            return RejectAsciiFont(out error, "Font Asset must use Static population");
        Texture2D[] atlases = font.atlasTextures;
        float emHeight = font.faceInfo.ascentLine - font.faceInfo.descentLine;
        if (atlases == null || atlases.Length == 0 || !IsFinite(emHeight) ||
            emHeight <= 0.0001f || font.characterLookupTable == null)
            return RejectAsciiFont(out error, "Font atlas or face metrics are invalid");
        for (int index = 0; index < AsciiRamp.Length; ++index)
        {
            if (!font.characterLookupTable.TryGetValue(AsciiRamp[index],
                    out TMP_Character character) || character == null ||
                character.glyph == null)
                return RejectAsciiFont(out error,
                    "Missing ramp character '" + AsciiRamp[index] + "'");
            var glyph = character.glyph;
            int atlasIndex = glyph.atlasIndex;
            if (atlasIndex < 0 || atlasIndex >= atlases.Length ||
                atlases[atlasIndex] == null || (atlas != null &&
                    atlas != atlases[atlasIndex]))
                return RejectAsciiFont(out error, "Ramp characters must share one atlas");
            atlas = atlases[atlasIndex];
            var rect = glyph.glyphRect;
            var metrics = glyph.metrics;
            float scale = glyph.scale;
            if (rect.width <= 0 || rect.height <= 0 || rect.x < 0 || rect.y < 0 ||
                rect.x + rect.width > atlas.width || rect.y + rect.height > atlas.height ||
                !IsFinite(scale) || scale <= 0.0f || metrics.width <= 0.0f ||
                metrics.height <= 0.0f || metrics.horizontalAdvance <= 0.0f)
                return RejectAsciiFont(out error, "Ramp glyph metrics are non-finite or empty");
            glyphUVs[index] = new Vector4((float)rect.x / atlas.width,
                (float)rect.y / atlas.height, (float)rect.width / atlas.width,
                (float)rect.height / atlas.height);
            placements[index] = new Vector4(
                0.5f - metrics.horizontalAdvance * scale / (2.0f * emHeight) +
                    metrics.horizontalBearingX * scale / emHeight,
                (metrics.horizontalBearingY * scale - metrics.height * scale -
                    font.faceInfo.descentLine) / emHeight,
                metrics.width * scale / emHeight,
                metrics.height * scale / emHeight);
            if (!IsFinite(glyphUVs[index]) || !IsFinite(placements[index]))
                return RejectAsciiFont(out error, "Derived glyph metadata is non-finite");
            for (int previous = 0; previous < index; ++previous)
                if (glyphUVs[previous] == glyphUVs[index])
                    return RejectAsciiFont(out error,
                        "Ramp glyph atlas regions must be distinct");
        }
        return true;
    }

    internal static TMP_FontAsset ResolveAsciiFont(Texture2D atlas, out string error)
    {
        if (atlas == null)
            return RejectAsciiFontAsset(out error, "No Font Asset selected");
        error = null;
        string samePath = AssetDatabase.GetAssetPath(atlas);
        if (string.IsNullOrEmpty(samePath))
            return RejectAsciiFontAsset(out error, "Atlas ownership could not be resolved");
        TMP_FontAsset match = null;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(samePath))
        {
            TMP_FontAsset candidate = asset as TMP_FontAsset;
            if (!OwnsAsciiAtlas(candidate, atlas))
                continue;
            if (match != null)
                return RejectAsciiFontAsset(out error, "Atlas ownership is ambiguous");
            match = candidate;
        }
        if (match != null)
            return match;

        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" });
        int limit = Mathf.Min(guids.Length, 256);
        for (int index = 0; index < limit; ++index)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            TMP_FontAsset candidate = path == samePath ? null :
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (!OwnsAsciiAtlas(candidate, atlas))
                continue;
            if (match != null)
                return RejectAsciiFontAsset(out error, "Atlas ownership is ambiguous");
            match = candidate;
        }

        error = match == null ? "Atlas ownership could not be resolved" : null;
        return match;
    }

    private static bool OwnsAsciiAtlas(TMP_FontAsset font, Texture2D atlas) =>
        font != null && font.atlasTextures != null &&
        System.Array.IndexOf(font.atlasTextures, atlas) >= 0;

    private static void SetAsciiFontData(Material material, Vector4[] glyphUVs,
        Vector4[] placements)
    {
        bool ready = glyphUVs != null && placements != null;
        float readyValue = ready ? 1.0f : 0.0f;
        if (material.GetFloat("_AsciiFontReady") != readyValue)
            material.SetFloat("_AsciiFontReady", readyValue);
        for (int index = 0; index < AsciiRamp.Length; ++index)
        {
            string name = "_AsciiGlyphUV" + index;
            Vector4 value = ready ? glyphUVs[index] : Vector4.zero;
            if (material.GetVector(name) != value)
                material.SetVector(name, value);
            name = "_AsciiGlyphPlacement" + index;
            value = ready ? placements[index] : Vector4.zero;
            if (material.GetVector(name) != value)
                material.SetVector(name, value);
        }
    }

    private static bool RejectAsciiFont(out string error, string message)
    {
        error = message;
        return false;
    }

    private static TMP_FontAsset RejectAsciiFontAsset(out string error,
        string message)
    {
        error = message;
        return null;
    }

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);

    private static bool IsFinite(Vector4 value) => IsFinite(value.x) &&
        IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);

    private static void SynchronizeSurface(Material material)
    {
        if (!material.HasProperty("_Surface"))
            return;

        if (material.HasProperty("_HologramEnabled") &&
            material.GetFloat("_HologramEnabled") > 0.5f)
            material.SetFloat("_Surface", 1.0f);

        int previousQueue = material.rawRenderQueue;
        // Surface shaders use URP's _QueueControl contract: 0 Auto, 1 Custom.
        // Missing _QueueControl remains Auto for tolerant imports.
        bool preserveCustomQueue = material.HasProperty("_QueueControl") &&
            Mathf.RoundToInt(material.GetFloat("_QueueControl")) == CustomQueueControl &&
            previousQueue >= 0;

        // Direct sprite/particle output owns RGB fading instead of lit specular.
        if ((material.shader.name == SpriteShader ||
             material.shader.name == ParticleShader) &&
            material.HasProperty("_BlendModePreserveSpecular"))
        {
            material.SetFloat("_BlendModePreserveSpecular", 0.0f);
        }

        UnityEditor.BaseShaderGUI.SetMaterialKeywords(material);

        // URP reserves _ALPHAPREMULTIPLY_ON for specular preservation; the
        // direct particle output instead owns the keyword as its blend mode.
        if (material.shader.name == ParticleShader &&
            material.HasProperty("_Blend"))
        {
            bool premultiply = material.GetFloat("_Surface") > 0.5f &&
                Mathf.RoundToInt(material.GetFloat("_Blend")) == 1;
            SetKeyword(material, "_ALPHAPREMULTIPLY_ON", premultiply);
        }

        if (preserveCustomQueue)
            material.renderQueue = previousQueue;
    }

    private static void SynchronizeKeywords(Material material, KeywordBinding[] bindings)
    {
        foreach (KeywordBinding binding in bindings)
        {
            if (!material.HasProperty(binding.PropertyName))
                continue;

            SetKeyword(material, binding.Keyword,
                binding.IsEnabled(material.GetFloat(binding.PropertyName), material));
        }
    }

    private static void SynchronizeParticleRenderState(Material material)
    {
        if (material.shader.name != ParticleShader)
            return;

        material.SetFloat("_ZTest", Mathf.Clamp(Mathf.RoundToInt(
            material.GetFloat("_ZTest")), 0, 8));
        material.SetFloat("_StencilComp", Mathf.Clamp(Mathf.RoundToInt(
            material.GetFloat("_StencilComp")), 0, 8));
        material.SetFloat("_StencilPass", Mathf.Clamp(Mathf.RoundToInt(
            material.GetFloat("_StencilPass")), 0, 7));
        material.SetFloat("_StencilRef", Mathf.Clamp(Mathf.RoundToInt(
            material.GetFloat("_StencilRef")), 0, 255));
        material.SetFloat("_StencilReadMask", Mathf.Clamp(Mathf.RoundToInt(
            material.GetFloat("_StencilReadMask")), 0, 255));
        material.SetFloat("_StencilWriteMask", Mathf.Clamp(Mathf.RoundToInt(
            material.GetFloat("_StencilWriteMask")), 0, 255));
        material.SetFloat("_ColorMask", Mathf.Clamp(Mathf.RoundToInt(
            material.GetFloat("_ColorMask")), 0, 15));
    }

    private static void SynchronizePasses(Material material)
    {
        if (material.HasProperty("_CastShadows"))
        {
            bool castShadows = material.GetFloat("_CastShadows") > 0.5f;
            material.SetShaderPassEnabled("ShadowCaster", castShadows);
        }

        if (material.HasProperty("_StencilOutlineEnabled"))
        {
            bool outline = material.GetFloat("_StencilOutlineEnabled") > 0.5f;
            material.SetShaderPassEnabled("StencilOutline", outline);
        }
    }

    private static void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (material.IsKeywordEnabled(keyword) == enabled)
            return;

        if (enabled)
            material.EnableKeyword(keyword);
        else
            material.DisableKeyword(keyword);
    }

    private static void SeedKeywords(MaterialProperty[] properties)
    {
        SeedKeywords(properties, StructuralKeywords);
        SeedKeywords(properties, EffectKeywords);

        MaterialProperty mode = FindProperty(properties, "_ScreenFilterMode");
        if (mode != null)
        {
            SetPostFilterVisibility(mode.hasMixedValue
                ? 0
                : NormalizePostFilterMode(mode.floatValue));
        }
    }

    private static void SeedKeywords(MaterialProperty[] properties, KeywordBinding[] bindings)
    {
        foreach (KeywordBinding binding in bindings)
        {
            MaterialProperty property = FindProperty(properties, binding.PropertyName);
            if (property == null)
                continue;

            bool parentEnabled = string.IsNullOrEmpty(binding.RequiredPropertyName) ||
                IsPropertyEnabled(properties, binding.RequiredPropertyName);
            LWGUI.GUIData.keyWord[binding.Keyword] =
                parentEnabled && binding.IsEnabled(property.floatValue, null);
        }
    }

    private static bool IsPropertyEnabled(MaterialProperty[] properties, string propertyName)
    {
        MaterialProperty property = FindProperty(properties, propertyName);
        return property != null && property.floatValue > 0.5f;
    }

    private static MaterialProperty FindProperty(MaterialProperty[] properties, string propertyName)
    {
        foreach (MaterialProperty property in properties)
        {
            if (property.name == propertyName)
                return property;
        }

        return null;
    }

    private static bool IsUberShader(Shader shader)
    {
        if (shader == null)
            return false;

        string shaderName = shader.name;
        return shaderName == ObjectShader || shaderName == SpriteShader ||
            shaderName == UIShader || shaderName == PostShader ||
            shaderName == ParticleShader;
    }

    private readonly struct KeywordBinding
    {
        public readonly string PropertyName;
        public readonly string Keyword;
        public readonly int EnabledValue;
        public readonly string RequiredPropertyName;

        public KeywordBinding(string propertyName, string keyword, int enabledValue,
            string requiredPropertyName = null)
        {
            PropertyName = propertyName;
            Keyword = keyword;
            EnabledValue = enabledValue;
            RequiredPropertyName = requiredPropertyName;
        }

        public bool IsEnabled(float propertyValue, Material material)
        {
            if (!string.IsNullOrEmpty(RequiredPropertyName) && material != null &&
                (!material.HasProperty(RequiredPropertyName) ||
                 material.GetFloat(RequiredPropertyName) <= 0.5f))
            {
                return false;
            }

            return Mathf.RoundToInt(propertyValue) == EnabledValue;
        }
    }
}
