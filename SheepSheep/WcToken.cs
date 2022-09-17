using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SheepSheep
{
    class WcToken
    {
        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();
        [DllImport("kernel32.dll")]
        private static extern int OpenProcess(int dwDesiredAccess, int bInheritHandle, int dwProcessId);
        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);
        [DllImport("Kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr handle, int address, byte[] data, int size, byte[] read);
        private struct MEMORY_BASIC_INFORMATION
        {
            public int BaseAddress;
            public int AllocationBase;
            public int AllocationProtect;
            public int RegionSize;
            public int State;
            public int Protect;
            public int lType;
        }
        const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        private static byte[] GetBlankBytes(int Length)
        {
            byte[] AOB = new byte[Length];
            for (int i = 0; i < Length; i++)
            {
                AOB[i] = 0;
            }
            return AOB;
        }
        public static int IndexOf(byte[] array, byte[] pattern, int startOffset = 0)
        {
            int success = 0;
            for (int i = startOffset; i < array.Length; i++)
            {
                if (array[i] == pattern[success])
                {
                    success++;
                }
                else
                {
                    success = 0;
                }
                if (pattern.Length == success)
                {
                    return i - pattern.Length + 1;
                }
            }
            return -1;
        }
        private static unsafe long IndexOf2(byte[] haystack, byte[] needle, long startOffset = 0)
        {
            fixed (byte* h = haystack) fixed (byte* n = needle)
            {
                for (byte* hNext = h + startOffset, hEnd = h + haystack.LongLength + 1 - needle.LongLength, nEnd = n + needle.LongLength; hNext < hEnd; hNext++)
                    for (byte* hInc = hNext, nInc = n; *nInc == *hInc; hInc++)
                        if (++nInc == nEnd)
                            return hNext - h;
                return -1;
            }
        }
        private static string getSubString(string text, string start, string end)
        {
            Regex regex = new Regex("(?<=(" + start + "))[.\\s\\S]*?(?=(" + end + "))", RegexOptions.Multiline | RegexOptions.Singleline);
            return regex.Match(text).Value;
        }
        private static string ReadMemoryString(IntPtr HWND, int IpAddr, int length)
        {
            byte[] readByte = GetBlankBytes(length);
            byte[] readBytes = GetBlankBytes(length);
            ReadProcessMemory(HWND, IpAddr, readByte, length, readBytes);
            return System.Text.Encoding.UTF8.GetString(readByte);
        }
        private static List<int> MemorySearch(IntPtr HWND, byte[] content)
        {
            int IpAddr = 0x000000;
            List<int> foundList = new List<int>();
            MEMORY_BASIC_INFORMATION mbi = new MEMORY_BASIC_INFORMATION();
            while (VirtualQueryEx(HWND, (IntPtr)IpAddr, out mbi, 28) != 0)
            {
                if (mbi.Protect != 16 && mbi.Protect != 1 && mbi.Protect != 512)
                {
                    byte[] readByte = GetBlankBytes(mbi.RegionSize);
                    byte[] readBytes = GetBlankBytes(mbi.RegionSize);
                    bool isRead = false;
                    int position = 0;
                    isRead = ReadProcessMemory(HWND, IpAddr, readByte, mbi.RegionSize, readBytes);
                    while (isRead)
                    {
                        position = (int)IndexOf(readByte, content, position);
                        if (position == -1)
                        {
                            break;
                        }
                        else
                        {
                            foundList.Add(IpAddr + position);
                        }
                        position = position + content.Length;
                    }
                }
                IpAddr = IpAddr + mbi.RegionSize;
            }
            return foundList;
        }

        public static string GetTokenFromWechat()
        {
            Process[] processes = Process.GetProcesses();
            byte[] searchStr = System.Text.Encoding.UTF8.GetBytes("\",\"token\":\"");
            foreach (Process process in processes)
            {
                if (process.ProcessName.Equals("WeChatAppEx"))
                {
                    IntPtr HWND = (IntPtr)OpenProcess(PROCESS_ALL_ACCESS, 0, process.Id);
                    List<int> foundList = MemorySearch(HWND, searchStr);
                    foreach (var item in foundList)
                    {
                        string ret = ReadMemoryString(HWND, item, 1024);
                        string token = getSubString(ret, "\",\"token\":\"", "\",\"");
                        if (!token.Equals("") && token.IndexOf("eyJ") != -1)
                        {
                            return token;
                        }
                    }
                }
            }
            return "false";
        }
    }
}
