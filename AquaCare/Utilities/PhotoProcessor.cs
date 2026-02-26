using System.IO;
using System.Linq;

namespace AquaCare;

public class PhotoProcessor
{
    public static List<string> ProcessSpeciesPhoto(string directoryName, List<string> selectedPhotoPaths)
    {
        var dbImagePaths = new List<string>();
        string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string imagesFolder = Path.Combine(appDirectory, directoryName);
        Directory.CreateDirectory(imagesFolder);

        foreach (string originalPath in selectedPhotoPaths)
        {
            string fileName = Path.GetFileName(originalPath);
            string destinationPath = Path.Combine(imagesFolder, fileName);
            File.Copy(originalPath, destinationPath, true);

            string relativePath = Path.Combine(directoryName, fileName);
            dbImagePaths.Add(relativePath);
        }
        return dbImagePaths;
    }
}