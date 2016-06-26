using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Permissions;
using System.Drawing.Imaging;

namespace TaskMask
{

    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class TaskMaskForm : Form
    {

        internal static Timer TimerWatcher = new Timer();

        List<Item> items = new List<Item>();

        public void activateItem(String itemId)
        {
            int id = Int32.Parse(itemId);

            foreach (Item item in items)
            {
                if (item.id == id)
                {
                    if (!item.visibility)
                    {
                        Manager.showApp(item.handle);
                        item.visibility = true;
                    }

                    Manager.setForegroundWindow(item.handle);
                }
            }
        }

        public void toggleItem(String itemId)
        {
            int id = Int32.Parse(itemId);
            foreach (Item item in items)
            {
                if (item.id == id)
                {
                    if (item.visibility)
                    {
                        Manager.hideApp(item.handle);
                        item.visibility = false;
                    }
                    else
                    {
                        Manager.showApp(item.handle);
                        item.visibility = true;
                    }
                }
            }
        }

        public TaskMaskForm()
        {
            InitializeComponent();

            //DoStartWatcher();


            // init webbrowser
            wb.AllowWebBrowserDrop = false;
            wb.IsWebBrowserContextMenuEnabled = false;
            wb.WebBrowserShortcutsEnabled = false;
            wb.ObjectForScripting = this;
            //wb.ScriptErrorsSuppressed = true;

            string html = Properties.Resources.main_html;
            wb.DocumentText = html;
        }

        // clean hiden items after close
        private void TaskMaskForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (Item item in items)
            {
                if (!item.visibility)
                {
                    Manager.showApp(item.handle);
                    item.visibility = true;
                }
            }
        }

        // load jquery to webbrowser
        private void loadJquery() {
            
            wb.Document.InvokeScript("setDebugMode", new object[] { false });

            string jquery_ui_css = Properties.Resources.jquery_ui_css;
            wb.Document.InvokeScript("loadStyle", new object[] { jquery_ui_css });

            string jquery = Properties.Resources.jquery_min;
            wb.Document.InvokeScript("eval", new object[] { jquery });

            string jquery_ui = Properties.Resources.jquery_ui;
            wb.Document.InvokeScript("eval", new object[] { jquery_ui });
        }

        // After browser load
        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (this.wb.ReadyState != WebBrowserReadyState.Complete)
                return;
            else {
                loadJquery();
                updateList();
                wb.Document.InvokeScript("doIt");
            }
        }

        int id = 0;

        private void updateList()
        {
            IDictionary<IntPtr, string>  windows = Manager.GetOpenWindows();

            
            for (int i = items.Count()-1; i>=0; i--)
            {
                if (!Manager.isLive(items[i].handle))
                {

                    Object[] objArray = new Object[3];
                    objArray[0] = (Object)items[i].id;
                    wb.Document.InvokeScript("removeFromList", objArray);
                    items.RemoveAt(i);
                }
            }

            foreach (KeyValuePair<IntPtr, string> window in windows)
            {
                
                // check if is item new
                bool exists = false;
                foreach (Item itemIn in items)
                {
                    if (window.Key == itemIn.handle) {
                        exists = true;
                        break;
                    }
                }

                // add new
                if (!exists)
                {
                    Item item = new Item();
                    item.id = ++id;
                    item.handle = window.Key;
                    item.title = window.Value;
                    item.visibility = true;
                    item.image = Manager.GetSmallWindowIcon(window.Key);
                    items.Add(item);

                    // add item to list of applications
                    Object[] objArray = new Object[3];
                    objArray[0] = (Object)item.id;
                    objArray[1] = (Object)item.title;
                    objArray[2] = (Object)item.image;
                    wb.Document.InvokeScript("addToList", objArray);
                }
            }

            wb.Document.InvokeScript("makeSortable");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            updateList();
        }
    }
}
