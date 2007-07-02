using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Globalization;

namespace SimpleSharing
{
	public abstract class FeedReader
	{
		XmlReader reader;
		public event EventHandler ItemRead;

		public FeedReader(XmlReader reader)
		{
			this.reader = reader;
		}

		public void Read(out Feed feed, out IEnumerable<Item> items)
		{
			feed = ReadFeedImpl();
			items = ReadItemsImpl();
		}

		protected abstract XmlQualifiedName ItemName { get; }

		protected abstract Feed ReadFeed(XmlReader reader);

		protected abstract IXmlItem ReadItem(XmlReader reader);

		private Feed ReadFeedImpl()
		{
			SharingXmlReader sharingReader = new SharingXmlReader(reader);

			Feed feed = ReadFeed(sharingReader);
			feed.Sharing = sharingReader.Sharing;

			return feed;
		}

		private IEnumerable<Item> ReadItemsImpl()
		{
			XmlQualifiedName itemName = this.ItemName;

			do
			{
				if (IsItemElement(reader, itemName, XmlNodeType.Element))
				{
					yield return ReadItemImpl(reader);
				}
			}
			while (reader.Read());
		}

		private Item ReadItemImpl(XmlReader reader)
		{
			SyncXmlReader syncReader = new SyncXmlReader(reader, this);

			IXmlItem item = ReadItem(syncReader);
			Sync sync = syncReader.Sync;

			item.Id = sync.Id;

			return new Item(item, sync);
		}

		//private Item ReadItem(XmlReader reader)
		//{
		//   if (reader.ReadState == ReadState.Initial)
		//      reader.MoveToContent();
		//   if (!IsItemElement(reader))
		//      throw new InvalidOperationException();

		//   DateTime lastUpdated = DateTime.MinValue;
		//   string title = null;
		//   string description = null;

		//   MemoryStream mem = new MemoryStream();
		//   XmlWriter writer = XmlWriter.Create(mem);
		//   writer.WriteStartElement("payload");

		//   Sync sync = null;

		//   while (reader.Read())
		//   {
		//      if (reader.NodeType == XmlNodeType.Element)
		//      {
		//         if (reader.LocalName == "title")
		//            title = ReadElementValue(reader);
		//         else if (reader.LocalName == "pubDate")
		//            lastUpdated = RssDateTime.Parse(ReadElementValue(reader)).LocalTime;
		//         else if (reader.LocalName == "description")
		//            description = ReadElementValue(reader);
		//         else if (IsSseElement(reader, Schema.ElementNames.Sync))
		//            sync = ReadSync();
		//         // Anything that is unknown is payload.
		//         else
		//            writer.WriteNode(reader.ReadSubtree(), false);
		//      }
		//      else if (reader.NodeType == XmlNodeType.EndElement &&
		//         IsItemElement(reader))
		//         break;
		//   }

		//   writer.WriteEndElement();
		//   writer.Close();
		//   mem.Position = 0;

		//   Item item;

		//   // TODO: workaround for SSE Live feeds that 
		//   // don't have a description.
		//   if (String.IsNullOrEmpty(description))
		//      description = title;

		//   if (!sync.Deleted)
		//   {
		//      XmlDocument doc = new XmlDocument();
		//      doc.Load(mem);
		//      XmlElement payload = doc.DocumentElement;

		//      item = new Item(
		//         new XmlItem(title, description, lastUpdated, payload),
		//         sync);
		//      item.XmlItem.Id = sync.Id;
		//   }
		//   else
		//   {
		//      item = new Item(
		//         new NullXmlItem(sync.Id, sync.LastUpdate.When),
		//         sync);
		//   }

		//   if (ItemRead != null)
		//      ItemRead(this, EventArgs.Empty);

		//   return item;
		//}

		//internal Sync ReadSync()
		//{
		//   if (!IsSseElement(reader, Schema.ElementNames.Sync))
		//      throw new InvalidOperationException();

		//   Sync newSync = null;

		//   reader.MoveToAttribute(Schema.AttributeNames.Id);
		//   string id = reader.Value;
		//   reader.MoveToAttribute(Schema.AttributeNames.Updates);
		//   int updates = XmlConvert.ToInt32(reader.Value);

		//   newSync = new Sync(id, updates);

		//   if (reader.MoveToAttribute(Schema.AttributeNames.Deleted))
		//   {
		//      newSync.Deleted = XmlConvert.ToBoolean(reader.Value);
		//   }
		//   if (reader.MoveToAttribute(Schema.AttributeNames.NoConflicts))
		//   {
		//      newSync.NoConflicts = XmlConvert.ToBoolean(reader.Value);
		//   }

		//   reader.MoveToElement();

		//   List<History> historyUpdates = new List<History>();

		//   while (reader.Read() &&
		//      !(reader.NodeType == XmlNodeType.EndElement &&
		//         IsSseElement(reader, Schema.ElementNames.Sync)))
		//   {
		//      if (reader.NodeType == XmlNodeType.Element)
		//      {
		//         if (IsSseElement(reader, Schema.ElementNames.History))
		//         {
		//            reader.MoveToAttribute(Schema.AttributeNames.Sequence);
		//            int sequence = XmlConvert.ToInt32(reader.Value);
		//            string by = null;
		//            DateTime? when = null;

		//            if (reader.MoveToAttribute(Schema.AttributeNames.When))
		//               when = DateTime.Parse(reader.Value);
		//            if (reader.MoveToAttribute(Schema.AttributeNames.By))
		//               by = reader.Value;

		//            historyUpdates.Add(new History(by, when, sequence));
		//         }
		//         else if (IsSseElement(reader, Schema.ElementNames.Conflicts))
		//         {
		//            while (reader.Read() &&
		//               !(IsSseElement(reader, Schema.ElementNames.Conflicts)
		//               && reader.NodeType == XmlNodeType.EndElement))
		//            {
		//               if (IsItemElement(reader))
		//               {
		//                  newSync.Conflicts.Add(ReadItem(reader.ReadSubtree()));
		//               }
		//            }
		//         }
		//      }
		//   }

		//   if (historyUpdates.Count != 0)
		//   {
		//      historyUpdates.Reverse();
		//      foreach (History history in historyUpdates)
		//      {
		//         newSync.AddHistory(history);
		//      }
		//   }

		//   return newSync;
		//}

		private static bool IsSseElement(XmlReader reader, string elementName, XmlNodeType nodeType)
		{
			return reader.LocalName == elementName &&
				reader.NamespaceURI == Schema.Namespace &&
				reader.NodeType == nodeType;
		}

		private static bool IsItemElement(XmlReader reader, XmlQualifiedName itemName, XmlNodeType nodeType)
		{
			return reader.LocalName == itemName.Name &&
				reader.NamespaceURI == itemName.Namespace &&
				reader.NodeType == nodeType;
		}

		protected bool IsItemElement(XmlReader reader, XmlNodeType nodeType)
		{
			return IsItemElement(reader, this.ItemName, nodeType);
		}

		protected string ReadElementValue(XmlReader reader)
		{
			if (reader.NodeType == XmlNodeType.Element)
			{
				if (reader.IsEmptyElement)
					return null;
				else
					reader.Read();
			}

			return reader.Value;
		}

		class SharingXmlReader : XmlWrappingReader
		{
			Sharing sharing;

			public SharingXmlReader(XmlReader baseReader)
				: base(baseReader)
			{
			}

			public override bool Read()
			{
				bool read = base.Read();

				if (IsSseElement(this, Schema.ElementNames.Sharing, XmlNodeType.Element))
				{
					ReadSharing();
				}

				if (IsSseElement(this, Schema.ElementNames.Sharing, XmlNodeType.EndElement))
				{
					read = base.Read();
				}

				return read;
			}

			private Sharing ReadSharing()
			{
				sharing = new Sharing();
				if (base.MoveToAttribute(Schema.AttributeNames.Since))
					sharing.Since = base.Value;
				if (base.MoveToAttribute(Schema.AttributeNames.Until))
					sharing.Until = base.Value;
				if (base.MoveToAttribute(Schema.AttributeNames.Expires))
					sharing.Expires = DateTime.Parse(base.Value);

				if (base.NodeType == XmlNodeType.Attribute)
					base.MoveToElement();

				if (!base.IsEmptyElement)
				{
					while (base.Read() &&
						!IsSseElement(this, Schema.ElementNames.Sharing, XmlNodeType.EndElement))
					{
						if (IsSseElement(this, Schema.ElementNames.Related, XmlNodeType.Element))
						{
							sharing.Related.Add(new Related(
								base.GetAttribute(Schema.AttributeNames.Link),
								(RelatedType)Enum.Parse(
									typeof(RelatedType),
									CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
										base.GetAttribute(Schema.AttributeNames.Type)),
										false),
								base.GetAttribute(Schema.AttributeNames.Title)));
						}
					}
				}

				return sharing;
			}

			public Sharing Sharing
			{
				get { return sharing; }
			}
		}

		public class SyncXmlReader : XmlWrappingReader
		{
			FeedReader feedReader;
			Sync sync;

			public SyncXmlReader(XmlReader baseReader, FeedReader feedReader)
				: base(baseReader)
			{
				this.feedReader = feedReader;
			}

			public Sync Sync { get { return sync; } }

			public override bool Read()
			{
				bool read = base.Read();

				if (IsSseElement(this, Schema.ElementNames.Sync, XmlNodeType.Element))
				{
					sync = ReadSync();
				}

				if (IsSseElement(this, Schema.ElementNames.Sync, XmlNodeType.EndElement))
				{
					read = base.Read();
				}

				return read;
			}

			internal Sync ReadSync()
			{
				XmlQualifiedName itemName = feedReader.ItemName;
				Sync newSync = null;

				base.MoveToAttribute(Schema.AttributeNames.Id);
				string id = base.Value;
				base.MoveToAttribute(Schema.AttributeNames.Updates);
				int updates = XmlConvert.ToInt32(base.Value);

				newSync = new Sync(id, updates);

				if (base.MoveToAttribute(Schema.AttributeNames.Deleted))
				{
					newSync.Deleted = XmlConvert.ToBoolean(base.Value);
				}
				if (base.MoveToAttribute(Schema.AttributeNames.NoConflicts))
				{
					newSync.NoConflicts = XmlConvert.ToBoolean(base.Value);
				}

				base.MoveToElement();

				List<History> historyUpdates = new List<History>();

				while (base.Read() && !IsEndSync())
				{
					if (IsSseElement(this, Schema.ElementNames.History, XmlNodeType.Element))
					{
						base.MoveToAttribute(Schema.AttributeNames.Sequence);
						int sequence = XmlConvert.ToInt32(base.Value);
						string by = null;
						DateTime? when = null;

						if (base.MoveToAttribute(Schema.AttributeNames.When))
							when = DateTime.Parse(base.Value);
						if (base.MoveToAttribute(Schema.AttributeNames.By))
							by = base.Value;

						historyUpdates.Add(new History(by, when, sequence));
					}
					else if (IsSseElement(this, Schema.ElementNames.Conflicts, XmlNodeType.Element))
					{
						while (base.Read() &&
							!IsSseElement(this, Schema.ElementNames.Conflicts, XmlNodeType.EndElement))
						{
							if (IsItemElement(this, itemName, XmlNodeType.Element))
							{
								newSync.Conflicts.Add(feedReader.ReadItemImpl(base.BaseReader.ReadSubtree()));
							}
						}
					}
				}

				if (historyUpdates.Count != 0)
				{
					historyUpdates.Reverse();
					foreach (History history in historyUpdates)
					{
						newSync.AddHistory(history);
					}
				}

				return newSync;
			}

			private bool IsEndSync()
			{
				return IsSseElement(this, Schema.ElementNames.Sync, XmlNodeType.EndElement);
			}
		}
	}
}
