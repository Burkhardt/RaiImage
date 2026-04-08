namespace RaiImage
{
	/// <summary>
	/// Convention for composing and parsing image file names.
	/// Parallel to PathConventionType which governs directory tree layout.
	/// </summary>
	public enum ImageNamingConvention
	{
		/// <summary>
		/// Full legacy naming with all components.
		/// ItemId_Color_ImageNumber_NameExt,TileTemplate-TileNumber.Ext
		/// Example: "308024_0DEAD0_01_zoom,4x4tile-17.tiff"
		/// Components are optional and omitted when empty/unset.
		/// </summary>
		Legacy,
		/// <summary>
		/// Simplified naming for rendered images.
		/// ItemId_TemplateName.Ext
		/// TemplateName refers to an ImageMagick rendering template.
		/// TemplateName maps to the NameExt component.
		/// Example: "1234567890_thumbnail.webp"
		/// When TemplateName is empty: "1234567890.webp"
		/// </summary>
		ItemTemplate,
		/// <summary>
		/// Glob-searchable structured naming with positional and metadata sections.
		/// Positional (underscore-separated): ItemId_ImageNumber_NameExt
		/// Metadata (comma-separated): ,Color,TileTemplate-TileNumber
		/// Full: ItemId_ImageNumber_NameExt,Color,TileTemplate-TileNumber.Ext
		/// Example: "471100_03_FullSizeHQ,Himmelblau,TilesZoomLevel3-37.webp"
		/// Components are omitted when empty/unset. The comma marks the boundary
		/// between the searchable positional prefix and optional metadata.
		/// Search pattern: "471100_03_FullSizeHQ,*" finds all color/tile variants.
		/// </summary>
		Structured
	}
	/// <summary>
	/// Contract for files that enforce a naming convention.
	/// Parallel to IPathConvention for directory tree layout.
	/// </summary>
	public interface INamingConvention
	{
		ImageNamingConvention NamingConvention { get; }
		/// <summary>
		/// Recompute structured name components from the current raw name.
		/// </summary>
		void ApplyNamingConvention();
	}
}
