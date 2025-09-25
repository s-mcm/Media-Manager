using MediaDevices;
using MediaManager.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CA1416

namespace MediaManager
{
    internal class iPhoneManager(ILogger logger)
    {
        private readonly ILogger log = logger ?? throw new ArgumentNullException(nameof(logger));
        private MediaDevice? device;


        private bool Connect(string deviceNameContains = "iPhone")
        {
            try
            {
                log.Log($"Attempting to connect to {deviceNameContains}...", LogLevel.Info);

                device = MediaDevice.GetDevices()
                    .FirstOrDefault(d => d.FriendlyName.Contains(deviceNameContains));

                if (device == null)
                {
                    log.Log("No device found.", LogLevel.Error);
                    return false;
                }

                device.Connect();

                log.Log($"Connected to {device.FriendlyName}!", LogLevel.Info);

                return device.IsConnected;
            }
            catch (Exception ex)
            {
                log.Log($"Exception during connect: {ex.Message}", LogLevel.Error);
                return false;
                throw;
            }            
        }

        public void Disconnect()
        {
            log.Log($"Disconnected from device {device?.FriendlyName}.", LogLevel.Info);
            device?.Disconnect();
        }

        public string[] ListDirectories(string path = @"\")
        {
            log.Log($"Getting directories from {path}.", LogLevel.Verbose);
            return device.GetDirectories(path);
        }

        public string[] ListFiles(string path)
        {
            log.Log($"Getting files from {path}.", LogLevel.Verbose);
            return device.GetFiles(path);
        }

        public MediaFileInfo GetFileInfo(string path)
        {
            log.Log($"Getting MediaFileInfo from {path}.", LogLevel.Verbose);
            return device.GetFileInfo(path);
        }

        private string GenerateFileName(MediaFileInfo fileInfo, string sourcePath)
        {
            if (fileInfo.CreationTime is DateTime creationTime)
            {
                return creationTime.ToString("yyyyMMdd_HHmmss") + " " + Path.GetFileName(sourcePath);
            }
            else
            {
                throw new Exception($"Cannot get creation time for {sourcePath}");
            }
        }

        public void DownloadFile(string sourcePath, string destinationFolder)
        {
            MediaFileInfo fileInfo = GetFileInfo(sourcePath);
            string newFileName = GenerateFileName(fileInfo, sourcePath);
            string destinationPath = Path.Combine(destinationFolder, newFileName);

            device.DownloadFile(sourcePath, destinationPath);
            log.Log($"Sucessfully downloaded: {sourcePath} to {destinationPath}", LogLevel.Verbose);
        }

        // print all MediaFileInfo properties
        public void PrintFileInfoProperties(MediaFileInfo fileInfo)
        {
            var properties = typeof(MediaFileInfo).GetProperties();
            foreach (var prop in properties)
            {
                object value = prop.GetValue(fileInfo);
                Console.WriteLine($"{prop.Name}: {value}");
            }
        }

        public string DestinationFolder { get; private set; }

        public void SetDestinationFolder(string folder)
        {
            DestinationFolder = folder;
            Directory.CreateDirectory(DestinationFolder);
            log.Log($"Files will be saved to {DestinationFolder}", LogLevel.Info);
        }

        // Recursive method to process all images in a folder and subfolders
        public void ProcessAllFolderRecursive()
        {
            if (!Connect())
                throw new Exception("Device not connected.");

            if (string.IsNullOrEmpty(DestinationFolder))
                throw new Exception("DestinationFolder is not set.");


            // Start from the root DCIM folder (or device root)
            string rootFolder = @"\";
            log.Log($"Processing folder {rootFolder}", LogLevel.Verbose);
            ProcessAllFolderRecursive(rootFolder);
        }

        // Keep the existing private recursive method
        private void ProcessAllFolderRecursive(string remoteFolder)
        {
            log.Log($"Current folder: {remoteFolder}", LogLevel.Verbose);

            foreach (string file in ListFiles(remoteFolder))
            {
                try
                { 
                    // Compute relative path from root
                    string relativePath = file.Substring(@"\".Length).TrimStart('\\');
                    // Compute the relative folder path (without the filename)
                    string relativeFolder = Path.GetDirectoryName(relativePath);

                    // Combine with your destination root
                    string localFolder = Path.Combine(DestinationFolder, relativeFolder);
                    Directory.CreateDirectory(localFolder);


                    DownloadFile(file, localFolder);


                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading {file}: {ex.Message}");
                }

                Task.Delay(100).Wait(); // 100ms pause between files. possibly redundant - call MediaDevice.DownloadFile is synchronous/blocking so in theory the method does not return until the entire file has been transferred

            }

            foreach (string dir in ListDirectories(remoteFolder))
            {
                ProcessAllFolderRecursive(dir);
            }
        }
    }
}
