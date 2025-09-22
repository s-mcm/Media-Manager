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
            log.Log($"GetFiles from folder 1: {currentFolder1}", LogLevel.Verbose);
            string[] files1 = Directory.GetFiles(currentFolder1);
            log.Log($"GetFiles from folder 2: {currentFolder2}", LogLevel.Verbose);
            string[] files2 = Directory.GetFiles(currentFolder2);

            // Build dictionary: hash -> full path
            log.Log($"Compute file hash for all files from folder 1", LogLevel.Verbose);
            var folder1Hashes = files1.ToDictionary(f => ComputeHash(f), f => f);
            log.Log($"Compute file hash for all files from folder 2", LogLevel.Verbose);
            var folder2Hashes = files2.ToDictionary(f => ComputeHash(f), f => f);

            // Check files from folder1 in folder2
            foreach (var kvp in folder1Hashes)
            {
                string relativePath = Path.GetRelativePath(root1, kvp.Value);

                if (!folder2Hashes.ContainsKey(kvp.Key))
                {
                    log.Log($"File {root1}\\{relativePath} no match found in folder 2.", LogLevel.Info);
                }
                else
                {
                    log.Log($"File {root1}\\{relativePath} match found in folder 2: {root2}\\{relativePath}.", LogLevel.Verbose);
                }
            }

            // Check files from folder2 in folder1
            foreach (var kvp in folder2Hashes)
            {
                string relativePath = Path.GetRelativePath(root2, kvp.Value);

                if (!folder1Hashes.ContainsKey(kvp.Key))
                {
                    log.Log($"Missing or different in Folder1: {relativePath}", LogLevel.Error);
                }
                else
                {
                    log.Log($"Match found in Folder2: {relativePath}", LogLevel.Verbose);
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
                    log.Log($"Folder missing in Folder1: {sub2}", LogLevel.Error);
                    continue;
                }
                if (!Directory.Exists(sub2))
                {
                    log.Log($"Folder missing in Folder2: {sub1}", LogLevel.Error);
                    continue;
                }

                CompareFoldersRecursive(sub1, sub2, root1, root2);
            }
        }
    }
}
