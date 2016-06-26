using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskMask
{
    class Item
    {
        public int id = 0;
        public IntPtr handle = IntPtr.Zero;
        public string title = "";
        public bool visibility = false;
        public string image = null;
    };
}
