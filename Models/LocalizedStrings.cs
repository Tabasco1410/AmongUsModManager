using System.ComponentModel;
using AmongUsModManager.Models.Services;

namespace AmongUsModManager.Models
{
    public class LocalizedStrings : INotifyPropertyChanged
    {
        public static readonly LocalizedStrings Instance = new();

        public LocalizedStrings()
        {
            LocalizationService.LanguageChanged += Refresh;
        }

        public string this[string key] => LocalizationService.Get(key);

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Refresh()
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
