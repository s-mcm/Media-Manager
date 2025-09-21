// See https://aka.ms/new-console-template for more information

using MediaDevices;
using MediaManager;
using MediaManager.Logging;
using System.IO;
using System.Reflection;



string destinationFolder = @"D:\.Backups\fucking photos";
LogLevel logLevel = LogLevel.Verbose;







ILogger logger = new ConsoleLogger(logLevel);

iPhoneManager manager = new iPhoneManager(logger);

manager.SetDestinationFolder(destinationFolder);

manager.ProcessAllFolderRecursive();


manager.Disconnect();














/*
PhotoRenamer renamer = new PhotoRenamer(folderPath);
renamer.RenameFiles();
Console.WriteLine("✅ All files renamed!"); //*/
