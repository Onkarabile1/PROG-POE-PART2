public static class ResultStatuses
{
    public const string Finished = "Finished";
    public const string DNF = "DNF"; // did not finish
    public const string DNS = "DNS"; // did not start

    public static readonly string[] All = { Finished, DNF, DNS };
}