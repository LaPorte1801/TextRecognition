using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace TextRecognition
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class ObjectForScriptingHelper
    {
        MainWindow mExternalWPF;
        public ObjectForScriptingHelper(MainWindow w)
        {
            this.mExternalWPF = w;
        }

        public void InvokeMeFromJavascript(string jsscript)
        {
            this.mExternalWPF._recivedData = string.Format(jsscript);
        }

    }
}
