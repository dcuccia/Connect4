namespace Connect4.Components;

public record MutableName
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    public bool IsValid() => !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName);
    public override string ToString() => $"{FirstName} {LastName}";
}
