using System;
using System.Windows;

namespace FramePFX.Editors.Controls.Binders {
    public class UpdaterBinder<TModel> : BaseObjectBinder<TModel> where TModel : class {
        private readonly Action<IBinder<TModel>> updateControl;
        private readonly Action<IBinder<TModel>> updateModel;

        /// <summary>
        /// A dependency property, matched when <see cref="OnPropertyChanged"/> is called, which will
        /// call <see cref="BaseObjectBinder{TModel}.OnControlValueChanged"/> if the changed property matches
        /// </summary>
        public DependencyProperty Property { get; set; }

        public UpdaterBinder(Action<IBinder<TModel>> updateControl, Action<IBinder<TModel>> updateModel) {
            this.updateControl = updateControl;
            this.updateModel = updateModel;
        }

        public UpdaterBinder(DependencyProperty property, Action<IBinder<TModel>> updateControl, Action<IBinder<TModel>> updateModel) : this(updateControl, updateModel) {
            this.Property = property;
        }

        /// <summary>
        /// This method calls <see cref="BaseObjectBinder{TModel}.OnControlValueChanged"/> if
        /// our <see cref="Property"/> matches the changed property, we are not already updating
        /// the control and our property is non-null
        /// </summary>
        /// <param name="e">The property changed event args</param>
        public void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            if (!this.IsUpdatingControl && e.Property == this.Property && this.Property != null) {
                this.OnControlValueChanged();
            }
        }

        private void OnEvent() => this.OnModelValueChanged();

        protected override void UpdateModelCore() {
            this.updateModel?.Invoke(this);
        }

        protected override void UpdateControlCore() {
            this.updateControl?.Invoke(this);
        }
    }
}