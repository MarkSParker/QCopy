//  QCopy - Quick Copy
//
//  Given two folders as arguments, copies A to B recursively...
//
//  1. Creates files and folders which do not exist
//  2. Overwwrites file in B when A is (a) different size or (b) modified more recently
//
//  Command format:
//
//  qcopy A:\a B:\b    
//

internal class Program
{
    private static void Main(string[] args)
    {
        var filesCreated = 0;
        var filesOverwitten = 0;
        var filesAlreadySame = 0;

        //  Folders to be copied 
        var folderA = string.Empty;
        var folderB = string.Empty;

        try
        {
            //  Check two args are given
            if (args.Length < 2)
            {
                throw new ArgumentException("missing command parameters");
            }
            else if (args.Length > 2)
            {
                throw new ArgumentException("excess command paramters");
            }

            //  Normalise args
            folderA = args[0].Trim();
            folderB = args[1].Trim();

            if (string.IsNullOrWhiteSpace(folderA))
                throw new ArgumentException($"'from' folder is not given.");

            if (string.IsNullOrWhiteSpace(folderB))
                throw new ArgumentException($"'to' folder not given.");

            //  Add a trailing slash
            if (folderA[^1] != Path.DirectorySeparatorChar)
                folderA = folderA + Path.DirectorySeparatorChar;

            if (folderB[^1] != Path.DirectorySeparatorChar)
                folderB = folderB + Path.DirectorySeparatorChar;

            //  Check both folders are existing, rooted folders
            foreach (var folder in new[] { folderA, folderB })
            {
                //  Must have min length of three, eg C:\
                if (folder.Length < 3)
                    throw new ArgumentException($"Folder '{folder}' is too short to be a fully qualified folder.");

                //  UNC not supported
                if (folder.StartsWith(@"\\"))
                    throw new ArgumentException($"Folder '{folder}' UNC paths are not supported.");

                //  Check folder exists as folder
                if (!Directory.Exists(folder))
                    throw new ArgumentException($"Folder '{folder}' does not exist.");

                //  Check rooted
                if (!Path.IsPathRooted(folder))
                    throw new ArgumentException($"Folder '{folder}' is not rooted (fully qualified).");
            }

            //  Folders must be different from each other (although mirror to self would be a no-op.)
            if (folderA == folderB)
                throw new ArgumentException("Two different folders must be specified.");

            //  Folders must not be one inside the other else madness ensues
            if (folderA.StartsWith(folderB, StringComparison.InvariantCultureIgnoreCase)
                || folderB.StartsWith(folderA, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception("One folder must not be a subfolder of the other.");
            }

            //  Build an array of all files in folderA.
            Console.WriteLine($"Scanning '{folderA}'...");

            var filesA = Directory.EnumerateFiles(folderA, "*",
                new EnumerationOptions
                {
                    AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true
                }
                ).ToArray();

            Console.WriteLine($"Files found: {filesA.Length}");

            //  For all files in src dictionary
            foreach (var fileA in filesA)
            {
                var action = "None";

                //  Compute the 'to' file name by removing folderA and pre-prending folderB
                var relativePath = Path.GetRelativePath(folderA, fileA);
                var fileB = Path.Combine(folderB, relativePath);

                //  See if we should copy the file
                var doCopy = false;

                //  Exclude temp files
                if (fileA.EndsWith(".tmp", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine($"Temp file, not copied: {fileA}");
                    continue;
                }

                //  If 'to' file does not exist, do copy
                if (!File.Exists(fileB))
                {
                    doCopy = true;
                    action = "Creating";
                    filesCreated++;
                }

                //  If 'to' file is smaller than 'from' or 'from' has more recent modification date
                //  then do copy
                if (!doCopy)
                {
                    var fileInfoA = new FileInfo(fileA);
                    var fileInfoB = new FileInfo(fileB);

                    doCopy = (fileInfoB.Length < fileInfoB.Length) || (fileInfoA.LastWriteTime > fileInfoB.LastWriteTime);

                    if (doCopy)
                    {
                        action = "Overwriting";
                        filesOverwitten++;
                    }
                }

                //  Count the files that don't need copying
                if (!doCopy)
                {
                    filesAlreadySame++;
                }

                //  Copy the file
                if (doCopy)
                {
                    Console.WriteLine($"{action}: {fileB}");

                    // Creating an existant dir is a no-op
                    var dirB = Path.GetDirectoryName(fileB);
                    if (!string.IsNullOrWhiteSpace(dirB))
                        Directory.CreateDirectory(dirB);

                    //  Copy the file
                    File.Copy(fileA, fileB, overwrite: true);
                }
            }

            //  Show final stats
            Console.WriteLine($"Files created: {filesCreated}  Files overwritten: {filesOverwitten}  Files already present: {filesAlreadySame}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex.Message);

            //  Remind user of command syntax if args are wrong
            if (ex is ArgumentException)
            {
                ReportSyntax();
            }
        }
    }

    //  ReportSyntax
    //
    //  Advises on correct command syntax
    private static void ReportSyntax()
    {
        Console.WriteLine("");
        Console.WriteLine("Command format is:-");
        Console.WriteLine("");
        Console.WriteLine(@"qcopy A:\a B:\b");    
        Console.WriteLine("");
        Console.WriteLine("Given two folders as arguments, copies A to B recursively...");
        Console.WriteLine("");
        Console.WriteLine("1. Creates files and folders which do not exist");
        Console.WriteLine("2. Overwrites file in B when A is (a) different size or (b) modified more recently");
        Console.WriteLine("");
        Console.WriteLine("For example:");
        Console.WriteLine("");
        Console.WriteLine(@"qcopy C:\Temp\MyFiles D:\MyFiles");
        Console.WriteLine("");

        return;
    }
}