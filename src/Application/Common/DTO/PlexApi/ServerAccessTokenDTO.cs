namespace PlexRipper.Application;

public class ServerAccessTokenDTO
{
    public int PlexAccountId { get; set; }

    public string MachineIdentifier { get; set; }

    public string AccessToken { get; set; }
}