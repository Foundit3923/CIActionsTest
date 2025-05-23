using System.IO;
using UnityEngine;

public class _internal : MonoBehaviour
{
    private string PersistentDataDirectory;
    [SerializeField] private string internalDataFolderPath;
    public string InternalDataFolderPath => internalDataFolderPath;
    [SerializeField] private string animationConditionsDictFilepath;
    public string AnimationConditionsDictFilepath => animationConditionsDictFilepath;

    private string serializedDataFileExtension = ".json";

    private void Awake()
    {
        if (InternalDataFolderPath != null && AnimationConditionsDictFilepath != null)
        {
            SetFilePaths();
        }
    }

    private void SetFilePaths()
    {
        PersistentDataDirectory = Application.persistentDataPath;
        internalDataFolderPath = System.IO.Path.Combine(PersistentDataDirectory, InternalDataFolderPath);
        if (!Directory.Exists(internalDataFolderPath))
        {
            Directory.CreateDirectory(internalDataFolderPath);
        }

        animationConditionsDictFilepath = System.IO.Path.Combine(InternalDataFolderPath, AnimationConditionsDictFilepath);
        animationConditionsDictFilepath = AnimationConditionsDictFilepath + serializedDataFileExtension;
        if (!File.Exists(animationConditionsDictFilepath))
        {
            File.Create(animationConditionsDictFilepath);
        }
    }

    public void ProvideFilePaths(string InternalDataFolderName, string AnimationConditionsDictFilename)
    {
        internalDataFolderPath = InternalDataFolderName;
        animationConditionsDictFilepath = AnimationConditionsDictFilename;
        SetFilePaths();
    }
}
