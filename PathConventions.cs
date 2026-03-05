namespace RaiImage
{
    public enum PathConventionType
    {
        CanonicalByName,
        ItemIdTree3x3,
        ItemIdTree8x2
    }

    public interface IPathConventionFile
    {
        PathConventionType ConventionName { get; }
        void ApplyPathConvention();
    }
}
