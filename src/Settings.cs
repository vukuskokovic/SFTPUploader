using System.Text.Json.Serialization;

namespace SFTPUploader;

public class Settings
{
    [JsonIgnore]
    public static Settings Instance {get;set;} = null!;
    [JsonIgnore]
    public static List<string> IgnoredFolders {get;set;} = [];

    public int IntervalMs {get;set;} = 1000;
    public string RemoteFolder {get;set;} = "";
    public string LocalFolder {get;set;} = "";
    public string[] Ignore {get;set;} = [];
    public ConnectionSettings ConnectionSettings{get;set;} = new ();
}

public class ConnectionSettings
{
    public string Url {get;set;} = "";
    public string Username {get;set;} = "";
    public int PortNumber {get;set;} = 0;
    public string? Password {get;set;} = null;
    public bool GiveUpSecurityAndAcceptAnySshHostKey{get;set;} = false;
    public string? PrivateKeyPassphrase{get;set;} = null;
    public string? SshPrivateKey{get;set;} = null;
    public string? SshPrivateKeyPath{get;set;} = null;
    public string? SshHostKeyFingerprint{get;set;} = null;
    public bool Secure {get;set;} = true;
}