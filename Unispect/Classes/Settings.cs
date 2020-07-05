using System;
using System.Collections.Generic;

namespace Unispect
{
    [Serializable]
    public class Settings
    {
        public bool AreEmpty => _internalSettings == null;
        // Just in case I decide I want to add more control over the settings.
        public Settings()
        {
            _internalSettings = new Dictionary<string, string>();
        }
        private Dictionary<string, string> _internalSettings;//= new Dictionary<string, string>();

        public void AddOrUpdate(string key, string value)
        {
            if (_internalSettings.ContainsKey(key))
                _internalSettings[key] = value;
            else
                _internalSettings.Add(key, value);
        }

        public string Get(string key) => _internalSettings[key];
        public void Remove(string key) => _internalSettings.Remove(key);
        public void Update(string key, string value) => _internalSettings[key] = value;
        public bool TryGetValue(string key, out string value) => _internalSettings.TryGetValue(key, out value);
        public string this[string key]
        {
            get => _internalSettings[key];
            set => _internalSettings[key] = value;
        }
    }
}