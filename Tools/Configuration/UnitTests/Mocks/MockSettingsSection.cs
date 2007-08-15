//===============================================================================
// Microsoft patterns & practices
// Mobile Client Software Factory - July 2006
//===============================================================================
// Copyright  Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//===============================================================================

using System.Configuration;
using Microsoft.Practices.Mobile.Configuration;

namespace Microsoft.Practices.Mobile.Configuration.Tests.Mocks
{
	/// <summary>
	/// Definition of the configuration section for the block.
	/// </summary>
	public class MockSettingsSection : ConfigurationSection
	{
		/// <summary>
		/// The configuration section name for this section.
		/// </summary>
		public const string SectionName = "CompositeUI";

		/// <summary>
		/// List of startup services that will be initialized on the host.
		/// </summary>
		[ConfigurationProperty("services", IsRequired = true)]
		public MockServiceElementCollection Services
		{
			get { return (MockServiceElementCollection)this["services"]; }
		}
	}
}