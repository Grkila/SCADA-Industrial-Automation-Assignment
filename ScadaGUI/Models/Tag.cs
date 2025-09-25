using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;

namespace ScadaGUI.Models
{
    public enum TagType { DI, DO, AI, AO }

    public class Tag : INotifyPropertyChanged
    {
       
        private double? _currentValue;
        public double? CurrentValue
        {
            get => _currentValue;
            set { _currentValue = value; OnPropertyChanged(); }
        }

        public string Name { get; set; }
        public TagType Type { get; set; }
        public string IOAddress { get; set; }
        public string Description { get; set; }

        public Dictionary<string, object> Characteristics { get; private set; } = new Dictionary<string, object>();

        public double? LowLimit { get => GetValue<double?>("LowLimit"); set => SetValue("LowLimit", value); }
        public double? HighLimit { get => GetValue<double?>("HighLimit"); set => SetValue("HighLimit", value); }
        public string Units { get => GetValue<string>("Units"); set => SetValue("Units", value); }
        public int? ScanTime { get => GetValue<int?>("ScanTime"); set => SetValue("ScanTime", value); }
        public bool? IsScanning { get => GetValue<bool?>("IsScanning"); set { SetValue("IsScanning", value); OnPropertyChanged(); } } // Dodat OnPropertyChanged za CheckBox
        public double? InitialValue { get => GetValue<double?>("InitialValue"); set => SetValue("InitialValue", value); }

        private T GetValue<T>(string key)
        {
            if (Characteristics.TryGetValue(key, out object value) && value != null)
            {
                var targetType = typeof(T);
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    targetType = Nullable.GetUnderlyingType(targetType);
                }
                return (T)Convert.ChangeType(value, targetType);
            }
            return default(T);
        }

        private void SetValue(string key, object value)
        {
            Characteristics[key] = value;
            OnPropertyChanged(key);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}