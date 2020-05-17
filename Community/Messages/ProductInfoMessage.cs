﻿namespace StockSharp.Community.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Product info message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ProductInfoMessage : Message, IOriginalTransactionIdMessage
	{
		/// <inheritdoc />
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Name.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		/// Description.
		/// </summary>
		[DataMember]
		public string Description { get; set; }

		/// <summary>
		/// Package id.
		/// </summary>
		[DataMember]
		public string PackageId { get; set; }

		/// <summary>
		/// Tags.
		/// </summary>
		[DataMember]
		public string Tags { get; set; }

		/// <summary>
		/// Author.
		/// </summary>
		[DataMember]
		public long Author { get; set; }

		/// <summary>
		/// Price.
		/// </summary>
		[DataMember]
		public Currency Price { get; set; }

		/// <summary>
		/// Download count.
		/// </summary>
		[DataMember]
		public int DownloadCount { get; set; }

		/// <summary>
		/// Rating.
		/// </summary>
		[DataMember]
		public decimal? Rating { get; set; }

		/// <summary>
		/// Internet address of help site.
		/// </summary>
		[DataMember]
		public string DocUrl { get; set; }

		/// <summary>
		/// Product required connectors.
		/// </summary>
		[DataMember]
		public bool IsRequiredConnectors { get; set; }

		/// <summary>
		/// Product required connectors.
		/// </summary>
		[DataMember]
		public ProductContentTypes ContentType { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ProductInfoMessage"/>.
		/// </summary>
		public ProductInfoMessage()
			: base(CommunityMessageTypes.ProductInfo)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="ProductInfoMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new ProductInfoMessage
			{
				OriginalTransactionId = OriginalTransactionId,
				Id = Id,
				Name = Name,
				Description = Description,
				PackageId = PackageId,
				Tags = Tags,
				Author = Author,
				Price = Price?.Clone(),
				DownloadCount = DownloadCount,
				Rating = Rating,
				DocUrl = DocUrl,
				IsRequiredConnectors = IsRequiredConnectors,
				ContentType = ContentType,
			};
			CopyTo(clone);
			return clone;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var str = base.ToString() + $",OrigTrId={OriginalTransactionId}";

			if (Id != 0)
				str += $",Id={Id}";

			if (!Name.IsEmpty())
				str += $",Name={Name}";

			if (!Description.IsEmpty())
				str += $",Descr={Description}";

			if (!PackageId.IsEmpty())
				str += $",PackageId={PackageId}";

			if (!Tags.IsEmpty())
				str += $",Tags={Tags}";

			if (Author != 0)
				str += $",Author={Author}";

			if (Price != null)
				str += $",Price={Price}";

			str += $",Downloads={DownloadCount}";

			if (Rating != null)
				str += $",Rating={Rating}";

			if (!DocUrl.IsEmpty())
				str += $",Doc={DocUrl}";

			str += $",Connectors={IsRequiredConnectors},Content={ContentType}";

			return str;
		}
	}
}