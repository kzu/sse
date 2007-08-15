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

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Practices.Mobile.Configuration.Tests.Mocks
{
	public class MockCustomSection : ConfigurationSection
	{
		[ConfigurationProperty("test")]
		public string Test
		{
			get { return (string) this["test"]; }
			set { this["test"] = value; }
		}

		[ConfigurationProperty("someValue")]
		public int SomeValue
		{
			get { return (int) this["someValue"]; }
			set { this["someValue"] = value; }
		}

		[ConfigurationProperty("Child")]
		public SimpleElement Child
		{
			get { return (SimpleElement)this["Child"]; }
		}

		[ConfigurationProperty("AnotherChild")]
		public SimpleElement AnotherChild
		{
			get { return (SimpleElement)this["AnotherChild"]; }
		}
	}
}
