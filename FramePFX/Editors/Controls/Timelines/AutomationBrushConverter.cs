using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FramePFX.Shortcuts.Usage;

namespace FramePFX.Editors.Controls.Timelines {
    public class AutomationBrushConverter : IMultiValueConverter {
        public Brush NoAutomationBrush { get; set; } = Brushes.Transparent;
        public Brush AutomationBrush { get; set; } = Brushes.Orange;
        public Brush OverrideBrush { get; set; } = Brushes.Gray;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            bool isAutomated = (bool) values[0];
            bool isOverride = (bool) values[1];
            if (isAutomated) {
                return isOverride ? this.OverrideBrush : this.AutomationBrush;
            }
            else {
                return this.NoAutomationBrush;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}