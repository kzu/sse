using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace SimpleSharing
{
	/// <summary>
	/// Parses and renders <see cref="DateTime"/> instances in a format 
	/// compliant with RFC 3389 (see http://www.ietf.org/rfc/rfc3339.txt).
	/// </summary>
	public static class Timestamp
	{
		const string Rfc3389 = "yyyy'-'MM'-'dd'T'HH':'mm':'ssZ";

		public static DateTime Parse(string timestamp)
		{
			return DateTime.ParseExact(timestamp, Rfc3389, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal);
		}

		public static string ToString(DateTime timestamp)
		{
			return timestamp.ToUniversalTime().ToString(Rfc3389);
		}

		public static DateTime Normalize(DateTime dateTime)
		{
			return Parse(ToString(dateTime));
		}
	}
}
