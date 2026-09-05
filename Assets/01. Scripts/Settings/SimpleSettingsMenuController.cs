using System.Collections.Generic;
using Border.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Border.Settings
{
    public sealed class SimpleSettingsMenuController : MonoBehaviour
    {
        private const string MasterVolumeKey = "Settings.MasterVolume";
        private const string BgmVolumeKey = "Settings.BgmVolume";
        private const string SfxVolumeKey = "Settings.SfxVolume";
        private const string ResolutionIndexKey = "Settings.ResolutionIndex";
        private const string ResolutionWidthKey = "Settings.ResolutionWidth";
        private const string ResolutionHeightKey = "Settings.ResolutionHeight";

        [SerializeField] private SettingsSO currentSettings;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button closeButton;

        private IReadOnlyList<Resolution> resolutions;
        private bool ownsSettings;
        private bool initialized;

        private void Awake() => Initialize();

        private bool Initialize()
        {
            if (initialized) return true;
            if (panelRoot == null || resolutionDropdown == null || masterSlider == null
                || bgmSlider == null || sfxSlider == null || closeButton == null)
            {
                Debug.LogError("SettingsMenu prefab has missing UI references.", this);
                return false;
            }

            if (currentSettings == null)
            {
                currentSettings = ScriptableObject.CreateInstance<SettingsSO>();
                ownsSettings = true;
            }
            resolutionDropdown.onValueChanged.AddListener(ApplyResolution);
            masterSlider.onValueChanged.AddListener(ApplyMasterVolume);
            bgmSlider.onValueChanged.AddListener(ApplyBgmVolume);
            sfxSlider.onValueChanged.AddListener(ApplySfxVolume);
            closeButton.onClick.AddListener(Close);
            panelRoot.SetActive(false);
            initialized = true;
            return true;
        }

        public void Open()
        {
            if (!Initialize()) return;
            resolutions = SettingsGraphicsUtility.GetResolutionsList();
            resolutionDropdown.ClearOptions();
            var labels = new List<string>(resolutions.Count);
            foreach (Resolution resolution in resolutions)
                labels.Add($"{resolution.width} x {resolution.height}");
            resolutionDropdown.AddOptions(labels);
            resolutionDropdown.SetValueWithoutNotify(GetSelectedResolutionIndex());
            resolutionDropdown.RefreshShownValue();
            masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            bgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(BgmVolumeKey, 1f));
            sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
            panelRoot.SetActive(true);
            closeButton.Select();
        }

        private int GetSelectedResolutionIndex()
        {
            int width = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
            int height = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
            for (int i = 0; i < resolutions.Count; i++)
                if (resolutions[i].width == width && resolutions[i].height == height) return i;
            return SettingsGraphicsUtility.GetValidatedResolutionIndex(resolutions, PlayerPrefs.GetInt(ResolutionIndexKey, 0));
        }

        private void ApplyResolution(int index)
        {
            if (resolutions == null || resolutions.Count == 0) return;
            int validated = SettingsGraphicsUtility.GetValidatedResolutionIndex(resolutions, index);
            int mode = Screen.fullScreenMode == FullScreenMode.Windowed ? SettingsGraphicsUtility.WindowedModeIndex
                : Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen ? SettingsGraphicsUtility.FullScreenModeIndex
                : SettingsGraphicsUtility.BorderlessWindowModeIndex;
            currentSettings.SaveGraphicsSettings(validated, mode);
            Resolution selected = resolutions[validated];
            PlayerPrefs.SetInt(ResolutionIndexKey, validated);
            PlayerPrefs.SetInt(ResolutionWidthKey, selected.width);
            PlayerPrefs.SetInt(ResolutionHeightKey, selected.height);
            Screen.SetResolution(selected.width, selected.height, Screen.fullScreenMode);
        }

        private void ApplyMasterVolume(float value) => ApplyVolume(MasterVolumeKey, value);
        private void ApplyBgmVolume(float value) => ApplyVolume(BgmVolumeKey, value);
        private void ApplySfxVolume(float value) => ApplyVolume(SfxVolumeKey, value);

        private void ApplyVolume(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            currentSettings.SaveAudioSettings(masterSlider.value, bgmSlider.value, sfxSlider.value);
            if (SoundManager.Instance == null) return;
            SoundManager.Instance.SetMasterVolume(masterSlider.value);
            SoundManager.Instance.SetBgmVolume(bgmSlider.value);
            SoundManager.Instance.SetSfxVolume(sfxSlider.value);
        }

        public void Close()
        {
            resolutionDropdown.Hide();
            panelRoot.SetActive(false);
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (initialized)
            {
                if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(ApplyResolution);
                if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(ApplyMasterVolume);
                if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(ApplyBgmVolume);
                if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(ApplySfxVolume);
                if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            }
            if (!ownsSettings || currentSettings == null) return;
            if (Application.isPlaying) Destroy(currentSettings);
            else DestroyImmediate(currentSettings);
        }
    }
}
