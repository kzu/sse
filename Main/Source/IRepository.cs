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
		/// Adds an item to the repository.
		/// </summary>
		/// <param name="item">The item to add.</param>
		void Add(Item item);

		/// <summary>
		/// Deletes the item with the given id, if present in the repository.
		/// </summary>
		/// <param name="id">The item SSE identifier.</param>
		void Delete(string id);

		/// <summary>
		/// Updates the item on the repository.
		/// </summary>
		/// <param name="item">The item to update.</param>
		void Update(Item item);

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
