using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaManager
{
    internal class PhotoRenamer
    {
        private readonly string folderPath;

        public PhotoRenamer(string folderPath)
        {
            this.folderPath = folderPath;
        }

        public void RenameFiles()
        {
            string[] files = Directory.GetFiles(folderPath);

            foreach (string filePath in files)
            {
                RenameSingleFile(filePath);
            }
        }

        private void RenameSingleFile(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            DateTime modifiedDate = fileInfo.LastWriteTime;
            string newFileName = GenerateFileName(modifiedDate, fileInfo.Extension);
            string newPath = Path.Combine(folderPath, newFileName);

            newPath = EnsureUniqueFileName(newPath, modifiedDate, fileInfo.Extension);

            //File.Move(filePath, newPath);
            Console.WriteLine($"Renamed: {fileInfo.Name} -> {Path.GetFileName(newPath)}");
        }

        private string GenerateFileName(DateTime date, string extension)
        {
            return date.ToString("yyyy-MM-dd_HH-mm-ss") + extension;
        }

        private string EnsureUniqueFileName(string newPath, DateTime date, string extension)
        {
            int counter = 1;
            string uniquePath = newPath;

            while (File.Exists(uniquePath))
            {
                string newFileName = date.ToString("yyyy-MM-dd_HH-mm-ss") + $"_{counter}" + extension;
                uniquePath = Path.Combine(folderPath, newFileName);
                counter++;
            }

            return uniquePath;
        }
    }
}
