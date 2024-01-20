namespace FramePFX.Editors.Controls.NumDragger {
    public interface IValueFormatter {
        string ToString(double value, int? roundedPlaces);
    }
}