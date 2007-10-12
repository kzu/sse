using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSharing
{
	/// <summary>
	/// Specifies when the <see cref="FilterHandler"/> delegate should 
	/// be called while synchronizing.
	/// </summary>
	public enum FilterBehavior
	{
		/// <summary>
		/// Call filter when merging items into the left repository.
		/// </summary>
		Left,
		/// <summary>
		/// Call filter when merging items into the right repository.
		/// </summary>
		Right,
		/// <summary>
		/// Call filter when merging items into both repositories.
		/// </summary>
		Both,
		/// <summary>
		/// Do not apply filter behavior to any repository.
		/// </summary>
		None,
	}
}
