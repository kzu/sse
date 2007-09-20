using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleSharing
{
	/// <summary>
	/// Main repository interface for an SSE adapter.
	/// </summary>
	public interface IRepository
	{
		/// <summary>
		/// Whether the repository performs its own merge behavior, or 
		/// it must be provided by the <see cref="SyncEngine"/>.
		/// </summary>
		bool SupportsMerge { get; }

		/// <summary>
		/// Tries to retrieve an item with the given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">SSE identifier for the item</param>
		/// <returns>An <see cref="Item"/> if the item was found in the repository; <see langword="null"/> otherwise.</returns>
		Item Get(string id);

		/// <summary>
		/// Gets all the items in the repository.
		/// </summary>
		/// <returns></returns>
		IEnumerable<Item> GetAll();

		/// <summary>
		/// Gets all the items in the repository that were added, changed or removed after the given date.
		/// </summary>
		/// <param name="since">Optional date to retrieve items since.</param>
		IEnumerable<Item> GetAllSince(DateTime? since);

		/// <summary>
		/// Returns the items with conflicts.
		/// </summary>
		IEnumerable<Item> GetConflicts();

		/// <summary>
		/// Adds an item to the repository.
		/// </summary>
		/// <param name="item">The item to add.</param>
		void Add(Item item);

		/// <summary>
		/// Marks the item with the given id as deleted in the repository.
		/// </summary>
		/// <param name="id">The item SSE identifier.</param>
		void Delete(string id);

		/// <summary>
		/// Updates the item on the repository.
		/// </summary>
		/// <param name="item">The item to update.</param>
		void Update(Item item);

		/// <summary>
		/// Updates the item on the repository, optionally merging the conflicts history 
		/// depending on the value of <paramref name="resolveConflicts"/>.
		/// </summary>
		/// <param name="item">The item to update.</param>
		/// <param name="resolveConflicts"><see langword="true"/> to apply the 
		/// conflict resolution algorithm and update the item; <see langword="false"/> to 
		/// only save the item any potential conflicts it may have.</param>
		/// <returns>The updated item if conflicts were resolved.</returns>
		/// <remarks>
		/// See 3.4 on SSE spec.
		/// </remarks>
		void Update(Item item, bool resolveConflicts);

		/// <summary>
		/// Merges the list of items in the repository, and returns any conflicting 
		/// items that were saved.
		/// </summary>
		/// <param name="items">The items to merge.</param>
		/// <returns>List of conflicts resulting from the merge. Items with conflicts are 
		/// persisted to the repository, and the winner determines the item payload.</returns>
		IList<Item> Merge(IEnumerable<Item> items);

		/// <summary>
		/// Friendly name of the repository, useful for showing 
		/// in dialogs to identify a given repository.
		/// </summary>
		string FriendlyName { get; }
	}

}
