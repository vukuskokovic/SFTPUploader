using System.Text.Json;
using SFTPUploader;
using WinSCP;

const string settingsFilePath = "settings.json";

static void PressAnyKeyToContinue(){
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    Environment.Exit(0);
}

if(!File.Exists(settingsFilePath)){
    File.WriteAllText(settingsFilePath, JsonSerializer.Serialize(
        new Settings(), 
        new JsonSerializerOptions(){
            WriteIndented = true
        }
    ));
    Console.WriteLine($"Fill in {settingsFilePath}");
    PressAnyKeyToContinue();
    return;
}

var settingsJson = File.ReadAllText(settingsFilePath);
try{
    Settings.Instance = JsonSerializer.Deserialize<Settings>(settingsJson)!;
    if(Settings.Instance == null)throw new Exception();
}
catch{
    Console.Error.WriteLine($"Error reading {settingsFilePath}");
    PressAnyKeyToContinue();
    return;
}
var connectionSettings = Settings.Instance.ConnectionSettings;
SessionOptions options = new(){
    PortNumber = connectionSettings.PortNumber,
    HostName = connectionSettings.Url,
    Password = connectionSettings.Password,
    UserName = connectionSettings.Username,
    PrivateKeyPassphrase = connectionSettings.PrivateKeyPassphrase,
    SshPrivateKey = connectionSettings.SshPrivateKey,
    SshPrivateKeyPath = connectionSettings.SshPrivateKeyPath,
    SshHostKeyFingerprint = connectionSettings.SshHostKeyFingerprint,
    GiveUpSecurityAndAcceptAnyTlsHostCertificate = connectionSettings.GiveUpSecurityAndAcceptAnySshHostKey,
    Protocol = Protocol.Sftp,
    Secure = connectionSettings.Secure
};
if(!Directory.Exists(Settings.Instance.LocalFolder)){
    Console.WriteLine("Local folder does not exist");
    PressAnyKeyToContinue();
    return;
}



foreach(var ignore in Settings.Instance.Ignore){
    try{
        var path = Path.Combine(Settings.Instance.LocalFolder, ignore);
        Settings.IgnoredFolders.Add(path);
        Console.WriteLine($"Ignoring folder {path}");
    }
    catch{
        Console.WriteLine($"'{ignore}' is not a valid folder path");
        PressAnyKeyToContinue();
        return;
    }
}

Folder? mainFolder = Folder.Init(Settings.Instance.LocalFolder);
if(mainFolder == null){
    Console.WriteLine("You cannot ignore the main folder");
    PressAnyKeyToContinue();
    return;
}
Session session = new();

async Task OpenConnection(){
    while(true){
        try{
            session.Open(options);
            break;
        }
        catch(Exception ex){
            Console.WriteLine("Could not open connection " + ex.ToString());
            Console.WriteLine("Retrting in 2 secconds...");
            await Task.Delay(2000);
        }
    }
}
await OpenConnection();

Console.WriteLine("Program started");
while(true){
    var differences = mainFolder.GetDifferences();
    while(differences.TryDequeue(out var diff)){
        if(!session.Opened)
            await OpenConnection();

        var remotePath = RemotePath.TranslateLocalPathToRemote(diff.Path, Settings.Instance.LocalFolder, Settings.Instance.RemoteFolder);
        switch(diff.Type){
            case DifferenceType.RemoveFolder:
                Console.WriteLine("Removing folder " + remotePath);
                if(session.FileExists(remotePath))
                    session.RemoveFiles(remotePath);
                break;
            case DifferenceType.NewFolder:
                Console.WriteLine("Creating folder " + remotePath);
                session.CreateDirectory(remotePath);
                break;
            case DifferenceType.FileDiff:
                Console.WriteLine("Copying file " + remotePath);
                var fileStream = new FileStream(diff.Path, FileMode.Open);
                try{
                    session.PutFile(fileStream, remotePath);
                }
                finally{
                    await fileStream.DisposeAsync();
                }
                break;
            case DifferenceType.RemoveFile:
                Console.WriteLine("Removing file " + remotePath);
                if(session.FileExists(remotePath))
                    session.RemoveFile(remotePath);
                break;
        }
    }
    
    await Task.Delay(Settings.Instance.IntervalMs);
}