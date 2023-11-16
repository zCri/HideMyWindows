using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HideMyWindows.App.Services.DllInjector
{
    public interface IDllInjector
    {
        public void InjectDll(Process process);
    }
}
