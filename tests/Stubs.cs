using Win10BloatRemover.Operations;

namespace Win10BloatRemover.Tests
{
    class OperationStub : IOperation
    {
        private readonly string operationName;
        private readonly IUserInterface ui;

        public OperationStub(string operationName, IUserInterface ui)
        {
            this.operationName = operationName;
            this.ui = ui;
        }

        public void Run()
        {
            ui.PrintNotice($"Requested execution of operation {operationName}.");
        }
    }
}
