using System;
using System.Runtime.CompilerServices;
using FramePFX.Editors.ResourceManaging.Events;
using FramePFX.RBC;
using FramePFX.Utils;

namespace FramePFX.Editors.ResourceManaging {
    public abstract class ResourceItem : BaseResource, IDisposable {
        public const ulong EmptyId = ResourceManager.EmptyId;
        private bool isOnline;

        /// <summary>
        /// Gets or sets if this resource is online (usable) or offline (not usable by clips).
        /// <see cref="OnIsOnlineStateChanged"/> must be called after modifying this value
        /// </summary>
        public bool IsOnline {
            get => this.isOnline;
            set {
                this.isOnline = value;
                if (value) {
                    this.IsOfflineByUser = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets if the reason the resource is offline is because a user forced it offline
        /// </summary>
        public bool IsOfflineByUser { get; set; }

        /// <summary>
        /// This resource item's unique identifier
        /// </summary>
        public ulong UniqueId { get; private set; }

        public event ResourceAndManagerEventHandler OnlineStateChanged;

        protected ResourceItem() {
        }

        public bool IsRegistered() {
            return this.Manager != null && this.UniqueId != EmptyId && this.Manager.TryGetEntryItem(this.UniqueId, out ResourceItem resource);
        }

        /// <summary>
        /// A method that forces this resource offline, releasing any resources in the process. This may call <see cref="Dispose"/> or <see cref="DisposeCore"/>
        /// </summary>
        /// <param name="list">A list to add encountered exceptions to</param>
        /// <param name="user">
        /// Whether or not this resource was disabled by the user force disabling
        /// the item. <see cref="IsOfflineByUser"/> is set to this parameter
        /// </param>
        /// <returns></returns>
        public void Disable(ErrorList list, bool user) {
            if (!this.IsOnline)
                return;

            this.OnDisableCore(user);
            this.isOnline = false;
            this.IsOfflineByUser = user;
            this.OnIsOnlineStateChanged();
        }

        /// <summary>
        /// Called by <see cref="Disable"/> when this resource item should be disabled
        /// </summary>
        /// <param name="user">True if this was disabled forcefully by the user via the UI</param>
        protected virtual void OnDisableCore(bool user) {
        }

        public override void WriteToRBE(RBEDictionary data) {
            base.WriteToRBE(data);
            if (this.UniqueId != 0)
                data.SetULong(nameof(this.UniqueId), this.UniqueId);
            if (!this.IsOnline)
                data.SetBool(nameof(this.IsOnline), false);
        }

        public override void ReadFromRBE(RBEDictionary data) {
            base.ReadFromRBE(data);
            this.UniqueId = data.GetULong(nameof(this.UniqueId), EmptyId);
            if (data.TryGetBool(nameof(this.IsOnline), out bool b) && !b) {
                this.isOnline = false;
                this.IsOfflineByUser = true;
            }
        }

        public void OnDataModified<T>(ref T property, T newValue, [CallerMemberName] string propertyName = null) {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));
            property = newValue;
            this.OnDataModified(propertyName);
        }

        public virtual void OnIsOnlineStateChanged() {
            this.OnlineStateChanged?.Invoke(this.Manager, this);
        }

        /// <summary>
        /// Internal method for setting a resource item's unique ID
        /// </summary>
        internal static void SetUniqueId(ResourceItem item, ulong id) => item.UniqueId = id;

        public static void SetOnlineState(ResourceItem item, bool isOnline) {
            if (isOnline == item.isOnline) {
                return;
            }

            item.IsOnline = isOnline;
            item.OnIsOnlineStateChanged();
        }
    }
}