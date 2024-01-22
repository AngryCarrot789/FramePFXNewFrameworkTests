using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FramePFX.Editors.Controls.Binders;
using FramePFX.Editors.ResourceManaging;
using FramePFX.Editors.ResourceManaging.Events;
using FramePFX.Editors.ResourceManaging.Resources;
using SkiaSharp;

namespace FramePFX.Editors.Controls.Resources {
    public class ResourceListItemContentControl : Control {
        private static readonly Dictionary<Type, Func<ResourceListItemContentControl>> Constructors;

        public ResourceListItemControl ListItem { get; private set;}

        public BaseResource Resource => this.ListItem?.Model;

        protected ResourceListItemContentControl() {
        }

        public static void RegisterType<T>(Type resourceType, Func<T> func) where T : ResourceListItemContentControl {
            Constructors[resourceType] = func;
        }

        public static ResourceListItemContentControl NewInstance(Type resourceType) {
            if (resourceType == null) {
                throw new ArgumentNullException(nameof(resourceType));
            }

            // Just try to find a base control type. It should be found first try unless I forgot to register a new control type
            bool hasLogged = false;
            for (Type type = resourceType; type != null; type = type.BaseType) {
                if (Constructors.TryGetValue(resourceType, out var func)) {
                    return func();
                }

                if (!hasLogged) {
                    hasLogged = true;
                    Debugger.Break();
                    Debug.WriteLine("Could not find control for resource type on first try. Scanning base types");
                }
            }

            throw new Exception("No such content control for resource type: " + resourceType.Name);
        }

        static ResourceListItemContentControl() {
            Constructors = new Dictionary<Type, Func<ResourceListItemContentControl>>();
            RegisterType(typeof(ResourceFolder), () => new ResourceListItemContentControlFolder());
            RegisterType(typeof(ResourceColour), () => new ResourceListItemContentControlColour());
            RegisterType(typeof(ResourceImage), () => new ResourceListItemContentControlImage());
            RegisterType(typeof(ResourceTextStyle), () => new ResourceListItemContentControlTextStyle());
        }

        public void Connect(ResourceListItemControl item) {
            this.ListItem = item ?? throw new ArgumentNullException(nameof(item));
            this.OnConnected();
        }

        public void Disconnect() {
            this.OnDisconnected();
            this.ListItem = null;
        }

        protected virtual void OnConnected() {

        }

        protected virtual void OnDisconnected() {

        }
    }

    public class ResourceListItemContentControlFolder : ResourceListItemContentControl {
        public static readonly DependencyProperty ItemCountProperty = DependencyProperty.Register("ItemCount", typeof(int), typeof(ResourceListItemContentControlFolder), new PropertyMetadata(0));

        public int ItemCount {
            get => (int) this.GetValue(ItemCountProperty);
            private set => this.SetValue(ItemCountProperty, value);
        }

        public new ResourceFolder Resource => (ResourceFolder) base.Resource;

        public ResourceListItemContentControlFolder() {

        }

        protected override void OnConnected() {
            base.OnConnected();
            this.Resource.ResourceAdded += this.OnResourceAddedOrRemoved;
            this.Resource.ResourceRemoved += this.OnResourceAddedOrRemoved;
            this.Resource.ResourceMoved += this.OnResourceMoved;
            this.UpdateItemCount();
        }

        protected override void OnDisconnected() {
            base.OnDisconnected();
            this.Resource.ResourceAdded -= this.OnResourceAddedOrRemoved;
            this.Resource.ResourceRemoved -= this.OnResourceAddedOrRemoved;
            this.Resource.ResourceMoved -= this.OnResourceMoved;
        }

        private void OnResourceAddedOrRemoved(ResourceFolder parent, BaseResource item, int index) => this.UpdateItemCount();

        private void OnResourceMoved(ResourceFolder sender, ResourceMovedEventArgs e) => this.UpdateItemCount();

        private void UpdateItemCount() {
            this.ItemCount = this.Resource.Items.Count;
        }
    }

    public class ResourceListItemContentControlColour : ResourceListItemContentControl {
        public static readonly DependencyProperty ColourProperty = DependencyProperty.Register("Colour", typeof(Brush), typeof(ResourceListItemContentControlColour), new PropertyMetadata(null));

        public Brush Colour {
            get => (Brush) this.GetValue(ColourProperty);
            set => this.SetValue(ColourProperty, value);
        }

        public new ResourceColour Resource => (ResourceColour) base.Resource;

        private readonly AutoPropertyUpdateBinder<ResourceColour> colourBinder = new AutoPropertyUpdateBinder<ResourceColour>(ColourProperty, nameof(ResourceColour.ColourChanged), binder => {
            ResourceListItemContentControlColour element = (ResourceListItemContentControlColour) binder.Control;
            SKColor c = binder.Model.Colour;
            ((SolidColorBrush) element.Colour).Color = Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue);
        }, binder => {
            ResourceListItemContentControlColour element = (ResourceListItemContentControlColour) binder.Control;
            Color c = ((SolidColorBrush) element.Colour).Color;
            binder.Model.Colour = new SKColor(c.R, c.G, c.B, c.A);
        });

        public ResourceListItemContentControlColour() {
            this.Colour = new SolidColorBrush();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            this.colourBinder.OnPropertyChanged(e);
        }

        protected override void OnConnected() {
            base.OnConnected();
            this.colourBinder.Attach(this, this.Resource);
        }

        protected override void OnDisconnected() {
            base.OnDisconnected();
            this.colourBinder.Detatch();
        }
    }

    public class ResourceListItemContentControlImage : ResourceListItemContentControl {
        public new ResourceImage Resource => (ResourceImage) base.Resource;

        public ResourceListItemContentControlImage() {

        }
    }

    public class ResourceListItemContentControlTextStyle : ResourceListItemContentControl {
        public ResourceListItemContentControlTextStyle() {

        }
    }
}