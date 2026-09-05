using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Border.UI
{
    public static class UiEventSystemUtility
    {
        public static void Ensure()
        {
            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            }

            Type inputSystemUiModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemUiModuleType != null)
            {
                foreach (StandaloneInputModule oldModule in eventSystem.GetComponents<StandaloneInputModule>())
                {
                    oldModule.enabled = false;
                    DestroyUnityObject(oldModule);
                }

                if (eventSystem.GetComponent(inputSystemUiModuleType) == null)
                {
                    eventSystem.gameObject.AddComponent(inputSystemUiModuleType);
                }

                return;
            }

            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
