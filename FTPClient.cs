using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace DigiHash
{
    public class FtpClient
    {
        private string _host;
        private string _username;
        private string _password;
        private bool _enableSsl;
        private string _welcomeMessage;
        private string _bannerMessage;
        private DirectoryEntryInfo _currentDirectory;

        public FtpClient(string host) : this(host, null, null) { }
        public FtpClient(string host, string username, string password)
        {
            this._host = host;
            this._username = username;
            this._password = password;
            this.EnableSsl = false;
            this.KeepAlive = true;
            this._currentDirectory = new DirectoryEntryInfo("", new DateTime(), null, this._host);
        }

        public string Host
        {
            get { return this._host; }
        }

        public string Username
        {
            get { return this._username; }
        }

        public bool EnableSsl
        {
            get { return this._enableSsl; }
            set
            {
                this._enableSsl = value;

                if (this._enableSsl)
                    this.Port = 990;
                else
                    this.Port = 21;
            }
        }

        public string WelcomeMessage
        {
            get { return this._welcomeMessage; }
        }

        public string BannerMessage
        {
            get { return this._bannerMessage; }
        }

        public bool UseBinary { get; set; }
        public bool UsePassive { get; set; }
        public int Port { get; set; }
        public bool KeepAlive { get; set; }

        private void Copy(Stream source, Stream destination)
        {
            if (source.CanSeek)
                source.Position = 0;

            int len;
            do
            {
                // Read from the file stream 2kb at a time
                byte[] buffer = new byte[2048];
                len = source.Read(buffer, 0, buffer.Length);

                // Write Content from source to the destination
                if (len > 0)
                    destination.Write(buffer, 0, len);

            } while (len != 0);// Till Stream content ends

            //Reset position
            if (destination.CanSeek)
                destination.Position = 0;
        }

        public void Upload(string localFile, DirectoryEntryInfo remoteDirectory)
        {
            this.Upload(localFile, remoteDirectory.FullName);
        }

        public void Upload(string localFile, string remoteDirectory)
        {
            this.Upload(localFile, remoteDirectory, Encoding.UTF8);
        }

        public void Upload(string[] localFiles, DirectoryEntryInfo remoteDirectory)
        {
            this.Upload(localFiles, remoteDirectory.FullName);
        }

        public void Upload(string[] localFiles, string remoteDirectory)
        {
            this.Upload(localFiles, remoteDirectory, Encoding.UTF8);
        }

        public void Upload(string[] localFiles, DirectoryEntryInfo remoteDirectory, Encoding encoding)
        {
            this.Upload(localFiles, remoteDirectory.FullName, encoding);
        }

        public void Upload(string[] localFiles, string remoteDirectory, Encoding encoding)
        {
            foreach (string localFile in localFiles)
                this.Upload(localFile, remoteDirectory, encoding);
        }

        public void Upload(string localFile, DirectoryEntryInfo remoteDirectory, Encoding encoding)
        {
            this.Upload(localFile, remoteDirectory.FullName, encoding);
        }

        public void Upload(string localFile, string remoteDirectory, Encoding encoding)
        {
            using (FileStream stream = new FileStream(localFile, FileMode.Open))
            {
                FileInfo file = new FileInfo(stream.Name);

                string filename = this.GetValidFile(remoteDirectory, file.Name);

                this.Upload(stream, filename);

                stream.Close();
            }
        }

        public void Upload(Stream stream, string remoteFile)
        {
            FtpWebRequest request = this.CreateWebRequest(WebRequestMethods.Ftp.UploadFile, this.GetValidFile(this._host, remoteFile));

            //get ftp response
            FtpWebResponse response = this.GetResponse(request);

            if (stream.CanSeek)
                request.ContentLength = stream.Length;

            //Upload data
            using (Stream remoteStream = request.GetRequestStream())
            {
                //Copy stream to remtoe stream
                this.Copy(stream, remoteStream);

                //Close stream
                remoteStream.Close();
            }

            //Close connection
            response.Close();
            request.Abort();
        }

        public void Upload(byte[] contents, string remoteFile)
        {
            FtpWebRequest request = this.CreateWebRequest(WebRequestMethods.Ftp.UploadFile, this.GetValidFile(this._host, remoteFile));

            //get ftp response
            FtpWebResponse response = this.GetResponse(request);

            //Upload data
            request.ContentLength = contents.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(contents, 0, contents.Length);
                stream.Close();
            }

            //Close connection
            response.Close();
            request.Abort();
        }

        public FileStream DownloadFile(string localFile, FileEntryInfo remoteFile)
        {
            return this.DownloadFile(localFile, remoteFile.FullName);
        }

        public FileStream DownloadFile(string localFile, string remoteFile)
        {
            FileStream fileStream = null;

            using (Stream stream = this.DownloadStream(remoteFile))
            {
                //create local file stream                    
                fileStream = new FileStream(localFile, FileMode.Create);

                //Copy to file stream
                this.Copy(stream, fileStream);

                stream.Close();
            }

            return fileStream;
        }

        public Stream[] DownloadStreams(FileEntryInfo[] remoteFiles)
        {
            List<Stream> streams = new List<Stream>();

            foreach (FileEntryInfo remoteFile in remoteFiles)
            {
                Stream stream = this.DownloadStream(remoteFile);
                streams.Add(stream);
            }

            return streams.ToArray();
        }

        public Stream DownloadStream(FileEntryInfo remoteFile)
        {
            return this.DownloadStream(remoteFile.FullName);
        }

        public Stream[] DownloadStreams(string[] remoteFiles)
        {
            List<Stream> streams = new List<Stream>();

            foreach (string remoteFile in remoteFiles)
            {
                Stream stream = this.DownloadStream(remoteFile);
                streams.Add(stream);
            }

            return streams.ToArray();
        }

        public Stream DownloadStream(string remoteFile)
        {
            //create ftp web request
            FtpWebRequest request = this.CreateWebRequest(WebRequestMethods.Ftp.DownloadFile, this.GetValidFile(this._host, remoteFile));

            //get ftp response
            FtpWebResponse response = this.GetResponse(request);

            MemoryStream memoryStream = new MemoryStream();
            using (Stream stream = response.GetResponseStream())
            {
                this.Copy(stream, memoryStream);

                stream.Close();
            }

            //Close connection
            response.Close();
            request.Abort();

            return memoryStream;
        }

        public void DeleteFiles(FileEntryInfo[] remoteFiles)
        {
            foreach (FileEntryInfo remoteFile in remoteFiles)
                this.DeleteFile(remoteFile);

        }

        public void DeleteFile(FileEntryInfo remoteFile)
        {
            this.DeleteFile(remoteFile.FullName);
        }

        public void DeleteFiles(string[] remoteFiles)
        {
            foreach (string remoteFile in remoteFiles)
                this.DeleteFile(remoteFile);

        }

        public void DeleteFile(string remoteFile)
        {
            //create ftp web request
            FtpWebRequest request = this.CreateWebRequest(WebRequestMethods.Ftp.DeleteFile, this.GetValidFile(this._host, remoteFile));

            //get ftp response
            FtpWebResponse response = this.GetResponse(request);

            response.Close();
            request.Abort();
        }

        public void RenameFile(FileEntryInfo remoteFile, string newName)
        {
            this.RenameFile(remoteFile.FullName, newName);
        }

        public void RenameFile(string remoteFile, string newName)
        {
            //create ftp web request
            FtpWebRequest request = this.CreateWebRequest(WebRequestMethods.Ftp.Rename, this.GetValidFile(this._host, remoteFile));
            request.RenameTo = newName;

            //get ftp response
            FtpWebResponse response = this.GetResponse(request);

            response.Close();
            request.Abort();
        }

        public void RenameDirectory(DirectoryEntryInfo remoteDirectory, string newName)
        {
            this.RenameDirectory(remoteDirectory.FullName, newName);
        }

        public void RenameDirectory(string remoteDirectory, string newName)
        {
            //create ftp web request
            FtpWebRequest request = this.CreateWebRequest(WebRequestMethods.Ftp.Rename, this.GetValidFile(this._host, remoteDirectory));
            request.RenameTo = newName;

            //get ftp response
            FtpWebResponse response = this.GetResponse(request);

            response.Close();
            request.Abort();
        }

        public void MakeDirectories(string[] remoteDirectories)
        {
            foreach (string remoteDirectory in remoteDirectories)
                this.MakeDirectory(remoteDirectory);
        }

        public void MakeDirectory(string remoteDirectory)
        {
            //create ftp web request
            FtpWebRequest request = this.CreateWebRequest(WebRequestMethods.Ftp.MakeDirectory, this.GetValidPath(this._host, remoteDirectory));

            //get ftp response
            FtpWebResponse response = this.GetResponse(request);

            response.Close();
            request.Abort();
        }

        public void DeleteDirectories(DirectoryEntryInfo[] remoteDirectories)
        {
            foreach (DirectoryEntryInfo remoteDirectory in remoteDirectories)
                this.DeleteDirectory(remoteDirectory);
        }

        public void DeleteDirectory(DirectoryEntryInfo remoteDirectory)
        {
            this.DeleteDirectory(remoteDirectory.FullName);
        }

        public void DeleteDirectories(string[] remoteDirectories)
        {
            foreach (string remoteDirectory in remoteDirectories)
                this.DeleteDirectory(remoteDirectory);
        }

        public void DeleteDirectory(string remoteDirectory)
        {
            //create ftp web request
            FtpWebRequest request = this.CreateWebRequest(WebRequestMethods.Ftp.RemoveDirectory, this.GetValidPath(this._host, remoteDirectory));

            //get ftp response
            FtpWebResponse response = this.GetResponse(request);

            response.Close();
            request.Abort();
        }

        public void Move(string remoteFile, string remoteDirectory)
        {
            //Download file as stream first 
            using (Stream stream = this.DownloadStream(remoteFile))
            {

                //Upload to directory
                string file = this.GetValidFile(remoteDirectory, this.GetFileName(remoteFile));
                this.Upload(stream, file);

                stream.Close();
            }

            //Delete file
            this.DeleteFile(remoteFile);
        }

        private string GetFileName(string remoteFile)
        {
            remoteFile = remoteFile.Replace("//", "/");

            string[] files = remoteFile.Split('/');

            return files[files.Length - 1];
        }

        public EntryInfoBase[] ListRepositories()
        {
            return this.ListRepositories(null);
        }

        public EntryInfoBase[] ListRepositories(string path)
        {
            List<EntryInfoBase> entries = new List<EntryInfoBase>();

            //create ftp web request
            FtpWebRequest request = this.CreateWebRequest(WebRequestMethods.Ftp.ListDirectoryDetails, this.GetValidPath(this._host, path));

            //get ftp response
            FtpWebResponse response = this.GetResponse(request);

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                //add all file path
                while (!reader.EndOfStream)
                {
                    EntryInfoBase entry = EntryInfoBase.Parse(reader.ReadLine(), path);
                    entries.Add(entry);
                }
            }

            //Close connection
            response.Close();
            request.Abort();

            return entries.ToArray();
        }

        private FtpWebResponse GetResponse(FtpWebRequest request)
        {
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            this._welcomeMessage = response.WelcomeMessage;
            this._bannerMessage = response.BannerMessage;

            return response;
        }

        private FtpWebRequest CreateWebRequest(string method, string uri)
        {
            UriBuilder uriBuilder = new UriBuilder("ftp://" + uri);
            uriBuilder.Port = this.Port;

            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(uriBuilder.Uri);

            //set action
            request.Method = method;
            //use username and password login to ftp
            if (this._username != null && this._password != null)
                request.Credentials = new NetworkCredential(this._username, this._password);

            //Initialize
            request.UseBinary = this.UseBinary;
            request.UsePassive = this.UsePassive;
            request.EnableSsl = this.EnableSsl;
            request.KeepAlive = this.KeepAlive;

            return request;
        }

        private string GetValidFile(string path, string file)
        {
            return string.Format("{0}/{1}", path, file);
        }

        private string GetValidPath(string hostname, string path)
        {
            string validPath = hostname;

            if (path != null)
            {
                path = path.Replace('\\', '/');

                if (!(path.EndsWith("/")))
                    path += "/";

                validPath = string.Format("{0}/{1}", hostname, path);
            }

            return validPath;
        }

        public abstract class EntryInfoBase
        {
            private string _name;
            private DateTime _time;
            private string _permission;
            private string _parentPath;

            /// List of REGEX formats for different FTP server listing formats
            /// The first three are various UNIX/LINUX formats, fourth is for MS FTP
            /// in detailed mode and the last for MS FTP in 'DOS' mode.
            private static string[] _parseFormats;

            static EntryInfoBase()
            {
                EntryInfoBase._parseFormats = new string[] 
                { 
                    "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\w+\\s+\\w+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{4})\\s+(?<name>.+)", 
                    "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\d+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{4})\\s+(?<name>.+)", 
                    "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\d+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{1,2}:\\d{2})\\s+(?<name>.+)", 
                    "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\w+\\s+\\w+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{1,2}:\\d{2})\\s+(?<name>.+)", 
                    "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})(\\s+)(?<size>(\\d+))(\\s+)(?<ctbit>(\\w+\\s\\w+))(\\s+)(?<size2>(\\d+))\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{2}:\\d{2})\\s+(?<name>.+)", 
                    "(?<timestamp>\\d{2}\\-\\d{2}\\-\\d{2}\\s+\\d{2}:\\d{2}[Aa|Pp][mM])\\s+(?<dir>\\<\\w+\\>){0,1}(?<size>\\d+){0,1}\\s+(?<name>.+)" 
                };
            }

            protected EntryInfoBase(string name, DateTime time, string permission, string parentPath)
            {
                this._name = name;
                this._time = time;
                this._permission = permission;
                this._parentPath = parentPath;
            }

            public string Name
            {
                get { return this._name; }
            }

            public DateTime Time
            {
                get { return this._time; }
            }

            public string Permission
            {
                get { return this._permission; }
            }

            public string ParentPath
            {
                get { return this._parentPath; }
            }

            public string FullName
            {
                get
                {
                    if (string.IsNullOrEmpty(this._parentPath))
                        return this._name;
                    else
                        return string.Format("{0}/{1}", this._parentPath, this._name);
                }
            }

            internal static EntryInfoBase Parse(string info, string parentPath)
            {
                EntryInfoBase entry = null;

                //parse line
                Match match = EntryInfoBase.GetMatch(info);
                if (match == null)//failed
                    throw new ArgumentException(string.Format("Parsing error:{0}", info));
                else
                {
                    //Parsing attribute 
                    string name = match.Groups["name"].Value;
                    string permission = match.Groups["permission"].Value;

                    //Parsing time
                    DateTime time;
                    try
                    {
                        time = DateTime.Parse(match.Groups["timestamp"].Value);
                    }
                    catch (Exception)
                    {
                        time = new DateTime();
                    }

                    //Check is file or directory
                    string dir = match.Groups["dir"].Value;
                    if (!string.IsNullOrEmpty(dir) && dir != "-")
                        entry = new DirectoryEntryInfo(name, time, permission, parentPath);
                    else
                    {
                        long size = 0;
                        try
                        {
                            size = Convert.ToInt64(match.Groups["size"].Value);
                        }
                        catch(Exception)
                        {

                        }

                        //Parsing extension
                        string extension = null;
                        int i = name.LastIndexOf(".");
                        if (i >= 0 && i < name.Length - 1)
                            extension = name.Substring(i + 1);

                        entry = new FileEntryInfo(name, time, size, extension, permission, parentPath);
                    }

                }

                return entry;
            }

            private static Match GetMatch(string info)
            {
                Match match = null;

                foreach (string format in EntryInfoBase._parseFormats)
                {
                    Regex regex = new Regex(format);
                    match = regex.Match(info);
                    if (match.Success)
                        break;
                }

                return match;
            }
        }

        public class FileEntryInfo : EntryInfoBase
        {
            private long _size;
            private string _extension;

            internal FileEntryInfo(string name, DateTime time, long size, string extension, string permission, string parentPath)
                : base(name, time, permission, parentPath)
            {
                this._size = size;
                this._extension = extension;
            }

            public long Size
            {
                get { return this._size; }
            }

            public string Extension
            {
                get { return this._extension; }
            }

        }

        public class DirectoryEntryInfo : EntryInfoBase
        {
            internal DirectoryEntryInfo(string name, DateTime time, string permission, string parentPath)
                : base(name, time, permission, parentPath)
            {
            }
        }

    }

}