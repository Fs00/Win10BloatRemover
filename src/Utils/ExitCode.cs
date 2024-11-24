namespace Win10BloatRemover.Utils;

readonly struct ExitCode(int code)
{
    private readonly int code = code;

    public bool IsSuccessful() => code == 0;
    public bool IsNotSuccessful() => !IsSuccessful();

    public override string ToString() => code.ToString();

    public static implicit operator int(ExitCode e) => e.code;
}
