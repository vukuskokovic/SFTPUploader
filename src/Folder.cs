namespace SFTPUploader;

public class Folder
{
    public Dictionary<string, DateTime> Files = [];
    public Dictionary<string, Folder> SubFolders = [];

    private HashSet<string> CachedIgnoredFolders = [];
    private string FolderPath {get;init;}

    private Folder(string path){
        FolderPath = path;
    }

    public static Folder? Init(string path){
        foreach(var ignoredFolder in Settings.IgnoredFolders){
            //Ignored folder
            if(Path.GetRelativePath(ignoredFolder, path) == ".")return null;
        }

        Folder folder = new(path);

        foreach(var file in Directory.EnumerateFiles(path)){
            folder.Files[file] = File.GetLastWriteTime(file);
        }

        foreach(var directory in Directory.EnumerateDirectories(path)){
            var newFolder = Init(directory);
            if(newFolder == null)continue;

            folder.SubFolders[directory] = newFolder;
        }

        return folder;
    }
    
    public Queue<Difference> GetDifferences()
    {   
        Queue<Difference> differences = [];

        foreach(var folder in SubFolders)
        {
            if(!Directory.Exists(folder.Key))continue;
            foreach(var item in folder.Value.GetDifferences()){
                differences.Enqueue(item);
            }
        }

        var dirFolders = Directory.GetDirectories(FolderPath);

        var newFolders = dirFolders.Where(x => !SubFolders.ContainsKey(x) && !CachedIgnoredFolders.Contains(x));
        foreach(var newFolder in newFolders)
        {
            var folder = Init(newFolder);
            if(folder == null){
                CachedIgnoredFolders.Add(newFolder);
                continue;
            }

            differences.Enqueue(new Difference(DifferenceType.NewFolder, newFolder));
            SubFolders.Add(newFolder, folder);
        }

        var deletedFolders = SubFolders.Keys.Where(x => !dirFolders.Contains(x)).ToList();
        foreach(var deletedFolder in deletedFolders){
            differences.Enqueue(new Difference(DifferenceType.RemoveFolder, deletedFolder));
            SubFolders.Remove(deletedFolder);
        }

        var dirFiles = Directory.GetFiles(FolderPath);
        var filesToRemove = new List<string>();
        foreach(var file in Files.ToList()){
            if(!File.Exists(file.Key)){
                filesToRemove.Add(file.Key);
                differences.Enqueue(new Difference(DifferenceType.RemoveFile, file.Key));
                continue;
            }

            var lastWrite = File.GetLastWriteTime(file.Key);
            if(lastWrite != file.Value){
                Files[file.Key] = lastWrite;
                differences.Enqueue(new Difference(DifferenceType.FileDiff, file.Key));
            }
        }
        
        foreach(var file in dirFiles.Where(x => !Files.ContainsKey(x))){
            Files[file] = File.GetLastWriteTime(file);
            differences.Enqueue(new Difference(DifferenceType.FileDiff, file));
        }

        filesToRemove.ForEach(x => Files.Remove(x));

        return differences;
    }
}