using MediaManager.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MediaManager
{
    internal class FolderComparer(ILogger logger)
    {
        private readonly ILogger log = logger ?? throw new ArgumentNullException(nameof(logger));

        private string ComputeHash(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            return BitConverter.ToString(md5.ComputeHash(stream));
        }

        public void CompareFolders(string folder1, string folder2)
        {
            if (!Directory.Exists(folder1) || !Directory.Exists(folder2))
            {
                throw new DirectoryNotFoundException("One of the folders does not exist.");
            }

            CompareFoldersRecursive(folder1, folder2, folder1, folder2);
        }

        private void CompareFoldersRecursive(string currentFolder1, string currentFolder2, string root1, string root2)
        {
            // Get files in current folders
            string[] files1 = Directory.GetFiles(currentFolder1);
            string[] files2 = Directory.GetFiles(currentFolder2);

            // Build dictionary: hash -> full path
            var folder1Hashes = files1.ToDictionary(f => ComputeHash(f), f => f);
            var folder2Hashes = files2.ToDictionary(f => ComputeHash(f), f => f);

            // Check files from folder1 in folder2
            foreach (var kvp in folder1Hashes)
            {
                if (!folder2Hashes.ContainsKey(kvp.Key))
                {
                    string relativePath = Path.GetRelativePath(root1, kvp.Value);
                    Log($"Missing or different in Folder2: {relativePath}", CompareLogLevel.ErrorsOnly);
                }
                else if (logLevel == CompareLogLevel.Verbose)
                {
                    string relativePath = Path.GetRelativePath(root1, kvp.Value);
                    Log($"Match found in Folder2: {relativePath}", CompareLogLevel.Verbose);
                }
            }

            // Check files from folder2 in folder1
            foreach (var kvp in folder2Hashes)
            {
                if (!folder1Hashes.ContainsKey(kvp.Key))
                {
                    string relativePath = Path.GetRelativePath(root2, kvp.Value);
                    Log($"Missing or different in Folder1: {relativePath}", CompareLogLevel.ErrorsOnly);
                }
            }

            // Recurse into subfolders
            string[] dirs1 = Directory.GetDirectories(currentFolder1);
            string[] dirs2 = Directory.GetDirectories(currentFolder2);

            var allDirs = new HashSet<string>(dirs1.Select(d => Path.GetFileName(d)));
            allDirs.UnionWith(dirs2.Select(d => Path.GetFileName(d)));

            foreach (var dirName in allDirs)
            {
                string sub1 = Path.Combine(currentFolder1, dirName);
                string sub2 = Path.Combine(currentFolder2, dirName);

                // If folder missing in one side, log it
                if (!Directory.Exists(sub1))
                {
                    Log($"Folder missing in Folder1: {sub2}", CompareLogLevel.ErrorsOnly);
                    continue;
                }
                if (!Directory.Exists(sub2))
                {
                    Log($"Folder missing in Folder2: {sub1}", CompareLogLevel.ErrorsOnly);
                    continue;
                }

                CompareFoldersRecursive(sub1, sub2, root1, root2);
            }
        }
    }
}
