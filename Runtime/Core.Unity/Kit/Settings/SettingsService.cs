using Framework.Config;
using UnityEngine;

namespace Framework.Settings
{
    public class Settings
    {
        public int volume;
        public int musicVolume = 100;
        public int sfxVolume = 100;
        public int brightness;
        public int QualityLevel;
        public bool fullscreen = true;
        public bool ShowMainUI = true;
    }


    public class SettingsService : KSingleton<SettingsService>, ISettingsService
    {
        private const string SettingsFileName = "Settings.json";
        Settings _settings;

        protected override void OnServiceInit()
        {
            ServiceLocator.Register<ISettingsService>(this);
        }
        public Settings CurrentSettings
        {
            get
            {
                if(_settings == null)
                    _settings = PersistentDataService.Instance.LoadData<Settings>(SettingsFileName);
                return _settings;
            }
        }

        public void SaveSettings()
        {
            PersistentDataService.Instance.SaveData(SettingsFileName, CurrentSettings);
        }

        public void LoadSettings()
        {
            var qualitySettings = CurrentSettings;
            // Load quality settings
            QualitySettings.SetQualityLevel(qualitySettings.QualityLevel);
            Screen.fullScreen = qualitySettings.fullscreen;
        }
    }
}
