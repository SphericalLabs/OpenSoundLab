    using System;
    using System.IO;
    using ICSharpCode.SharpZipLib.GZip;
    using ICSharpCode.SharpZipLib.Tar;
     
    public class Utility_SharpZipCommands {
     
            //// Calling example
            //CreateTarGZ(@"c:\temp\gzip-test.tar.gz", @"c:\data");
     
            //USE THIS:
            public static void CreateTarGZ_FromDirectory(string tgzFilename, string sourceDirectory) {

                string sourceFullPath = Path.GetFullPath(sourceDirectory);

                Stream outStream = File.Create(tgzFilename);
                Stream gzoStream = new GZipOutputStream(outStream);
                TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);

                string normalizedRoot = NormalizePath(sourceFullPath);
                tarArchive.RootPath = normalizedRoot;
                AddDirectoryFilesToTar(tarArchive, sourceFullPath, normalizedRoot, true);

                tarArchive.Close();
            }

            public static void AddDirectoryFilesToTar(TarArchive tarArchive, string sourceDirectory, string rootPath, bool recurse) {

                string directoryName = GetRelativeEntryName(sourceDirectory, rootPath, true);
                if (!string.IsNullOrEmpty(directoryName))
                {
                    TarEntry directoryEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
                    directoryEntry.Name = directoryName;
                    tarArchive.WriteEntry(directoryEntry, false);
                }

                string[] filenames = Directory.GetFiles(sourceDirectory);
                foreach (string filename in filenames) {
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

                if (recurse) {
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
     
        public static void ExtractTGZ(string gzArchiveName, string destFolder) {
            Stream inStream = File.OpenRead (gzArchiveName);
            Stream gzipStream = new GZipInputStream (inStream);
     
            TarArchive tarArchive = TarArchive.CreateInputTarArchive (gzipStream);
            tarArchive.ExtractContents (destFolder);
            tarArchive.Close ();
     
            gzipStream.Close ();
            inStream.Close ();
     
        }
       
    }    // Calling example
