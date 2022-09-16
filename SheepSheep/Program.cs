using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SheepSheep
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            releaseDLL();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static void releaseDLL() {
            byte[] dll = global::SheepSheep.Properties.Resources.GetTokenFromWechat;
            string path = Application.StartupPath + @"\GetTokenFromWechat.dll";
            using (FileStream fs = new FileStream(path, FileMode.Create)) {
                fs.Write(dll, 0, dll.Length);
            }
        }
    }
}
