using System;
using OsLibCore;

namespace RaiImage
{
    public class ItemTreePath
    {
        public string RootPath
        {
            get => rootPath;
            set
            {
                rootPath = NormalizeRootPath(value, ItemId, TopdirLength, SubdirLength);
                Apply();
            }
        }
        private string rootPath = string.Empty;

        public string ItemId
        {
            get => itemId;
            set
            {
                itemId = string.IsNullOrEmpty(value) ? string.Empty : value;
                rootPath = NormalizeRootPath(rootPath, itemId, TopdirLength, SubdirLength);
                Apply();
            }
        }
        private string itemId = string.Empty;

        public int TopdirLength { get; }
        public int SubdirLength { get; }

        public string Topdir { get; private set; } = string.Empty;
        public string Subdir { get; private set; } = string.Empty;
        public string Path { get; private set; } = string.Empty;

        private static string NormalizeRootPath(string rootCandidate, string itemId, int topdirLength, int subdirLength)
        {
            var normalized = string.IsNullOrEmpty(rootCandidate)
                ? string.Empty
                : new RaiFile(rootCandidate).Path;

            if (string.IsNullOrEmpty(normalized) || string.IsNullOrEmpty(itemId))
                return normalized;

            var top = itemId.Substring(0, Math.Min(itemId.Length, topdirLength));
            if (top.Length == 3 && top.Equals("con", StringComparison.OrdinalIgnoreCase))
                top = "C0N";
            var sub = itemId.Substring(0, Math.Min(itemId.Length, topdirLength + subdirLength));

            var marker = Os.DIRSEPERATOR + top + Os.DIRSEPERATOR + sub;
            var pos = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return pos >= 0 ? normalized.Remove(pos + 1) : normalized;
        }

        public void Apply()
        {
            Topdir = string.IsNullOrEmpty(ItemId)
                ? string.Empty
                : ItemId.Substring(0, Math.Min(ItemId.Length, TopdirLength));

            if (Topdir.Length == 3 && Topdir.Equals("con", StringComparison.OrdinalIgnoreCase))
                Topdir = "C0N";

            Subdir = string.IsNullOrEmpty(ItemId)
                ? string.Empty
                : ItemId.Substring(0, Math.Min(ItemId.Length, TopdirLength + SubdirLength));

            var p = RootPath;
            if (!string.IsNullOrEmpty(Topdir))
                p += Topdir + Os.DIRSEPERATOR;
            if (!string.IsNullOrEmpty(Subdir))
                p += Subdir + Os.DIRSEPERATOR;
            Path = p;
        }

        public ItemTreePath(string rootPath, string itemId, int topdirLength, int subdirLength)
        {
            TopdirLength = topdirLength;
            SubdirLength = subdirLength;
            this.itemId = string.IsNullOrEmpty(itemId) ? string.Empty : itemId;
            this.rootPath = NormalizeRootPath(rootPath, this.itemId, topdirLength, subdirLength);
            Apply();
        }
    }
}
