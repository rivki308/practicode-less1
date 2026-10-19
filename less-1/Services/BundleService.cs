
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace פרקטיקוד.Services;

public class BundleService
{
    public void CreateBundle(
        string sourceDirectory,
        string outputPath,
        string[] extensions,
        bool includeSourceNotes,
        bool removeEmptyLines,
        string sortBy,
        string? author)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException(
                "Output path must be provided.",
                nameof(outputPath));
        }


        if (extensions == null || extensions.Length == 0)
        {
            throw new ArgumentException(
                "At least one file extension must be provided.",
                nameof(extensions));
        }

        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Source directory was not found: {sourceDirectory}");
        }


        string fullOutputPath = Path.GetFullPath(outputPath);

        IEnumerable<string> files =
            GetSourceFiles(sourceDirectory, extensions)
                .Where(file =>
                    !Path.GetFullPath(file).Equals(
                        fullOutputPath,
                        StringComparison.OrdinalIgnoreCase));



        if (!files.Any())
        {
            throw new InvalidOperationException(
                "No matching source files were found.");
        }

        IEnumerable<string> sortedFiles;

        if (sortBy.Equals("type", StringComparison.OrdinalIgnoreCase))
        {
            sortedFiles = files
                .OrderBy(file => Path.GetExtension(file),
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(file => Path.GetFileName(file),
                    StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            sortedFiles = files
                .OrderBy(file => Path.GetFileName(file),
                    StringComparer.OrdinalIgnoreCase);
        }
        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            if (!string.IsNullOrWhiteSpace(author))
            {
                writer.WriteLine($"// Author: {author}");
                writer.WriteLine();
            }
            foreach (string file in sortedFiles)
            {

                if (includeSourceNotes)
                {
                    string relativePath =
                        Path.GetRelativePath(sourceDirectory, file);

                    writer.WriteLine($"// ===== Source: {relativePath} =====");
                }

                string[] lines = File.ReadAllLines(file);

                if (removeEmptyLines)
                {
                    lines = lines
                        .Where(line => !string.IsNullOrWhiteSpace(line))
                        .ToArray();
                }

                foreach (string line in lines)
                {
                    writer.WriteLine(line);
                }

                writer.WriteLine();
            }
        }


    }



    private static readonly HashSet<string> SupportedCodeExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
        ".cs",
        ".js",
        ".jsx",
        ".ts",
        ".tsx",
        ".py",
        ".java",
        ".c",
        ".cpp",
        ".h",
        ".hpp",
        ".html",
        ".css",
        ".sql"
        };

    private static readonly HashSet<string> ExcludedDirectories =
        new(StringComparer.OrdinalIgnoreCase)
        {
        "bin",
        "obj",
        "debug",
        "release",
        ".git",
        ".vs"
        };


    private static IEnumerable<string> GetSourceFiles(
        string sourceDirectory,
        string[] extensions)
    {
        bool includeAll = extensions.Any(e =>
            e.Equals("all", StringComparison.OrdinalIgnoreCase) ||
            e.Equals(".all", StringComparison.OrdinalIgnoreCase));

        HashSet<string> wantedExtensions = new(
            extensions
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.StartsWith(".") ? e : "." + e),
            StringComparer.OrdinalIgnoreCase);

        return Directory
            .EnumerateFiles(
                sourceDirectory,
                "*",
                SearchOption.AllDirectories)
            .Where(file =>
            {
                string relativePath =
                    Path.GetRelativePath(sourceDirectory, file);

                string[] parts = relativePath.Split(
                    new[]
                    {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                    },
                    StringSplitOptions.RemoveEmptyEntries);

                bool isExcluded = parts
                    .SkipLast(1)
                    .Any(part => ExcludedDirectories.Contains(part));

                string extension = Path.GetExtension(file);

                bool hasWantedExtension = includeAll
                    ? SupportedCodeExtensions.Contains(extension)
                    : wantedExtensions.Contains(extension);

                return !isExcluded && hasWantedExtension;
            });
    }

}
