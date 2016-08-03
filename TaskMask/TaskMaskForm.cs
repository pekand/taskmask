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
using System.Xml.Serialization;
using Microsoft.Win32;

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

        KeyboardHook hook = new KeyboardHook();


        public TaskMaskForm()
        {
            InitializeComponent();

            // init webbrowser
            wb.AllowWebBrowserDrop = false;
            wb.IsWebBrowserContextMenuEnabled = false;
            wb.WebBrowserShortcutsEnabled = false;
            wb.ObjectForScripting = this;
            wb.ScriptErrorsSuppressed = true;

            string html = Properties.Resources.main_html;
            wb.DocumentText = html;

            //Manager.Log(html);

            // register the event that is fired after the key press.
            hook.KeyPressed +=
                new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);
            // register the control + alt + F12 combination as hot key.
            hook.RegisterHotKey(ModifierKeysOption.Control | ModifierKeysOption.Alt,
                Keys.T);
        }

        void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            // show the keys pressed in a label.
            this.WindowState = FormWindowState.Normal;
        }

        // load jquery to webbrowser
        private void loadJquery() {

            dynamic state = wb.Document.InvokeScript("setDebugMode", new object[] { false });

            string jquery_ui_css = Properties.Resources.jquery_ui_css;
            state = wb.Document.InvokeScript("loadStyle", new object[] { jquery_ui_css });

            string jquery = Properties.Resources.jquery_min;
            state = wb.Document.InvokeScript("loadLibraryInline", new object[] { jquery });

            string jquery_ui = Properties.Resources.jquery_ui;
            state = wb.Document.InvokeScript("loadLibraryInline", new object[] { jquery_ui });
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
                    // remove item if not long exist
                    Object[] objArray = new Object[1];
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

                        if (itemIn.title != window.Value)
                        {
                            //update title
                            itemIn.title = window.Value;
                            Object[] objArray = new Object[2];
                            objArray[0] = (Object)itemIn.id;
                            objArray[1] = (Object)window.Value;
                            wb.Document.InvokeScript("updateTitle", objArray);
                        }

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

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void topMostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.TopMost = !this.TopMost;
            this.topMostToolStripMenuItem.Checked = this.TopMost;
        }

        private void TaskMaskForm_Load(object sender, EventArgs e)
        {
            this.Left = Properties.Settings.Default.PosLeft;
            this.Top = Properties.Settings.Default.PosTop;
            this.Height = Properties.Settings.Default.Height;
            this.Width = Properties.Settings.Default.Width;
            this.TopMost = Properties.Settings.Default.TopMost;

            if (!this.IsOnScreen(this))
            {
                this.Left = (Screen.PrimaryScreen.Bounds.Width - this.Width) / 2;
                this.Top = (Screen.PrimaryScreen.Bounds.Height - this.Height) / 2;
                this.WindowState = FormWindowState.Normal;
            }

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

            Properties.Settings.Default.PosLeft = this.Left;
            Properties.Settings.Default.PosTop = this.Top;
            Properties.Settings.Default.Height = this.Height;
            Properties.Settings.Default.Width = this.Width;
            Properties.Settings.Default.TopMost = this.TopMost;

            Properties.Settings.Default.Save();
        }

        // check if form is on screen
        public bool IsOnScreen(Form form)
        {
            Screen[] screens = Screen.AllScreens;
            foreach (Screen screen in screens)
            {
                Point formTopLeft = new Point(form.Left, form.Top);

                if (screen.WorkingArea.Contains(formTopLeft))
                {
                    return true;
                }
            }

            return false;
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dynamic item = wb.Document.InvokeScript("getActualItem");
            Object[] objArray = new Object[1];
            objArray[0] = (Object)item.ToString();
            wb.Document.InvokeScript("removeFromList", objArray);
        }

        private void desktopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Manager.showDesktop();
        }
    }
}
