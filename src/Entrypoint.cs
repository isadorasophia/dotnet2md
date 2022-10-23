using System.Reflection;

namespace DotnetToMd
{
    /// <summary>
    /// This will process C# xml documentation and metadata and generate a markdown format,
    /// optimized for mdBook.
    /// </summary>
    internal class Entrypoint
    {
        /// <summary>
        /// This will generate the markdown files based on a .xml path.
        /// </summary>
        /// <param name="args">
        /// This expects two arguments:
        ///  1. Path to the output folder, relative to the executable or absolute.
        ///  2. Target assembly name.
        ///  2. Output path of the markdown files.</param>
        /// <exception cref="ArgumentException">Whenever the arguments mismatch the expectation of <paramref name="args"/>.</exception>
        internal static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Arguments were invalid.\nExpected: Parser.exe <xml_path> <out_path> <targets>\n" +
                    "  * <xml_path>\tpath to the .xml;\n" +
                    "  * <out_path>\toutput path\n" +
                    "  * <targets>\ttarget assemblies to scan.\n");
                Console.ResetColor();

                return;
            }

            string sourcePath = ProcessPathToRoot(args[0]);
            string outputPath = ProcessPathToRoot(args[1]);

            // Name of the target assembly which we will scan.
            List<string> targetAssemblies = new();
            for (int i = 2; i < args.Length; i++)
            {
                targetAssemblies.AddRange(args[i].Split(' '));
            }

            try
            {
                Parse(sourcePath, outputPath, targetAssemblies);
            }
            catch (ReflectionTypeLoadException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ResetColor();

                Console.WriteLine($"Make sure your project has all the dependencies reachable from '{sourcePath}'.");
                return;
            }
            catch (Exception e)
            {
                // Write output before exiting.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ResetColor();

                return;
            }

            Console.WriteLine("Finished generating markdown files!");
        }

        /// <summary>
        /// Public entrypoint if called as a library.
        /// </summary>
        public static void Parse(string sourcePath, string outputPath, IEnumerable<string> targetAssemblies)
        {
            CreateIfNotFound(outputPath);

            string[] xmlFiles = GetXmlFilePaths(sourcePath).ToArray();
            if (xmlFiles.Count() == 0)
            {
                throw new ArgumentException("No .xml path was found. Please revisit the output path.");
            }

            IEnumerable<string> allAssemblies = GetAllLibrariesInPath(sourcePath);
            if (!allAssemblies.Any())
            {
                throw new InvalidOperationException("Unable to find the any binaries. Have you built the target project?");
            }

            List<Assembly> assembliesToScan = new();

            List<Assembly> dependencies = new();
            foreach (string assembly in allAssemblies)
            {
                try
                {
                    Assembly asm = Assembly.LoadFrom(assembly);
                    dependencies.Add(asm);

                    foreach (string targetAssembly in targetAssemblies)
                    {
                        if (asm.ManifestModule.Name.Equals($"{targetAssembly}.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            assembliesToScan.Add(asm);
                        }
                    }
                }
                catch (Exception e) when (e is FileLoadException || e is BadImageFormatException)
                {
                    // Ignore invalid (or native) assemblies.
                }
            }

            if (assembliesToScan.Count == 0)
            {
                throw new InvalidOperationException($"Unable to find any of the target assemblies. Did you pass the correct name?");
            }

            Parser parser = new(assembliesToScan, dependencies, xmlFiles, outputPath);
            parser.Generate();
        }

        /// <summary>
        /// Look recursively for all the files in <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Rooted path to the binaries folder. This must be a valid directory.</param>
        private static IEnumerable<string> GetAllLibrariesInPath(in string path) =>
            Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories);

        /// <summary>
        /// Look recursively for all the files in <paramref name="path"/> with a .xml file.
        /// </summary>
        /// <param name="path">Rooted path to the binaries folder. This must be a valid directory.</param>
        private static IEnumerable<string> GetXmlFilePaths(in string path)
        {
            return Directory.EnumerateFiles(path, "*.xml", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Create a directory at <paramref name="path"/> if none is found.
        /// </summary>
        private static void CreateIfNotFound(in string path)
        {
            if (!Directory.Exists(path))
            {
                _ = Directory.CreateDirectory(path);
            }
        }

        private static string ProcessPathToRoot(in string path)
        {
            if (!Path.IsPathRooted(path))
            {
                return Path.GetFullPath(Path.Join(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location), path));
            }

            return path;
        }
    }
}