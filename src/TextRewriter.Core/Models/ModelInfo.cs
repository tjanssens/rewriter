namespace TextRewriter.Core.Models;

public sealed class ModelInfo
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";

    public override string ToString() => DisplayName;
    public override bool Equals(object? obj) => obj is ModelInfo other && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}
