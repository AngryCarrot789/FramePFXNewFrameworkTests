namespace FramePFX.Editors.Controls.NumDragger {
    public class ValueStringFormatter : IValueFormatter {
        public string Format { get; set; }

        public string ToString(double value, int? roundedPlaces) {
            return string.Format(this.Format ?? "{0}", value.ToString());
        }
    }
}