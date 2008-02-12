using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.IO;

namespace FeedSync
{
	public class Sync : ICloneable<Sync>, IEquatable<Sync>
	{
		string id;
		bool deleted = false;
		int updates = 0;
		bool noConflicts = false;
		ComparableStack<History> updatesHistory = new ComparableStack<History>();
		List<FeedSyncSyndicationItem> conflicts = new List<FeedSyncSyndicationItem>();
		object tag;

		private Sync()
		{
		}

		private Sync(string id)
		{
			this.id = id;
		}

		public string Id
		{
			get { return id; }
		}

		public int Updates
		{
			get { return updates; }
		}

		public bool Deleted
		{
			get { return deleted; }
		}

		public bool NoConflicts
		{
			get { return noConflicts; }
		}

		public History LastUpdate
		{
			get { return updatesHistory.Count > 0 ? updatesHistory.Peek() : null; }
		}

		public IEnumerable<History> UpdatesHistory
		{
			get { return updatesHistory; }
		}

		public IList<FeedSyncSyndicationItem> Conflicts
		{
			get { return conflicts; }
		}

		// <summary>
		/// Tag value that univoquely identifies the item
		/// </summary>
		public object Tag
		{
			get { return tag; }
			set { tag = value; }
		}

		public bool IsSubsumedBy(Sync sync)
		{
			History Hx = this.LastUpdate;
			foreach (History Hy in sync.UpdatesHistory)
			{
				if (Hx.IsSubsumedBy(Hy))
				{
					return true;
				}
			}
			return false;
		}

		public static Sync Create(XmlReader reader, string version)
		{
			Guard.ArgumentNotNull(reader, "reader");
			Guard.ArgumentNotNullOrEmptyString(version, "version");

			Sync sync = new Sync();
			sync.ReadXml(reader, version);

			return sync;
		}

		public static Sync Create(string id, string by, DateTime? when)
		{
			return Update(new Sync(id), by, when, false);
		}

		public Sync Update(string by, DateTime? when)
		{
			Sync updated = this.Clone();

			return Update(updated, by, when, false);
		}

		public Sync Delete(string by, DateTime? when)
		{
			Sync updated = this.Clone();

			//Deleted attribute set to true because it is a deletion (3.2.4 from spec)
			return Update(updated, by, when, true);
		}

		private static Sync Update(Sync sync, string by, DateTime? when, bool deleteItem)
		{
			// 3.2.1
			sync.updates++;

			// 3.2.2 & 3.2.2.a.i
			History history = new History(by, when, sync.Updates);

			// 3.2.3
			sync.updatesHistory.Push(history);

			// 3.2.4
			sync.deleted = deleteItem;

			return sync;
		}

		public Sync SparsePurge()
		{
			List<History> purgedHistory = new List<History>();

			Dictionary<string, History> latest = new Dictionary<string, History>();

			foreach (History history in this.updatesHistory)
			{
				// By may be null or empty if not specified.
				// SSE allows either By or When to be specified.
				if (String.IsNullOrEmpty(history.By))
				{
					// Can't purge without a By
					purgedHistory.Add(history);
				}
				else
				{
					History last;
					if (latest.TryGetValue(history.By, out last))
					{
						if (history.Sequence > last.Sequence)
						{
							// Replace the item we added before.
							purgedHistory.Remove(last);
							latest.Add(history.By, history);
						}
					}
					else
					{
						latest.Add(history.By, history);
						purgedHistory.Add(history);
					}
				}
			}

			purgedHistory.Reverse();

			Sync purged = this.Clone();
			purged.updatesHistory.Clear();

			foreach (History history in purgedHistory)
			{
				purged.updatesHistory.Push(history);
			}

			return purged;
		}

		/// <summary>
		/// Adds the conflict history immediately after the topmost history.
		/// </summary>
		/// <remarks>Used for conflict resolution only.</remarks>
		internal void AddConflictHistory(History history)
		{
			History topmost = updatesHistory.Pop();
			updatesHistory.Push(history);
			updatesHistory.Push(topmost);
		}

		#region ICloneable<Sync> Members

		public Sync Clone()
		{
			Sync newSync = new Sync(this.id);
			newSync.updates = this.updates;
			newSync.deleted = this.deleted;

			List<History> newHistory = new List<History>(updatesHistory);
			newHistory.Reverse();
			foreach (History history in newHistory)
			{
				newSync.updatesHistory.Push(history.Clone());
			}

			foreach (FeedSyncSyndicationItem conflict in this.conflicts)
			{
				newSync.conflicts.Add((FeedSyncSyndicationItem)conflict.Clone());
			}

			return newSync;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion

		#region Equality

		public static bool Equals(Sync s1, Sync s2)
		{
			if (Object.ReferenceEquals(s1, s2)) return true;
			if (!Object.Equals(null, s1) && !Object.Equals(null, s2))
			{
				return s1.id == s2.id &&
					s1.updates == s2.updates &&
					s1.deleted == s2.deleted &&
					s1.noConflicts == s2.noConflicts &&
					s1.LastUpdate == s2.LastUpdate;
			}

			return false;
		}

		public bool Equals(Sync sync)
		{
			return Sync.Equals(this, sync);
		}

		public override bool Equals(object obj)
		{
			return Sync.Equals(this, obj as Sync);
		}

		public override int GetHashCode()
		{
			int hash = 0;
			hash = hash ^ this.id.GetHashCode();
			hash = hash ^ this.updates.GetHashCode();
			hash = hash ^ this.deleted.GetHashCode();
			hash = hash ^ this.noConflicts.GetHashCode();
			hash = hash ^ this.LastUpdate.GetHashCode();

			return hash;
		}

		/// <summary>Determines whether two specified instances of <see cref="Sync"></see> are equal.</summary>
		/// <returns><see langword="true"/> if s1 and s2 represent the same sync information; <see langword="false"/> otherwise.</returns>
		/// <param name="s2">A <see cref="Sync"></see>.</param>
		/// <param name="s1">A <see cref="Sync"></see>.</param>
		public static bool operator ==(Sync s1, Sync s2)
		{
			return Sync.Equals(s1, s2);
		}

		/// <summary>Determines whether two specified instances of <see cref="Sync"></see> are not equal.</summary>
		/// <returns><see langword="false"/> if s1 and s2 represent the same sync information; <see langword="true"/> otherwise.</returns>
		/// <param name="s2">A <see cref="Sync"></see>.</param>
		/// <param name="s1">A <see cref="Sync"></see>.</param>
		public static bool operator !=(Sync s1, Sync s2)
		{
			return !Sync.Equals(s1, s2);
		}

		#endregion

		internal void ReadXml(XmlReader reader, string version)
		{
			if (reader.NamespaceURI != Schema.Namespace || reader.LocalName != Schema.ElementNames.Sync)
				throw new InvalidOperationException(String.Format(Properties.Resources.InvalidNamespaceOrElement,
					Schema.Namespace, Schema.ElementNames.Sync));

			reader.MoveToAttribute(Schema.AttributeNames.Id);
			this.id = reader.Value;
			reader.MoveToAttribute(Schema.AttributeNames.Updates);
			this.updates = XmlConvert.ToInt32(reader.Value);

			if (reader.MoveToAttribute(Schema.AttributeNames.Deleted))
			{
				this.deleted = XmlConvert.ToBoolean(reader.Value);
			}
			if (reader.MoveToAttribute(Schema.AttributeNames.NoConflicts))
			{
				this.noConflicts = XmlConvert.ToBoolean(reader.Value);
			}

			reader.MoveToElement();

			List<History> historyUpdates = new List<History>();

			if (!reader.IsEmptyElement)
			{
				while (reader.Read() && !IsFeedSyncElement(reader, Schema.ElementNames.Sync, XmlNodeType.EndElement))
				{
					if (IsFeedSyncElement(reader, Schema.ElementNames.History, XmlNodeType.Element))
					{
						reader.MoveToAttribute(Schema.AttributeNames.Sequence);
						int sequence = XmlConvert.ToInt32(reader.Value);
						string by = null;
						DateTime? when = DateTime.Now;

						if (reader.MoveToAttribute(Schema.AttributeNames.When))
							when = DateTime.Parse(reader.Value);
						if (reader.MoveToAttribute(Schema.AttributeNames.By))
							by = reader.Value;

						historyUpdates.Add(new History(by, when, sequence));
					}
					else if (IsFeedSyncElement(reader, Schema.ElementNames.Conflicts, XmlNodeType.Element))
					{
						while (reader.Read() &&
							!IsFeedSyncElement(reader, Schema.ElementNames.Conflicts, XmlNodeType.EndElement))
						{
							XmlReader subTreeReader = reader.ReadSubtree();
							subTreeReader.MoveToContent();

							SyndicationItemFormatter itemFormatter = SyndicationFormatterFactory.CreateItemFormatter(version);
							itemFormatter.ReadFrom(subTreeReader);
							
							this.conflicts.Add((FeedSyncSyndicationItem)itemFormatter.Item);
						}
					}
				}
			}

			if (historyUpdates.Count != 0)
			{
				historyUpdates.Reverse();
				foreach (History history in historyUpdates)
				{
					this.updatesHistory.Push(history);
				}
			}
		}

		public void WriteXml(XmlWriter writer, string version)
		{
			// <sx:sync>
			writer.WriteStartElement(Schema.DefaultPrefix, Schema.ElementNames.Sync, Schema.Namespace);
			writer.WriteAttributeString(Schema.AttributeNames.Id, this.Id);
			writer.WriteAttributeString(Schema.AttributeNames.Updates, XmlConvert.ToString(this.Updates));
			writer.WriteAttributeString(Schema.AttributeNames.Deleted, XmlConvert.ToString(this.Deleted));
			writer.WriteAttributeString(Schema.AttributeNames.NoConflicts, XmlConvert.ToString(this.NoConflicts));

			foreach (History history in this.UpdatesHistory)
			{
				WriteHistory(writer, history);
			}

			if (this.conflicts.Count > 0)
			{
				// <sx:conflicts>
				writer.WriteStartElement(Schema.DefaultPrefix, Schema.ElementNames.Conflicts, Schema.Namespace);

				foreach (FeedSyncSyndicationItem conflict in this.conflicts)
				{
					SyndicationItemFormatter itemFormatter = SyndicationFormatterFactory.CreateItemFormatter(version, conflict);
					itemFormatter.WriteTo(writer);
				}

				// </sx:conflicts>
				writer.WriteEndElement();
			}

			// </sx:sync>
			writer.WriteEndElement();
		}

		private void WriteHistory(XmlWriter writer, History history)
		{
			// <sx:history>
			writer.WriteStartElement(Schema.DefaultPrefix, Schema.ElementNames.History, Schema.Namespace);
			writer.WriteAttributeString(Schema.AttributeNames.Sequence, XmlConvert.ToString(history.Sequence));
			if (history.When.HasValue)
				writer.WriteAttributeString(Schema.AttributeNames.When, Timestamp.ToString(history.When.Value));
			writer.WriteAttributeString(Schema.AttributeNames.By, history.By);
			// </sx:history>
			writer.WriteEndElement();
		}

		private static bool IsFeedSyncElement(XmlReader reader, string elementName, XmlNodeType nodeType)
		{
			return reader.LocalName == elementName &&
				reader.NamespaceURI == Schema.Namespace &&
				reader.NodeType == nodeType;
		}



	}
}
