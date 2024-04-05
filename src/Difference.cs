namespace SFTPUploader;

public enum DifferenceType
{
    RemoveFolder,
    NewFolder,
    FileDiff,
    RemoveFile
}

public class Difference{
    public DifferenceType Type {get;set;}
    public string Path {get;set;}
    public Difference(DifferenceType type, string path){
        Type = type;
        //Path = System.IO.Path.GetRelativePath(Settings.Instance.LocalFolder, path);
        Path = path;
    }
}