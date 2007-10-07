using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSharing
{
	public class DataEventArgs<T> : EventArgs
	{
		private T value;

		public DataEventArgs(T value)
		{
			this.value = value;
		}

		public T Value
		{
			get { return value; }
		}
	}
}
