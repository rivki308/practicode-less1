using System;
using System.CommandLine;
using System.Globalization;
using פרקטיקוד.Services;


RootCommand rootCommand = new RootCommand(
    "פרקטיקוד - כלי לאיחוד קובצי מקור");

Command bundleCommand = new Command(
    "bundle",
    "איחוד קובצי מקור לקובץ אחד");

Option<string> outputOption = new Option<string>(
    "--output")
{
    Description = "Path of the output bundle file",
    Aliases = { "-o" }
};

bundleCommand.Options.Add(outputOption);

Option<string[]> languageOption = new Option<string[]>(
    "--language")
{
    Description = "File extensions to include, or 'all'",
    Aliases = { "-l" },
    AllowMultipleArgumentsPerToken = true,
    Required = true
};

bundleCommand.Options.Add(languageOption);


Option<bool> noteOption = new Option<bool>("--note")
{
    Description = "Include source file path comments",
    Aliases = { "-n" }
};

bundleCommand.Options.Add(noteOption);


Option<bool> removeEmptyLinesOption =
    new Option<bool>("--remove-empty-lines")
    {
        Description = "Remove empty lines from source files",
        Aliases = { "-r" }
    };

bundleCommand.Options.Add(removeEmptyLinesOption);


Option<string> sortOption = new Option<string>("--sort")
{
    Description = "Sort files by 'file' or 'type'",
    Aliases = { "-s" },
    DefaultValueFactory = _ => "file"
};

bundleCommand.Options.Add(sortOption);



Option<string?> authorOption = new Option<string?>("--author")
{
    Description = "Name of the author",
    Aliases = { "-a" }
};

bundleCommand.Options.Add(authorOption);



bundleCommand.SetAction(parseResult =>
{
    string? outputPath = parseResult.GetValue(outputOption);
    string[]? extensions = parseResult.GetValue(languageOption);

    if (extensions != null &&
        !extensions.Any(e =>
            e.Equals("all", StringComparison.OrdinalIgnoreCase)))
    {
        extensions = extensions
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.StartsWith(".") ? e : "." + e)
            .ToArray();
    }

    if (extensions != null &&
        !extensions.Any(e =>
            e.Equals("all", StringComparison.OrdinalIgnoreCase)))
    {
        HashSet<string> supportedExtensions = new(
            StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".js", ".jsx", ".ts", ".tsx", ".py",
            ".java", ".c", ".cpp", ".h", ".hpp",
            ".html", ".css", ".sql"
        };

        string[] unsupportedExtensions = extensions
            .Where(e => !supportedExtensions.Contains(e))
            .ToArray();

        if (unsupportedExtensions.Length > 0)
        {
            Console.WriteLine(
                $"Error: Unsupported language(s): " +
                $"{string.Join(", ", unsupportedExtensions.Select(e => e.TrimStart('.')))}.");

            return;
        }
    }


    bool includeSourceNotes = parseResult.GetValue(noteOption);
    bool removeEmptyLines = parseResult.GetValue(removeEmptyLinesOption);
    string sortBy = parseResult.GetValue(sortOption)!;
    if (!sortBy.Equals("file", StringComparison.OrdinalIgnoreCase) &&
    !sortBy.Equals("type", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine(
            "Error: Sort must be 'file' or 'type'.");
        return;
    }

    string? author = parseResult.GetValue(authorOption);

    string sourceDirectory = Directory.GetCurrentDirectory();

    if (string.IsNullOrWhiteSpace(outputPath))
    {
        Console.WriteLine("Error: Output path is required.");
        return;
    }

    if (extensions == null || extensions.Length == 0)
    {
        Console.WriteLine("Error: At least one language is required.");
        return;
    }

    try
    {
        string? outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));

        if (!Directory.Exists(outputDirectory))
        {
            Console.WriteLine($"Error: The folder '{outputDirectory}' does not exist.");
            return;
        }

        BundleService service = new BundleService();

        service.CreateBundle(
            sourceDirectory,
            outputPath,
            extensions,
            includeSourceNotes,
            removeEmptyLines,
            sortBy,
            author);

        Console.WriteLine($"Bundle created successfully: {outputPath}");
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine($"Error: No permission to write to '{outputPath}'.");
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Error: Could not write the file '{outputPath}': {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
});


Command createRspCommand = new Command(
    "create-rsp",
    "Create a response file for the bundle command");

Option<string> rspFileOption = new Option<string>("--file")
{
    Description = "Name of the response file",
    Aliases = { "-f" }
};

createRspCommand.Options.Add(rspFileOption);

createRspCommand.SetAction(parseResult =>
{
    string? fileName = parseResult.GetValue(rspFileOption);

    if (string.IsNullOrWhiteSpace(fileName))
    {
        Console.WriteLine("Error: Response file name is required.");
        return;
    }

    Console.WriteLine("Enter languages (for example: cs js or all):");
    string? languages = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(languages))
    {
        Console.WriteLine("Error: At least one language is required.");
        return;
    }

    string[] languageList = languages.Split(
        ' ',
        StringSplitOptions.RemoveEmptyEntries);

    if (!languageList.Any(e =>
            e.Equals("all", StringComparison.OrdinalIgnoreCase)))
    {
        HashSet<string> supportedExtensions = new(
            StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".js", ".jsx", ".ts", ".tsx", ".py",
            ".java", ".c", ".cpp", ".h", ".hpp",
            ".html", ".css", ".sql"
        };

        string[] unsupportedLanguages = languageList
            .Where(e => !supportedExtensions.Contains(
                e.StartsWith(".") ? e : "." + e))
            .ToArray();

        if (unsupportedLanguages.Length > 0)
        {
            Console.WriteLine(
                $"Error: Unsupported language(s): " +
                $"{string.Join(", ", unsupportedLanguages)}.");
            return;
        }
    }

    Console.WriteLine("Enter output file path:");
    string? output = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(output))
    {
        Console.WriteLine("Error: Output path is required.");
        return;
    }

    Console.WriteLine("Include source notes? (y/n):");
    string? note = Console.ReadLine();

    if (note?.Trim().ToLower() != "y" &&
        note?.Trim().ToLower() != "n")
    {
        Console.WriteLine(
            "Error: Please enter y or n for source notes.");
        return;
    }

    Console.WriteLine("Remove empty lines? (y/n):");
    string? removeEmpty = Console.ReadLine();

    if (removeEmpty?.Trim().ToLower() != "y" &&
        removeEmpty?.Trim().ToLower() != "n")
    {
        Console.WriteLine(
            "Error: Please enter y or n for removing empty lines.");
        return;
    }

    Console.WriteLine("Sort by file or type (press Enter for file):");
    string? sort = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(sort))
        sort = "file";

    if (!sort.Trim().Equals("file", StringComparison.OrdinalIgnoreCase) &&
        !sort.Trim().Equals("type", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Error: Sort must be 'file' or 'type'.");
        return;
    }

    Console.WriteLine("Enter author name (or press Enter to skip):");
    string? author = Console.ReadLine();

    // Values with spaces must be quoted so the response file keeps them as one token
    static string Quote(string value) =>
        value.Contains(' ') ? $"\"{value}\"" : value;

    List<string> arguments = new()
    {
        "bundle",
        "--language"
    };

    arguments.AddRange(languageList);

    arguments.Add("--output");
    arguments.Add(Quote(output.Trim()));

    if (note?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true)
        arguments.Add("--note");

    if (removeEmpty?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true)
        arguments.Add("--remove-empty-lines");

    arguments.Add("--sort");
    arguments.Add(sort.Trim());

    if (!string.IsNullOrWhiteSpace(author))
    {
        arguments.Add("--author");
        arguments.Add(Quote(author.Trim()));
    }

    File.WriteAllLines(fileName, arguments);

    Console.WriteLine($"Response file created: {fileName}");


});




rootCommand.Subcommands.Add(createRspCommand);


rootCommand.Subcommands.Add(bundleCommand);

rootCommand.Parse(args).Invoke();