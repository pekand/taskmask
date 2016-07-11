using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskMask
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var targetApplication = Process.GetCurrentProcess().ProcessName + ".exe";
            int ie_emulation = 10000;
            try
            {
                ie_emulation = 11;
            }
            catch { }
            SetIEVersioneKeyforWebBrowserControl(targetApplication, ie_emulation);

            Application.Run(new TaskMaskForm());
        }


        private static void SetIEVersioneKeyforWebBrowserControl(string appName, int ieval)
        {
            RegistryKey Regkey = null;
            try
            {
                Regkey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", 
                    true
                );

                if (Regkey == null)
                {
                    return;
                }

                string FindAppkey = Convert.ToString(Regkey.GetValue(appName));

                if (FindAppkey == "" + ieval)
                {
                    Regkey.Close();
                    return;
                }

                Regkey.SetValue(appName, unchecked((int)ieval), RegistryValueKind.DWord);

                FindAppkey = Convert.ToString(Regkey.GetValue(appName));

            }
            catch(Exception e)
            {
            }
            finally
            {
                if (Regkey != null)
                    Regkey.Close();
            }
        }
    }
}
