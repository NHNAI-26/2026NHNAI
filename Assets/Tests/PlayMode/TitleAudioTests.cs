#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using Border.Audio;
using Border.Title;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Border.Audio.Tests
{
    public sealed class TitleAudioTests
    {
        [UnityTest]
        public IEnumerator Title_PlaysFourLoops_OneClick_AndStopsOnlyItsLoops()
        {
            Assert.IsNull(SoundManager.Instance);
            var camera = new GameObject("Title audio test camera", typeof(Camera));
            camera.tag = "MainCamera";
            var rocket = new GameObject("TitleRocket");
            GameObject screen = null;
            SoundManager manager = null;
            try
            {
                screen = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/03. Prefabs/UI/TitleScreen.prefab"));
                manager = SoundManager.Instance;
                Assert.IsNotNull(manager);
                Assert.IsNotNull(camera.GetComponent<AudioListener>());
                var menu = screen.GetComponent<TitleMenu>();
                var database = AssetDatabase.LoadAssetAtPath<SoundDatabaseSO>(
                    "Assets/02. ScriptableObjects/Audio/SoundDatabase.asset");
                Assert.IsTrue(database.TryGetSfx("RocketLoop", out var loopEntry));
                Assert.IsTrue(database.TryGetSfx("click", out var clickEntry));
                yield return null; // Let the global button sound hook scan the title buttons.

                var loops = Voices(manager, loopEntry.Clip);
                Assert.AreEqual(4, loops.Length);
                Assert.IsTrue(loops.All(source => source.loop && source.spatialBlend == 1f));
                Assert.IsTrue(loops.All(source => source.transform.position == rocket.transform.position));

                var settings = (Button)typeof(TitleMenu)
                    .GetField("settingsButton", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(menu);
                settings.onClick.Invoke();
                Assert.AreEqual(1, Voices(manager, clickEntry.Clip).Length,
                    "The title callback and global hook must not both play the click.");

                menu.enabled = false;
                Assert.AreEqual(0, Voices(manager, loopEntry.Clip).Length);
                Assert.AreEqual(1, Voices(manager, clickEntry.Clip).Length,
                    "Leaving the title must preserve the click tail.");
                menu.enabled = true;
                Assert.AreEqual(4, Voices(manager, loopEntry.Clip).Length);
                Object.Destroy(screen);
                yield return null;
                Assert.AreEqual(0, Voices(manager, loopEntry.Clip).Length);
            }
            finally
            {
                if (screen != null) Object.Destroy(screen);
                if (manager != null) Object.Destroy(manager.gameObject);
                Object.Destroy(rocket);
                Object.Destroy(camera);
            }
            yield return null;
        }

        private static AudioSource[] Voices(SoundManager manager, AudioClip clip) =>
            manager.GetComponentsInChildren<AudioSource>().Where(source => source.clip == clip).ToArray();
    }
}
#endif
