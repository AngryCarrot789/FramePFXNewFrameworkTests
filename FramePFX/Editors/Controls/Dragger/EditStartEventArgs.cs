using System.Windows;

namespace FramePFX.Editors.Controls.NumDragger {
    public class EditStartEventArgs : RoutedEventArgs {
        public EditStartEventArgs() : base(NumberDragger.EditStartedEvent) {
        }
    }
}