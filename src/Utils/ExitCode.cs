namespace Win10BloatRemover.Utils
{
    public readonly struct ExitCode
    {
        private readonly int code;

        public ExitCode(int code)
        {
            this.code = code;
        }

        public bool IsSuccessful() => code == 0;
        public bool IsNotSuccessful() => !IsSuccessful();

        public override string ToString() => code.ToString();

        public static implicit operator int(ExitCode e) => e.code;
    }
}
