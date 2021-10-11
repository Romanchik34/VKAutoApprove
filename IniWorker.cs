using System;
using System.Runtime.InteropServices;
using System.Text;

namespace VKAutoApprove
{
    public static class IniWorker
    {
        [DllImport("kernel32")]
        public static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32")]
        public static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);
    }
}
