using System;
using System.IO;
using Ionic.Zip; // Ensure you have referenced the DotNetZip DLL

namespace Antivirus
{
    public class ZipExtractor
    {
        /// <summary>
        /// Extracts a ZIP file to the specified output directory.
        /// </summary>
        /// <param name="zipFilePath">The path to the ZIP file.</param>
        /// <param name="outputDirectory">The directory to extract the files to.</param>
        /// <param name="password">The password for the ZIP file, if it is password-protected. Pass null if not password-protected.</param>
        public static void ExtractZip(string zipFilePath, string outputDirectory, string password = null)
        {
            if (string.IsNullOrEmpty(zipFilePath))
                throw new ArgumentException("ZIP file path cannot be null or empty.", nameof(zipFilePath));

            if (string.IsNullOrEmpty(outputDirectory))
                throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

            if (!File.Exists(zipFilePath))
                throw new FileNotFoundException("The specified ZIP file does not exist.", zipFilePath);

            try
            {
                using (ZipFile zip = ZipFile.Read(zipFilePath))
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        zip.Password = password;
                    }

                    foreach (ZipEntry entry in zip)
                    {
                        entry.Extract(outputDirectory, ExtractExistingFileAction.OverwriteSilently);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle exceptions as needed
                throw new ApplicationException("An error occurred while extracting the ZIP file.", ex);
            }
        }
    }
}