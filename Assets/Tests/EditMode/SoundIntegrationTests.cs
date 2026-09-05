using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Border.Events;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Border.Audio.Tests
{
    public sealed class SoundIntegrationTests
    {
        private const string MixerPath = "Assets/04. Audios/SoundMixer.mixer";
        private const string DatabasePath = "Assets/02. ScriptableObjects/Audio/SoundDatabase.asset";
        private const string PrefabPath = "Assets/03. Prefabs/Systems/SoundManager.prefab";
        private const string ScenePath = "Assets/00. Scenes/SampleScene.unity";
        private const string SteamSettingsPath = "Assets/Plugins/SteamAudio/Resources/SteamAudioSettings.asset";
        private static readonly string[] VolumeParameters = { "MasterVolume", "BgmVolume", "SfxVolume" };

        [Test]
        public void Mixer_HasRequiredHierarchyParametersAndVolumeContract()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Assert.That(mixer, Is.Not.Null);

            AudioMixerGroup[] groups = mixer.FindMatchingGroups(string.Empty);
            CollectionAssert.AreEquivalent(new[] { "Master", "BGM", "SFX" }, groups.Select(group => group.name).ToArray());

            Type controllerType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController", true);
            Type groupType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerGroupController", true);
            Type exposedType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.ExposedAudioParameter", true);
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            object master = controllerType.GetProperty("masterGroup", Flags)?.GetValue(mixer);
            Array children = groupType.GetProperty("children", Flags)?.GetValue(master) as Array;
            Array exposed = controllerType.GetProperty("exposedParameters", Flags)?.GetValue(mixer) as Array;

            Assert.That(master, Is.Not.Null);
            Assert.That(((Object)master).name, Is.EqualTo("Master"));
            Assert.That(children, Is.Not.Null);
            CollectionAssert.AreEquivalent(new[] { "BGM", "SFX" }, children.Cast<Object>().Select(child => child.name).ToArray());
            Assert.That(exposed, Is.Not.Null);
            FieldInfo exposedName = exposedType.GetField("name", Flags);
            CollectionAssert.AreEquivalent(VolumeParameters, exposed.Cast<object>().Select(item => (string)exposedName.GetValue(item)).ToArray());

            foreach (string parameter in VolumeParameters)
            {
                Assert.That(mixer.GetFloat(parameter, out float value), Is.True, $"Missing exposed parameter {parameter}.");
                Assert.That(value, Is.EqualTo(0f).Within(.001f));
            }

            Assert.That(AudioMixerVolumeController.LinearToDecibels(0f), Is.EqualTo(-80f));
            Assert.That(AudioMixerVolumeController.LinearToDecibels(.5f), Is.EqualTo(-6.0206f).Within(.001f));
            Assert.That(AudioMixerVolumeController.LinearToDecibels(1f), Is.EqualTo(0f).Within(.001f));
        }

        [Test]
        public void SteamAudio_ProjectConfigurationIsEnabledForUnityHrtf()
        {
            Assert.That(AudioSettings.GetSpatializerPluginName(), Is.EqualTo("Steam Audio Spatializer"));

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string audioManagerYaml = File.ReadAllText(Path.Combine(projectRoot, "ProjectSettings", "AudioManager.asset"));
            StringAssert.Contains("m_AmbisonicDecoderPlugin: Steam Audio Ambisonic Decoder", audioManagerYaml);

            Object settings = AssetDatabase.LoadMainAssetAtPath(SteamSettingsPath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.GetType().FullName, Is.EqualTo("SteamAudio.SteamAudioSettings"));
            var serialized = new SerializedObject(settings);
            SerializedProperty audioEngine = serialized.FindProperty("audioEngine");
            SerializedProperty hrtfDisabled = serialized.FindProperty("hrtfDisabled");
            Assert.That(audioEngine, Is.Not.Null);
            Assert.That(audioEngine.enumNames[audioEngine.enumValueIndex], Is.EqualTo("Unity"));
            Assert.That(hrtfDisabled, Is.Not.Null);
            Assert.That(hrtfDisabled.boolValue, Is.False);

            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            Assert.That(defines.Split(';'), Does.Contain("STEAMAUDIO_ENABLED"));
        }

        [Test]
        public void SoundManagerPrefab_HasAllRequiredReferencesAndRouting()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                SoundManager manager = contents.GetComponent<SoundManager>();
                BgmPlayer bgmPlayer = contents.GetComponentInChildren<BgmPlayer>(true);
                SfxPool pool = contents.GetComponentInChildren<SfxPool>(true);
                AudioMixerVolumeController volume = contents.GetComponentInChildren<AudioMixerVolumeController>(true);
                SoundDatabaseSO database = AssetDatabase.LoadAssetAtPath<SoundDatabaseSO>(DatabasePath);
                AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

                Assert.That(manager, Is.Not.Null);
                Assert.That(bgmPlayer, Is.Not.Null);
                Assert.That(pool, Is.Not.Null);
                Assert.That(volume, Is.Not.Null);
                Assert.That(database, Is.Not.Null);
                Assert.That(database.BgmEntries, Is.Empty);
                foreach (string id in new[] { "SparkStart", "RocketLaunch", "RocketLoop", "EngineStop" })
                    Assert.That(database.TryGetSfx(id, out _), Is.True, $"Missing rocket SFX: {id}");
                Assert.That(database.TryGetSfx("RocketLoop", out SfxEntry loop), Is.True);
                Assert.That(loop.Loop, Is.True);
                Assert.That(loop.UseSpatialAudio, Is.True);
                Assert.That(database.TryGetSfx("EngineStop", out SfxEntry stop), Is.True);
                Assert.That(stop.Loop, Is.False);
                Assert.That(stop.UseSpatialAudio, Is.False);
                Assert.DoesNotThrow(database.RebuildLookup);
                Assert.That(database.TryGetBgm("missing", out _), Is.False);
                Assert.That(database.TryGetSfx("missing", out _), Is.False);

                var managerSerialized = new SerializedObject(manager);
                Assert.That(managerSerialized.FindProperty("database").objectReferenceValue, Is.SameAs(database));
                Assert.That(managerSerialized.FindProperty("bgmPlayer").objectReferenceValue, Is.SameAs(bgmPlayer));
                Assert.That(managerSerialized.FindProperty("sfxPool").objectReferenceValue, Is.SameAs(pool));
                Assert.That(managerSerialized.FindProperty("volumeController").objectReferenceValue, Is.SameAs(volume));

                AudioMixerGroup bgmGroup = mixer.FindMatchingGroups("BGM").Single();
                AudioMixerGroup sfxGroup = mixer.FindMatchingGroups("SFX").Single();
                AudioSource[] sources = bgmPlayer.GetComponentsInChildren<AudioSource>(true);
                Assert.That(sources, Has.Length.EqualTo(2));
                foreach (AudioSource source in sources)
                {
                    Assert.That(source.outputAudioMixerGroup, Is.SameAs(bgmGroup));
                    Assert.That(source.spatialBlend, Is.Zero);
                    Assert.That(source.spatialize, Is.False);
                    Assert.That(source.playOnAwake, Is.False);
                }

                var poolSerialized = new SerializedObject(pool);
                Assert.That(poolSerialized.FindProperty("outputMixerGroup").objectReferenceValue, Is.SameAs(sfxGroup));
                var voiceRoot = poolSerialized.FindProperty("voiceRoot").objectReferenceValue as Transform;
                Assert.That(voiceRoot, Is.Not.Null);
                Assert.That(voiceRoot.IsChildOf(pool.transform), Is.True);
                Assert.That(poolSerialized.FindProperty("prewarmCount").intValue, Is.EqualTo(16));
                Assert.That(poolSerialized.FindProperty("maxInactive").intValue, Is.EqualTo(32));

                var volumeSerialized = new SerializedObject(volume);
                Assert.That(volumeSerialized.FindProperty("mixer").objectReferenceValue, Is.SameAs(mixer));
                AssertAssetReference(volumeSerialized, "changeMasterVolumeEvent",
                    "Assets/06. Packages/Demo/Settings/Events/ChangeMasterVolumeEventChannel.asset");
                AssertAssetReference(volumeSerialized, "changeMusicVolumeEvent",
                    "Assets/06. Packages/Demo/Settings/Events/ChangeMusicVolumeEventChannel.asset");
                AssertAssetReference(volumeSerialized, "changeSfxVolumeEvent",
                    "Assets/06. Packages/Demo/Settings/Events/ChangeSFXVolumeEventChannel.asset");

                Assert.That(contents.GetComponentsInChildren<Component>(true)
                    .Any(component => component != null && component.GetType().FullName == "SteamAudio.SteamAudioSource"), Is.False);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        [Test]
        public void SampleScene_HasExactlyOnePrefabLinkedSoundManager()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedByTest = !scene.IsValid() || !scene.isLoaded;
            if (openedByTest)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                SoundManager[] managers = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<SoundManager>(true)).ToArray();
                Assert.That(managers, Has.Length.EqualTo(1));
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(managers[0].gameObject);
                Assert.That(source, Is.SameAs(prefab));
                Assert.That(scene.isDirty, Is.False);
            }
            finally
            {
                if (openedByTest && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void AssertAssetReference(SerializedObject serialized, string property, string expectedPath)
        {
            Object value = serialized.FindProperty(property).objectReferenceValue;
            Assert.That(value, Is.SameAs(AssetDatabase.LoadAssetAtPath<FloatEventChannelSO>(expectedPath)));
            Assert.That(AssetDatabase.GetAssetPath(value), Is.EqualTo(expectedPath));
        }
    }
}
