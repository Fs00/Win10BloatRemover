namespace Win10BloatRemover;

class RebootRecommendedFlag
{
    public bool IsRebootRecommended { private set; get; }

    public void SetRebootRecommended()
    {
        IsRebootRecommended = true;
    }
}
