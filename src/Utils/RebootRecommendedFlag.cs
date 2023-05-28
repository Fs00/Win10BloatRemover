namespace Win10BloatRemover.Utils;

class RebootRecommendedFlag
{
    public bool IsRebootRecommended { private set; get; }

    public void SetRebootRecommended()
    {
        IsRebootRecommended = true;
    }
}
