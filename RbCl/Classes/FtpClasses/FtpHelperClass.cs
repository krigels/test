﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using RBClient.Classes;
using CustomLogger;
using RBClient.Classes.CustomClasses;

namespace Classes
{
    public class FtpHelperClass : LoggerBase
    {
        private string uri = null;
        public string UserName = null;
        public string Password = null;


        public FtpHelperClass(string server,string port,string option_path)
        {
            ArCh.Check(server,"server");
            uri="ftp://"+server;
            if(!ArCh.isn(port))
            {
                uri+=":"+port;
            }

            if(!ArCh.isn(option_path))
            {
                uri+=option_path;
            }
        }


        /// <summary>
        /// Проверяет существует ли файл на фтп сервере
        /// </summary>
        /// <param name="server">сервер</param>
        /// <param name="port">порт</param>
        /// <param name="file_name">имя файла</param>
        /// <param name="option_path">опционально: внутренние папки на фтп сервере</param>
        /// <param name="comp">опции поиска файла по имени</param>
        /// <returns></returns>
        public static bool CheckIfFileExist(string server, string port,ref string file_name,string userName,
            string password,string option_path,StringComparison comp)
        {
            #region parameter checking
            ArCh.Check(server,"server");ArCh.Check(file_name,"file_name");
            #endregion
            
            FtpHelperClass ftp = new FtpHelperClass(server, port, option_path);
            
            ftp.UserName = userName; ftp.Password = password;

            List<string> file_list = new List<string>();
            ftp.Log("ftp file list "+Serializer.JsonSerialize(file_list));
            var exp = ftp.GetFileList(ref file_list);
            if (exp != null) throw exp;
            string fname = file_name;
            string file = file_list.WhereFirst(a => (comp == null) ? a.Equals(fname) : a.Equals(fname, comp));
            if (file != null)
            {
                file_name = file;
                return true;
            }
            return false;
        }

        
        public static bool CheckIfFileExist(string server, string port,ref string file_name,string userName,
            string password,string option_path,StringComparison comp,Action<string> logevent)
        {
            #region parameter checking
            ArCh.Check(server,"server");ArCh.Check(file_name,"file_name");
            #endregion

            FtpHelperClass ftp = new FtpHelperClass(server, port, option_path);
            ftp.LogEvent = logevent;
            ftp.UserName = userName; ftp.Password = password;
            ftp.Log("CheckIfFileExist params is" + Serializer.JsonSerialize(new string[6] { server, port, file_name, userName, password, option_path }));

            List<string> file_list = new List<string>();
            var exp = ftp.GetFileList(ref file_list);
            ftp.Log("ftp file list " + Serializer.JsonSerialize(file_list));

            if (exp != null) throw exp;
            string fname = file_name;
            string file = file_list.WhereFirst(a => (comp == null) ? a.Equals(fname) : a.Equals(fname, comp));
            if (file != null)
            {
                file_name = file;
                return true;
            }
            return false;
        }

        private Exception GetFileList(ref List<string> list)
        {
            var temp = new List<string>();
            Exception ex = null;

            TimeOutAction tma = new TimeOutAction(o => {
                var k = (List<string>)o;
                var kk = _GetFileList(ref k);
                temp = k;
                ex = kk;
            }, list);
            tma.Timeout = 120000;
            tma.Tries = 1;
            tma.Start();

            if (tma.State == StateEnum.Error)
            {
                ex = new Exception("Ftp timeout is ended");
            }
            list = temp;
            return ex;
        }

        private Exception _GetFileList(ref List<string> list)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Proxy = GlobalProxySelection.GetEmptyWebProxy();

                ArCh.isne(UserName, "UserName"); ArCh.isne(Password, "Password");
                request.Credentials = new NetworkCredential(UserName, Password);

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        string str = reader.ReadToEnd();
                        list = str.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Log(ex, "GetFileList error");
                return ex;
            }
        }

        public static bool SendFileOnServer(string server, string port, FileInfo file_name, string userName,
            string password, string option_path)
        {
            try
            {
                #region parameter checking
                ArCh.Check(server, "server"); ArCh.Check(file_name, "file_name");
                #endregion

                FtpHelperClass ftp = new FtpHelperClass(server, port, option_path);
                ftp.UserName = userName; ftp.Password = password;

                ftp.UploadFile(file_name);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void UploadFile(FileInfo file_name)
        {
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri+"/"+file_name.Name);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            ArCh.isne(UserName, "UserName"); ArCh.isne(Password, "Password");
            request.Credentials = new NetworkCredential(UserName, Password);

            // Copy the contents of the file to the request stream.
            StreamReader sourceStream = new StreamReader(file_name.FullName);
            byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            sourceStream.Close();
            request.ContentLength = fileContents.Length;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            response.Close();
        }

    }
}
