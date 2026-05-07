namespace DragChain.API.Models;

public static class PipeTypeCategory
{
    public const string Tube = "tube";
    public const string WeakCable = "weak_cable";
    public const string StrongCable = "strong_cable";
    public const string Encoder = "encoder";
    public const string Other = "other";

    public static string Normalize(string? type)
    {
        return type switch
        {
            Tube or WeakCable or StrongCable or Encoder or Other => type,
            "cable" => WeakCable,
            _ => Other
        };
    }

    public static bool IsStrongCable(string type) => Normalize(type) == StrongCable;
}
