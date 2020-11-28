namespace Win10BloatRemover.Operations
{
    public interface IOperation
    {
        void Run();
        bool IsRebootRecommended => false;
    }
}
