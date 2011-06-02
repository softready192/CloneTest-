using System;
using System.IO;
using System.IO.Packaging;
namespace DA.DAIE.Common
{
    public class ZipUtil
    {

        private const long BUFFER_SIZE = 4096;
        public static void AddFileToZip(string zipFilename, string fileToAdd, string workingFolder, string zipFolder)
        {
            using (Package zip = System.IO.Packaging.Package.Open(workingFolder + zipFilename, FileMode.OpenOrCreate ))
            {
                string fileToAddEscaped = fileToAdd.Replace(' ', '_');
                string destFilename = zipFolder + Path.GetFileName(fileToAddEscaped);
               Uri uri = new Uri(destFilename, UriKind.Relative);
                if (zip.PartExists(uri)) { zip.DeletePart(uri); }
                PackagePart part = zip.CreatePart(uri, "", CompressionOption.Normal);

                try
                {
                    using (FileStream fileStream = new FileStream(workingFolder + fileToAdd, FileMode.Open, FileAccess.Read))
                    {
                        using (Stream dest = part.GetStream())
                        { CopyStream(fileStream, dest); }
                    }
                }
                catch (Exception ex)
                {
                    DA.Common.HandledException he = new DA.Common.HandledException("Failed Adding To Zip.  File: " + fileToAdd + " \n\r Error: " + ex.Message );
                    he.logOnly();
                }
            }
        }

        private static void CopyStream(System.IO.FileStream inputStream, System.IO.Stream outputStream)
        {
            long bufferSize = inputStream.Length < BUFFER_SIZE ? inputStream.Length : BUFFER_SIZE; byte[] buffer = new byte[bufferSize];
            int bytesRead = 0; long bytesWritten = 0;
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) != 0)
            { outputStream.Write(buffer, 0, bytesRead); bytesWritten += bufferSize; }
        }
    }
}
