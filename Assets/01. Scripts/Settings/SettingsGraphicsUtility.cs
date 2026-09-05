using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Border.Core;
using Border.Events;
using Border.UI;
using Border.Localization;

namespace Border.Settings
{
    public static class SettingsGraphicsUtility
    {
        public const int FullScreenModeIndex = 0;
        public const int WindowedModeIndex = 1;
        public const int BorderlessWindowModeIndex = 2;

        private const int MinResolution = 1280;
        private const int MinRefreshRate = 30;

        public static List<Resolution> GetResolutionsList()
        {
            return BuildResolutionList(Screen.resolutions, Screen.currentResolution);
        }

        public static List<Resolution> BuildResolutionList(IEnumerable<Resolution> available, Resolution desktop)
        {
            var candidates = new List<Resolution>(available);
            candidates.Add(desktop);
            // Window sizes remain selectable even when the monitor reports only its native mode.
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
            {
                if (size.x > desktop.width || size.y > desktop.height) continue;
                Resolution option = desktop;
                option.width = size.x;
                option.height = size.y;
                candidates.Add(option);
            }

            List<Resolution> resolutions = candidates
                .Where(resolution =>
                    resolution.width >= MinResolution &&
                    (resolution.refreshRateRatio.value == 0 || Mathf.RoundToInt((float)resolution.refreshRateRatio.value) >= MinRefreshRate))
                .GroupBy(resolution => (resolution.width, resolution.height))
                .Select(group => group.OrderByDescending(resolution => resolution.refreshRateRatio.value).First())
                .OrderByDescending(resolution => resolution.width)
                .ThenByDescending(resolution => resolution.height)
                .ToList();

            if (resolutions.Count == 0)
            {
                resolutions.Add(desktop);
            }

            return resolutions;
        }

        public static int GetValidatedResolutionIndex(IReadOnlyList<Resolution> resolutions, int resolutionIndex)
        {
            if (resolutions == null || resolutions.Count == 0)
            {
                return 0;
            }

            return Mathf.Clamp(resolutionIndex, 0, resolutions.Count - 1);
        }

        public static int GetValidatedWindowModeIndex(int modeIndex)
        {
            if (modeIndex < FullScreenModeIndex || modeIndex > BorderlessWindowModeIndex)
            {
                return BorderlessWindowModeIndex;
            }

            return modeIndex;
        }

        public static FullScreenMode GetFullScreenMode(int modeIndex)
        {
            switch (GetValidatedWindowModeIndex(modeIndex))
            {
                case FullScreenModeIndex:
                    return FullScreenMode.ExclusiveFullScreen;
                case BorderlessWindowModeIndex:
                    return FullScreenMode.FullScreenWindow;
                case WindowedModeIndex:
                default:
                    return FullScreenMode.Windowed;
            }
        }

        public static Resolution ApplyGraphicsSettings(int resolutionIndex, int windowModeIndex)
        {
            List<Resolution> resolutions = GetResolutionsList();
            int validatedResolutionIndex = GetValidatedResolutionIndex(resolutions, resolutionIndex);
            Resolution resolution = resolutions[validatedResolutionIndex];

            Screen.SetResolution(
                resolution.width,
                resolution.height,
                GetFullScreenMode(windowModeIndex));

            return resolution;
        }
    }

}
