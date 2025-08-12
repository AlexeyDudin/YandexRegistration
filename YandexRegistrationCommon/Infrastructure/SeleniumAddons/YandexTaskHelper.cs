using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using YandexRegistrationModel;

namespace YandexRegistrationCommon.Infrastructure.SeleniumAddons
{
    public static class YandexTaskHelper
    {
        public static string ExtractProfile(this YandexTask task)
        {
            using var zipFileStream = new MemoryStream(task.BrowserUserProfile);
            zipFileStream.Seek(0, SeekOrigin.Begin);
            using var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Read, true, Encoding.UTF8);
            var targetDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Selenium", $"{task.Id}"));
            if (Directory.Exists(targetDirectory.FullName))
                targetDirectory.Delete(true);
            archive.ExtractToDirectory(targetDirectory.FullName, true);
            return targetDirectory.FullName;
        }

        public static void ZipProfile(this YandexTask task)
        {
            using var outputMemoryStream = new MemoryStream();
            string pathToProfile = Path.Combine(Path.GetTempPath(), "Selenium", $"{task.Id}");
            using (var archive = new ZipArchive(outputMemoryStream, ZipArchiveMode.Create, true, Encoding.UTF8))
            {
                archive.AddFolderToZip(pathToProfile);
            }
            outputMemoryStream.Seek(0, SeekOrigin.Begin);
            task.BrowserUserProfile = outputMemoryStream.ToArray();
        }

        private static void AddFolderToZip(this ZipArchive archive, string sourceFolder, string basePath = "")
        {
            // Добавляем все файлы в текущей директории
            foreach (var filePath in Directory.GetFiles(sourceFolder))
            {
                string entryName = Path.Combine(basePath, Path.GetFileName(filePath)).Replace("\\", "/");
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

                using (var entryStream = entry.Open())
                using (var fileStream = File.OpenRead(filePath))
                {
                    fileStream.CopyTo(entryStream);
                }
            }

            // Рекурсивно обрабатываем все вложенные папки
            foreach (var directory in Directory.GetDirectories(sourceFolder))
            {
                string dirName = Path.GetFileName(directory);
                string newEntryRoot = Path.Combine(basePath, dirName).Replace("\\", "/");

                // В zip-архиве папки неявно создаются при добавлении файлов,
                // но если нужна пустая папка, можно добавить пустую запись с '/' в конце:
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                {
                    archive.CreateEntry(newEntryRoot + "/");
                }

                AddFolderToZip(archive, directory, newEntryRoot);
            }
        }
    }
}
