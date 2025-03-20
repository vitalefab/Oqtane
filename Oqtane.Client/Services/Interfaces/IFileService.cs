using Oqtane.Models;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Oqtane.Services
{
    /// <summary>
    /// Service to get / create / upload / download files.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Get all <see cref="File"/>s in the specified Folder
        /// </summary>
        /// <param name="folderId">The folder ID</param>
        /// <returns></returns>
        Task<List<File>> GetFilesAsync(int folderId);

        /// <summary>
        /// Get all <see cref="File"/>s in the specified folder. 
        /// </summary>
        /// <param name="folder">
        /// The folder path relative to where the files are stored.
        /// TODO: todoc verify exactly from where the folder path must start
        /// </param>
        /// <returns></returns>
        Task<List<File>> GetFilesAsync(string folder);

        /// <summary>
        /// Get one <see cref="File"/>
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        Task<File> GetFileAsync(int fileId);

        /// <summary>
        /// Get a <see cref="File"/> based on the <see cref="Folder"/> and file name.
        /// </summary>
        /// <param name="folderId">Reference to the <see cref="Folder"/></param>
        /// <param name="name">name of the file
        /// </param>
        /// <returns></returns>
        Task<File> GetFileAsync(int folderId, string name);

        /// <summary>
        /// Add / store a <see cref="File"/> record.
        /// This does not contain the file contents. 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Task<File> AddFileAsync(File file);

        /// <summary>
        /// Update a <see cref="File"/> record.
        /// Use this for rename a file or change some attributes. 
        /// This does not contain the file contents. 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Task<File> UpdateFileAsync(File file);

        /// <summary>
        /// Delete a <see cref="File"/>
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        Task DeleteFileAsync(int fileId);

        /// <summary>
        /// Upload a file from a URL to a <see cref="Folder"/>
        /// </summary>
        /// <param name="url"></param>
        /// <param name="folderId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<File> UploadFileAsync(string url, int folderId, string name);

        /// <summary>
        /// Get / download a file (the body).
        /// </summary>
        /// <param name="fileId">Reference to a <see cref="File"/></param>
        /// <returns>The bytes of the file</returns>
        Task<byte[]> DownloadFileAsync(int fileId);

        /// <summary>
        /// Retrieve a list of files from a <see cref="Site"/> and <see cref="Folder"/>
        /// </summary>
        /// <param name="siteId">Reference to the <see cref="Site"/></param>
        /// <param name="folderPath">Path of the folder
        /// TODO: todoc verify exactly from where the folder path must start
        /// </param>
        /// <returns></returns>
        Task<List<File>> GetFilesAsync(int siteId, string folderPath);

        /// <summary>
        /// Unzips the contents of a zip file
        /// </summary>
        /// <param name="fileId">Reference to the <see cref="File"/></param>
        /// </param>
        /// <returns></returns>
        Task UnzipFileAsync(int fileId);
    }
}
