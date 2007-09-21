using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace SimpleSharing
{
	/// <summary>
	/// Base implementation of <see cref="ISupportInitialize"/>.
	/// </summary>
	public abstract class SupportInitialize : ISupportInitialize
	{
		bool beginCalled;
		bool initialized;

		/// <summary>
		/// Signals the object that initialization is starting.
		/// </summary>
		void ISupportInitialize.BeginInit()
		{
			ThrowIfInitialized();

			if (beginCalled)
			{
				throw new InvalidOperationException(Properties.Resources.InitializationBegunAlready);
			}

			OnBeginInit();
			beginCalled = true;
		}

		/// <summary>
		/// Signals the object that initialization is complete.
		/// </summary>
		void ISupportInitialize.EndInit()
		{
			ThrowIfInitialized();

			if (!beginCalled)
			{
				throw new InvalidOperationException(Properties.Resources.InitializationNotBegun);
			}

			OnEndInit();
			ValidateProperties();
			initialized = true;
		}

		/// <summary>
		/// Checks that the object has been properly initialized through 
		/// calls to <see cref="ISupportInitialize.BeginInit"/> and <see cref="ISupportInitialize.EndInit"/>. 
		/// </summary>
		/// <exception cref="InvalidOperationException">The object was not initialized 
		/// using the <see cref="ISupportInitialize"/> methods 
		/// <see cref="ISupportInitialize.BeginInit"/> and <see cref="ISupportInitialize.EndInit"/>.</exception>
		protected void EnsureInitialized()
		{
			if (!initialized)
			{
				throw new InvalidOperationException(Properties.Resources.NotInitialized);
			}
		}

		/// <summary>
		/// Signals the object that initialization is starting.
		/// </summary>
		protected virtual void OnBeginInit()
		{
		}

		/// <summary>
		/// Signals the object that initialization is complete.
		/// </summary>
		protected virtual void OnEndInit()
		{
		}

		/// <summary>
		/// Validates the object properties and throws if some are not valid.
		/// </summary>
		protected virtual void ValidateProperties()
		{
		}

		private void ThrowIfInitialized()
		{
			if (initialized)
			{
				throw new InvalidOperationException(Properties.Resources.InitializedAlready);
			}
		}
	}
}
