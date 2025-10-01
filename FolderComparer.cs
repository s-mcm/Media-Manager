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
        private string resultsRoot = @"C:\Users\User\Downloads";

        // compute file SHA256 hash
        // Collision risk is negligible. Only a concern if two different files in the same folder produce the same hash (i.e. duplicate file with different name).
        private string ComputeHash(string filePath)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(filePath);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }

        // start folder comparison
        public void CompareFolders(string folder1, string folder2)
        {
            if (!Directory.Exists(folder1) || !Directory.Exists(folder2))
            {
                throw new DirectoryNotFoundException("One of the folders does not exist.");
            }

            CompareFoldersRecursive(folder1, folder2, folder1, folder2);
        }

        // folder comparison recursive
        private void CompareFoldersRecursive(string currentFolder1, string currentFolder2, string root1, string root2)
        {
            log.Log($"Hashing folder1: {currentFolder1}", LogLevel.Verbose);
            Dictionary<string, string> folder1Hashes = BuildFileHashes(currentFolder1);

            log.Log($"Hashing folder2: {currentFolder2}", LogLevel.Verbose);
            Dictionary<string, string> folder2Hashes = BuildFileHashes(currentFolder2);


            // Compare both directions and separately to detect uniques in both directions
            CompareFileSets(folder1Hashes, folder2Hashes, root1, "A", "MediaManager_Unique_A", "MediaManager_results", true);
            CompareFileSets(folder2Hashes, folder1Hashes, root2, "B", "MediaManager_Unique_B", null, false);


            // Recurse into subfolders as before
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
                    // Folder only in B → copy to Unique_B
                    string relativePath = Path.GetRelativePath(root2, sub2);
                    string destPath = Path.Combine(resultsRoot, "MediaManager_Unique_B", relativePath);
                    Directory.CreateDirectory(destPath);
                    CopyDirectory(sub2, destPath);
                    log.Log($"Folder missing in Folder1: {sub2}. Copied to Unique_B.", LogLevel.Info);
                    continue;
                }
                if (!Directory.Exists(sub2))
                {
                    // Folder only in A → copy to Unique_A
                    string relativePath = Path.GetRelativePath(root1, sub1);
                    string destPath = Path.Combine(resultsRoot, "MediaManager_Unique_A", relativePath);
                    Directory.CreateDirectory(destPath);
                    CopyDirectory(sub1, destPath);
                    log.Log($"Folder missing in Folder2: {sub1}. Copied to Unique_A.", LogLevel.Info);
                    continue;
                }

                CompareFoldersRecursive(sub1, sub2, root1, root2);
            }
        }

        //
        // Helpers 
        //

        // key = file hash, value = file path
        private Dictionary<string, string> BuildFileHashes(string folder)
        {
            string[] files = Directory.GetFiles(folder);
            return files.ToDictionary(f => ComputeHash(f), f => f);
        }

        private void CompareFileSets(
            Dictionary<string, string> sourceHashes,
            Dictionary<string, string> targetHashes,
            string sourceRoot,
            string label,
            string uniqueFolder,
            string? duplicateFolder,
            bool checkDuplicates)
        {
            foreach (var kvp in sourceHashes)
            {
                string relativePath = Path.GetRelativePath(sourceRoot, kvp.Value);

                if (!targetHashes.TryGetValue(kvp.Key, out string? matchingPath))
                {
                    log.Log($"Unique in {label}: {relativePath}", LogLevel.Info);
                    CopyWithStructure(kvp.Value, Path.Combine(resultsRoot, uniqueFolder), relativePath);
                }
                else if (checkDuplicates && duplicateFolder != null)
                {
                    log.Log($"Duplicate found: {relativePath}", LogLevel.Verbose);
                    CopyWithStructure(kvp.Value, Path.Combine(resultsRoot, duplicateFolder), relativePath);
                }
            }
        }

        private void CopyWithStructure(string sourcePath, string destRoot, string relativePath)
        {
            string destPath = Path.Combine(destRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            try
            {
                File.Copy(sourcePath, destPath, true);
            }
            catch (Exception ex)
            {
                log.Log($"Failed to copy \"{sourcePath}\" → \"{destPath}\": {ex.Message}", LogLevel.Error);
            }
        }

        // to copy entire directory recursively
        private void CopyDirectory(string sourceDir, string destDir)
        {
            foreach (string dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dir.Replace(sourceDir, destDir));
            }
            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string destFile = file.Replace(sourceDir, destDir);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

                try
                {
                    File.Copy(file, destFile, true);
                }
                catch (Exception ex)
                {
                    log.Log($"Failed to copy \"{file}\" → \"{destFile}\": {ex.Message}", LogLevel.Error);
                }
            }
        }
    }
}
