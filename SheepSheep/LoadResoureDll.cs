using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SheepSheep
{
    public static class LoadResoureDll
    {
        private static Dictionary<string, Assembly> LoadedDlls = new Dictionary<string, Assembly>();
        private static Dictionary<string, object> Assemblies = new Dictionary<string, object>();
        private static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                Assembly ass;
                var assName = new AssemblyName(args.Name).FullName;
                if (LoadedDlls.TryGetValue(assName, out ass) && ass != null)
                {
                    LoadedDlls[assName] = null;
                    return ass;
                }
                else
                {
                    return ass;
                    throw new DllNotFoundException(assName);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("error1:\n位置：AssemblyResolve()！\n描述：" + ex.Message);
                return null;
            }
        }

        public static void RegistDLL(string pattern = "*.dll")
        {
            System.IO.Directory.GetFiles("", "");
            var ass = new StackTrace(0).GetFrame(1).GetMethod().Module.Assembly;
            if (Assemblies.ContainsKey(ass.FullName))
            {
                return;
            }
            Assemblies.Add(ass.FullName, null);
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            var res = ass.GetManifestResourceNames();
            var regex = new Regex("^" + pattern.Replace(".", "\\.").Replace("*", ".*").Replace("_", ".") + "$", RegexOptions.IgnoreCase);
            foreach (var r in res)
            {
                if (regex.IsMatch(r))
                {
                    try
                    {
                        var s = ass.GetManifestResourceStream(r);
                        var bts = new byte[s.Length];
                        s.Read(bts, 0, (int)s.Length);
                        var da = Assembly.Load(bts);
                        if (LoadedDlls.ContainsKey(da.FullName))
                        {
                            continue;
                        }
                        LoadedDlls[da.FullName] = da;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("error2:加载dll失败\n位置：RegistDLL()！\n描述：" + ex.Message);
                    }
                }
            }
        }
    }
}
