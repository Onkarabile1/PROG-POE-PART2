namespace RaceDay.Web.Models;

public static class FormOptions
{
    public static readonly string[] EventTypes = { "Run", "Walk", "Cycle" };

    public static readonly string[] Provinces =
    {
        "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal", "Limpopo",
        "Mpumalanga", "North West", "Northern Cape", "Western Cape"
    };

    public static readonly string[] ResultStatuses = { "Finished", "DNF", "DNS" };
}