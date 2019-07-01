using System;
using System.Resources;
using Win10BloatRemover.Utils;

namespace Win10BloatRemover.Operations
{
    class LicensePrinter : IOperation
    {
        public void PerformTask()
        {
            var resources = new ResourceManager("Win10BloatRemover.resources.Resources", typeof(Program).Assembly);
            ConsoleUtils.WriteLine(resources.GetString("LicenseText"), ConsoleColor.Cyan);
        }
    }
}
