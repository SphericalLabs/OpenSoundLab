using System;
using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

public static class Utility_SharpZipCommands
{
    //// Calling example
    //CreateTarGZ(@"c:\temp\gzip-test.tar.gz", @"c:\data");

    //USE THIS:
    public static void CreateTarGZ_FromDirectory(string tgzFilename, string sourceDirectory)
    {
        string sourceFullPath = Path.GetFullPath(sourceDirectory);

        using (Stream outStream = File.Create(tgzFilename))
        using (Stream gzoStream = new GZipOutputStream(outStream))
        using (TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzoStream))
        {
            string normalizedRoot = NormalizePath(sourceFullPath);
            tarArchive.RootPath = normalizedRoot;
            AddDirectoryFilesToTar(tarArchive, sourceFullPath, normalizedRoot, true);
        }
    }

    public static void AddDirectoryFilesToTar(TarArchive tarArchive, string sourceDirectory, string rootPath, bool recurse)
    {
        string directoryName = GetRelativeEntryName(sourceDirectory, rootPath, true);
        if (!string.IsNullOrEmpty(directoryName))
        {
            TarEntry directoryEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
            directoryEntry.Name = directoryName;
            tarArchive.WriteEntry(directoryEntry, false);
        }

        string[] filenames = Directory.GetFiles(sourceDirectory);
        foreach (string filename in filenames)
        {
            string nameOnly = Path.GetFileName(filename);
            if (nameOnly.StartsWith("."))
            {
                continue;
            }

            if (filename.EndsWith(".meta"))
            {
                continue;
            }

            string fileName = GetRelativeEntryName(filename, rootPath, false);
            if (string.IsNullOrEmpty(fileName))
            {
                continue;
            }

            TarEntry fileEntry = TarEntry.CreateEntryFromFile(filename);
            fileEntry.Name = fileName;
            tarArchive.WriteEntry(fileEntry, true);
        }

        if (recurse)
        {
            string[] directories = Directory.GetDirectories(sourceDirectory);
            foreach (string directory in directories)
            {
                string dirNameOnly = Path.GetFileName(directory);
                if (dirNameOnly.StartsWith("."))
                {
                    continue;
                }

                AddDirectoryFilesToTar(tarArchive, directory, rootPath, recurse);
            }
        }
    }

    static string GetRelativeEntryName(string path, string rootPath, bool isDirectory)
    {
        string normalizedPath = NormalizePath(path);
        string trimmed = normalizedPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)
            ? normalizedPath.Substring(rootPath.Length).TrimStart('/')
            : normalizedPath;

        if (string.IsNullOrEmpty(trimmed))
        {
            return string.Empty;
        }

        return isDirectory ? trimmed + "/" : trimmed;
    }

    static string NormalizePath(string path)
    {
        string normalized = path.Replace('\\', '/');
        if (normalized.EndsWith("/"))
        {
            normalized = normalized.Substring(0, normalized.Length - 1);
        }
        return normalized;
    }

    public static void ExtractTGZ(string gzArchiveName, string destFolder, Action<long, long> progressCallback = null)
    {
        if (string.IsNullOrEmpty(gzArchiveName))
        {
            throw new ArgumentException("Archive path must be provided.", nameof(gzArchiveName));
        }

        if (string.IsNullOrEmpty(destFolder))
        {
            throw new ArgumentException("Destination folder must be provided.", nameof(destFolder));
        }

        long totalBytes = CalculateTotalBytes(gzArchiveName);
        long processedBytes = 0;
        byte[] buffer = new byte[128 * 1024];

        using (Stream inStream = File.OpenRead(gzArchiveName))
        using (Stream gzipStream = new GZipInputStream(inStream))
        using (TarInputStream tarInput = new TarInputStream(gzipStream))
        {
            TarEntry entry;
            while ((entry = tarInput.GetNextEntry()) != null)
            {
                string entryPath = Path.Combine(destFolder, entry.Name.Replace('/', Path.DirectorySeparatorChar));

                if (entry.IsDirectory)
                {
                    Directory.CreateDirectory(entryPath);
                    continue;
                }

                string directoryName = Path.GetDirectoryName(entryPath);
                if (!string.IsNullOrEmpty(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                long remaining = entry.Size;
                using (FileStream outStream = File.Create(entryPath))
                {
                    while (remaining > 0)
                    {
                        int toRead = remaining > buffer.Length ? buffer.Length : (int)remaining;
                        int read = tarInput.Read(buffer, 0, toRead);
                        if (read <= 0)
                        {
                            break;
                        }

                        outStream.Write(buffer, 0, read);
                        remaining -= read;
                        processedBytes += read;
                        progressCallback?.Invoke(processedBytes, totalBytes);
                    }
                }

                try
                {
                    File.SetLastWriteTimeUtc(entryPath, entry.ModTime.ToUniversalTime());
                }
                catch (IOException)
                {
                    // Ignore timestamp failures; not critical for extraction.
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Ignore invalid timestamps in the archive.
                }
            }
        }

        progressCallback?.Invoke(totalBytes, totalBytes);
    }

    static long CalculateTotalBytes(string gzArchiveName)
    {
        long total = 0;

        using (Stream inStream = File.OpenRead(gzArchiveName))
        using (Stream gzipStream = new GZipInputStream(inStream))
        using (TarInputStream tarInput = new TarInputStream(gzipStream))
        {
            TarEntry entry;
            while ((entry = tarInput.GetNextEntry()) != null)
            {
                if (!entry.IsDirectory)
                {
                    total += entry.Size;
                }
            }
        }

        return total;
    }
}
