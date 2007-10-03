namespace SimpleSharing
{
    using System;
    using System.ComponentModel;
    
    
    public partial class CompoundRepository : ISupportInitialize, ISupportInitializeNotification, IChangeTracking, INotifyPropertyChanged
    {
        
        const string InitializationNotBegun = "Initialization has not been started.";
        
        const string NotInitialized = "The object has not been initialized properly.";
        
        private bool _beginCalled;
        
        private bool _isInitialized;
        
        private bool _isChanged;
        
        bool ISupportInitializeNotification.IsInitialized
        {
            get
            {
                return this._isInitialized;
            }
        }
        
        bool IChangeTracking.IsChanged
        {
            get
            {
                return this._isChanged;
            }
        }
        
        public event EventHandler Initialized;
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        public event EventHandler XmlRepositoryChanged;
        
        public event EventHandler SyncRepositoryChanged;
        
        public event EventHandler SupportsMergeChanged;
        
        public event EventHandler FriendlyNameChanged;
        
        private void Initialize()
        {
            ISupportInitialize init = ((ISupportInitialize)(this));
            init.BeginInit();
            init.EndInit();
        }
        
        void ISupportInitialize.BeginInit()
        {
            this._beginCalled = true;
        }
        
        void ISupportInitialize.EndInit()
        {
            if ((this._beginCalled == false))
            {
                throw new InvalidOperationException(CompoundRepository.InitializationNotBegun);
            }
            this.DoValidate();
            this._isInitialized = true;
            if ((this.Initialized != null))
            {
                this.Initialized(this, EventArgs.Empty);
            }
        }
        
        private bool IsChildInitialized(ISupportInitializeNotification child)
        {
            return ((child == null) 
                        || child.IsInitialized);
        }
        
        void IChangeTracking.AcceptChanges()
        {
            this.Validate();
        }
        
        private bool IsChildChanged(IChangeTracking child)
        {
            return ((child != null) 
                        && child.IsChanged);
        }
        
        private void RaisePropertyChanged(string property)
        {
            if ((this.PropertyChanged != null))
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
            this._isChanged = true;
        }
        
        /// <summary>
        ///Checks that the object has been properly initialized through 
        ///calls to <see cref='ISupportInitialize.BeginInit'/> and 
        ///<see cref='ISupportInitialize.EndInit'/> or the <see cref='Initialize'/> method.
        ///</summary>
        ///<exception cref='InvalidOperationException'>The object was not initialized 
        ///using the <see cref='ISupportInitialize'/> methods 
        ///<see cref='ISupportInitialize.BeginInit'/> and <see cref='ISupportInitialize.EndInit'/> or 
        ///by calling <see cref='Initialize'/> from the constructor.</exception>
        private void EnsureValid()
        {
            if ((this._isInitialized == false))
            {
                throw new InvalidOperationException(CompoundRepository.NotInitialized);
            }
            if (((IChangeTracking)(this)).IsChanged)
            {
                this.Validate();
            }
        }
        
        /// <summary>
        ///Validates the object properties and throws if some are not valid.
        ///</summary>
        public virtual void Validate()
        {
            try
            {
                this.DoValidate();
                this._isChanged = false;
            }
            catch (System.Exception )
            {
                throw;
            }
        }
        
        private void RaiseXmlRepositoryChanged()
        {
            if ((this.XmlRepositoryChanged != null))
            {
                this.XmlRepositoryChanged(this, EventArgs.Empty);
            }
        }
        
        private void RaiseSyncRepositoryChanged()
        {
            if ((this.SyncRepositoryChanged != null))
            {
                this.SyncRepositoryChanged(this, EventArgs.Empty);
            }
        }
        
        private void RaiseSupportsMergeChanged()
        {
            if ((this.SupportsMergeChanged != null))
            {
                this.SupportsMergeChanged(this, EventArgs.Empty);
            }
        }
        
        private void RaiseFriendlyNameChanged()
        {
            if ((this.FriendlyNameChanged != null))
            {
                this.FriendlyNameChanged(this, EventArgs.Empty);
            }
        }
    }
}
