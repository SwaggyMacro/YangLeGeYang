using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SheepSheep
{
    public static class InputDialog
    {
        public static DialogResult Show(out string strText, string title)
        {
            string strTemp = string.Empty;

            FrmInputDialog inputDialog = new FrmInputDialog(title);
            inputDialog.TextHandler = (str) => { strTemp = str; };

            DialogResult result = inputDialog.ShowDialog();
            strText = strTemp;

            return result;
        }
    }
}
