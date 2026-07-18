namespace ServiceLib.Models.Dto.CliCommand;

public record NodeListItem(
    string Id,
    string Name,
    string Address,
    int Port,
    string Type,
    string Delay,
    string GroupId = "",
    string GroupName = "")
{
    public override string ToString() => Name;
}
