﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.1433
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FeedSync.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("FeedSync.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to When or By must have a non-empty value..
        /// </summary>
        internal static string Arg_EitherWhenOrByMustBeSpecified {
            get {
                return ResourceManager.GetString("Arg_EitherWhenOrByMustBeSpecified", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Updates must be a positive value..
        /// </summary>
        internal static string Arg_InvalidUpdates {
            get {
                return ResourceManager.GetString("Arg_InvalidUpdates", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Argument must be an RSS date time instance..
        /// </summary>
        internal static string Arg_MustBeRssDateTime {
            get {
                return ResourceManager.GetString("Arg_MustBeRssDateTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Value cannot be null or an empty string..
        /// </summary>
        internal static string Arg_NullOrEmpty {
            get {
                return ResourceManager.GetString("Arg_NullOrEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sequence must be greater than zero..
        /// </summary>
        internal static string Arg_SequenceMustBeGreaterThanZero {
            get {
                return ResourceManager.GetString("Arg_SequenceMustBeGreaterThanZero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Deleted on {0} by {1}..
        /// </summary>
        internal static string DeletedTitle {
            get {
                return ResourceManager.GetString("DeletedTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Input string &apos;{0}&apos; was not recognized as a valid RSS 2.0 date and time..
        /// </summary>
        internal static string Format_BadDateTime {
            get {
                return ResourceManager.GetString("Format_BadDateTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Initialization has already begun..
        /// </summary>
        internal static string InitializationBegunAlready {
            get {
                return ResourceManager.GetString("InitializationBegunAlready", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Initialization has not been initialized..
        /// </summary>
        internal static string InitializationNotBegun {
            get {
                return ResourceManager.GetString("InitializationNotBegun", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The object has already been initialized..
        /// </summary>
        internal static string InitializedAlready {
            get {
                return ResourceManager.GetString("InitializedAlready", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid xml root element or namespace. Expected namespace {0} and element {1}.
        /// </summary>
        internal static string InvalidNamespaceOrElement {
            get {
                return ResourceManager.GetString("InvalidNamespaceOrElement", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Either when or by must be provided..
        /// </summary>
        internal static string MustProvideWhenOrBy {
            get {
                return ResourceManager.GetString("MustProvideWhenOrBy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The object has not been initialized properly..
        /// </summary>
        internal static string NotInitialized {
            get {
                return ResourceManager.GetString("NotInitialized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sync information does not contain any history elements..
        /// </summary>
        internal static string SyncHistoryRequired {
            get {
                return ResourceManager.GetString("SyncHistoryRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Initialize method has not been called on the repository..
        /// </summary>
        internal static string UninitializedRepository {
            get {
                return ResourceManager.GetString("UninitializedRepository", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An SyncRepository has not been set for this repository.
        /// </summary>
        internal static string UnitializedSyncRepository {
            get {
                return ResourceManager.GetString("UnitializedSyncRepository", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An XmlRepository has not been set for this repository.
        /// </summary>
        internal static string UnitializedXmlRepository {
            get {
                return ResourceManager.GetString("UnitializedXmlRepository", resourceCulture);
            }
        }
    }
}
