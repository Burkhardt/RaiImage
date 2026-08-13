using System;
using RaiUtils;

namespace RaiImage
{
	/// <summary>Base exception for RaiImage filesystem and image-boundary failures.</summary>
	public class RaiImageIOException : RaiException
	{
		public RaiImageIOException(string message) : base(message)
		{
		}

		public RaiImageIOException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}

	/// <summary>Thrown when a source or related image cannot be found.</summary>
	public sealed class RaiImageNotFoundException : RaiImageIOException
	{
		public RaiImageNotFoundException(string message, string fileName = null) : base(message)
		{
			FileName = fileName;
		}

		public RaiImageNotFoundException(string message, string fileName, Exception innerException)
			: base(message, innerException)
		{
			FileName = fileName;
		}

		/// <summary>The image path that could not be resolved, when known.</summary>
		public string FileName { get; }
	}
}
