namespace Win10BloatRemover.Utils;

class RebootRecommendedFlag
{
    public bool IsRebootRecommended { private set; get; }

    public void SetRecommended()
    {
        IsRebootRecommended = true;
    }

    public void UpdateIfNeeded(bool recommendsReboot)
    {
        if (IsRebootRecommended)
            return;

        IsRebootRecommended = recommendsReboot;
    }
}
