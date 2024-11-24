namespace Win10BloatRemover.Operations;

interface IOperation
{
    void Run();
    bool IsRebootRecommended => false;
}
