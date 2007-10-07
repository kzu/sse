using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSharing
{
	/// <summary>
	/// Specifies when the <see cref="PreviewImportHandler"/> delegate should 
	/// be called while synchronizing.
	/// </summary>
	public enum PreviewBehavior
	{
		/// <summary>
		/// Call preview handler when merging items into the left repository.
		/// </summary>
		Left,
		/// <summary>
		/// Call preview handler when merging items into the right repository.
		/// </summary>
		Right,
		/// <summary>
		/// Call preview handler when merging items into both repositories.
		/// </summary>
		Both,
		/// <summary>
		/// Do not apply preview behavior to any repository.
		/// </summary>
		None,
	}
}
