using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace MyZipper.src
{
    [SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);
    }

    public sealed class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return SafeNativeMethods.StrCmpLogicalW(a, b);
        }
    }

    public sealed class PxvTitleComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            string titlea;
            string titleb;

                titlea = Pxv.GetPxvArtworkTitleFromPath(a) + a;
                titleb = Pxv.GetPxvArtworkTitleFromPath(b) + b;

            return SafeNativeMethods.StrCmpLogicalW(titlea, titleb);
        }
    }

    public sealed class PxvTitleComparerB : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            string titlea;
            string titleb;

                string regex_str = @"(\d\d-\d\d-\d\d)\s+\d+(.*)\(\d+\)";
                titlea = Pxv.GetPxvArtworkTitleFromPath(a, regex_str) + a;
                titleb = Pxv.GetPxvArtworkTitleFromPath(b, regex_str) + b;

            return SafeNativeMethods.StrCmpLogicalW(titlea, titleb);
        }
    }

    public sealed class NaturalFileInfoNameComparer : IComparer<FileInfo>
    {
        public int Compare(FileInfo a, FileInfo b)
        {
            return SafeNativeMethods.StrCmpLogicalW(a.Name, b.Name);
        }
    }
}
