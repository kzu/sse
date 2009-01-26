#if PocketPC
using Microsoft.Practices.Mobile.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Collections.Generic;
using System.Xml;

namespace SimpleSharing.Tests
{
	[TestClass]
	public class TimestampFixture : TestFixtureBase
	{
		[TestMethod]
		public void ShouldRoundtripFormat()
		{
			DateTime now = DateTime.Now;
			// Timestamp resolution is up to seconds :S.
			now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);

			string value = Timestamp.ToString(now);
			DateTime dt = Timestamp.Parse(value);

			//Assert.AreEqual(DateTimeKind.Utc, dt.Kind);
			Assert.AreEqual(now, dt);
		}

		[TestMethod]
		public void ShouldPreserveIfAlreadyUtc()
		{
			DateTime now = DateTime.Now;
			// Timestamp resolution is up to seconds :S.
			now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

			string value = Timestamp.ToString(now);
			DateTime dt = Timestamp.Parse(value);

			Assert.AreEqual(now, dt.ToUniversalTime());
		}

		[TestMethod]
		public void ShouldParseAsLocal()
		{
			DateTime now = DateTime.Now;
			// Timestamp resolution is up to seconds :S.
			now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Local);

			DateTime norm = Timestamp.Normalize(now);

			Assert.AreEqual(now, norm);
		}

		[TestMethod]
		public void BewareLocalAndUniversalDoNotEqual()
		{
			// This gives you a local time.
			DateTime now = DateTime.Now;
			DateTime utc = now.ToUniversalTime();

			Assert.AreNotEqual(now, utc);
		}

		[TestMethod]
		public void ShouldNormalizeAdjustToLocalAndRemainEqual()
		{
			DateTime now = DateTime.Now;
			// Timestamp resolution is up to seconds :S.
			now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
			DateTime norm = Timestamp.Normalize(now);

			TimeSpan offset1 = TimeZone.CurrentTimeZone.GetUtcOffset(now);
			TimeSpan offset2 = TimeZone.CurrentTimeZone.GetUtcOffset(norm);

			Assert.AreEqual(offset1, offset2);
			Assert.AreEqual(now, norm);
		}
	}
}
