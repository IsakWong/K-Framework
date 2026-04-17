using Framework.Config;
using UnityEngine;

namespace Framework.Settings
{
    public class Settings
    {
        // Define settings properties here
        public int volume;
        public int brightness;
        public int QualityLevel;
        public bool fullscreen=true;
        public bool ShowMainUI=true;
    }


    public class SettingsManager : KSingleton<SettingsManager>, ISettingsService
    {
        Settings _settings;

        protected override void OnServiceRegistered()
        {
            ServiceLocator.Register<ISettingsService>(this);
        }
        public Settings CurrentSettings
        {
            get
            {
                if(_settings == null)
                    _settings = PersistentDataManager.Instance.LoadData<Settings>("Settings.json");
                return _settings;
            }
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