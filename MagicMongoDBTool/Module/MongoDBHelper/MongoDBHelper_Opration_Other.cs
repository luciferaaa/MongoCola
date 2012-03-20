﻿using System;
using System.IO;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace MagicMongoDBTool.Module
{
    public static partial class MongoDBHelper
    {
        #region"GFS操作"

        /// <summary>
        /// Save And Open String As File
        /// </summary>
        /// <param name="strJson"></param>
        public static void SaveAndOpenStringAsFile(String strJson)
        {
            if (!Directory.Exists(TempFileFolder))
            {
                Directory.CreateDirectory(TempFileFolder);
            }
            String LocalFileName = TempFileFolder + System.IO.Path.DirectorySeparatorChar + "JsonData.txt";
            StreamWriter t = new StreamWriter(LocalFileName, false);
            t.Write(strJson);
            t.Close();
            System.Diagnostics.Process.Start(LocalFileName);
        }


        ///在使用GirdFileSystem的时候，请注意：
        ///1.Windows 系统的文件名不区分大小写，不过，filename一定是区分大小写的，如果大小写不匹配的话，会发生无法找到文件的问题
        ///2.Download的时候，不能使用SlaveOk选项！

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="strFileName"></param>
        public static void OpenFile(String strRemoteFileName)
        {
            MongoDatabase mongoDB = SystemManager.GetCurrentDataBase();
            MongoGridFS gfs = mongoDB.GetGridFS(new MongoGridFSSettings());

            String[] strLocalFileName = strRemoteFileName.Split(System.IO.Path.DirectorySeparatorChar);

            try
            {
                if (!Directory.Exists(TempFileFolder))
                {
                    Directory.CreateDirectory(TempFileFolder);
                }
                String LocalFileName = TempFileFolder + System.IO.Path.DirectorySeparatorChar + strLocalFileName[strLocalFileName.Length - 1];
                gfs.Download(LocalFileName, strRemoteFileName);
                System.Diagnostics.Process.Start(LocalFileName);
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="strFileName"></param>
        public static void DownloadFile(String strLocalFileName, String strRemoteFileName)
        {
            MongoDatabase mongoDB = SystemManager.GetCurrentDataBase();
            MongoGridFS gfs = mongoDB.GetGridFS(new MongoGridFSSettings());
            gfs.Download(strLocalFileName, strRemoteFileName);
        }
        public enum enumGFSFileName
        {
            filename,
            path
        }
        public enum enumGFSAlready
        {
            JustAddIt,
            RenameIt,
            SkipIt,
            OverwriteIt,
            Stop
        }
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <remarks>Mongo允许同名文件，因为id才是主键</remarks>
        /// <param name="strFileName"></param>
        public static bool UpLoadFile(String strFileName, enumGFSFileName FileName, enumGFSAlready Option)
        {
            MongoDatabase mongoDB = SystemManager.GetCurrentDataBase();
            MongoGridFS gfs = mongoDB.GetGridFS(new MongoGridFSSettings());
            String RemoteName = String.Empty;
            if (FileName == enumGFSFileName.filename)
            {
                RemoteName = new FileInfo(strFileName).Name;
            }
            else
            {
                RemoteName = strFileName;
            }
            if (!gfs.Exists(RemoteName))
            {
                gfs.Upload(strFileName, RemoteName);
                return true;
            }
            else
            {
                switch (Option)
                {
                    case enumGFSAlready.JustAddIt:
                        gfs.Upload(strFileName, RemoteName);
                        return true;
                    case enumGFSAlready.RenameIt:
                        String ExtendName = new FileInfo(strFileName).Extension;
                        String MainName = RemoteName.Substring(0, RemoteName.Length - ExtendName.Length);
                        int i = 1;
                        while (gfs.Exists(MainName + i.ToString() + ExtendName))
                        {
                            i++;
                        }
                        gfs.Upload(strFileName, MainName + i.ToString() + ExtendName);
                        return true;
                    case enumGFSAlready.SkipIt:
                        return false;
                    case enumGFSAlready.OverwriteIt:
                        gfs.Delete(RemoteName);
                        gfs.Upload(strFileName, RemoteName);
                        return true;
                    case enumGFSAlready.Stop:
                        return false;
                }
                return true;
            }
        }
        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="strFileName"></param>
        public static void DelFile(String strFileName)
        {
            MongoDatabase mongoDB = SystemManager.GetCurrentDataBase();
            MongoGridFS gfs = mongoDB.GetGridFS(new MongoGridFSSettings());
            gfs.Delete(strFileName);
        }
        /// <summary>
        /// GFS初始化
        /// </summary>
        public static void InitGFS()
        {
            MongoDatabase mongoDB = SystemManager.GetCurrentDataBase();
            if (!mongoDB.CollectionExists(COLLECTION_NAME_GFS_FILES))
            {
                mongoDB.CreateCollection(COLLECTION_NAME_GFS_FILES);
            }
        }
        /// <summary>
        /// 数据库User初始化
        /// </summary>
        public static void InitDBUser()
        {
            MongoDatabase mongoDB = SystemManager.GetCurrentDataBase();
            if (!mongoDB.CollectionExists(COLLECTION_NAME_USER))
            {
                mongoDB.CreateCollection(COLLECTION_NAME_USER);
            }
        }
        /// <summary>
        /// Js数据集初始化
        /// </summary>
        public static void InitJavascript()
        {
            MongoDatabase mongoDB = SystemManager.GetCurrentDataBase();
            if (!mongoDB.CollectionExists(COLLECTION_NAME_JAVASCRIPT))
            {
                mongoDB.CreateCollection(COLLECTION_NAME_JAVASCRIPT);
            }
        }
        #endregion

        #region"用户操作"
        /// <summary>
        /// Add A User to Admin database
        /// </summary>
        /// <param name="strUser">Username</param>
        /// <param name="password">Password</param>
        /// <param name="isReadOnly">Is ReadOnly</param>
        public static void AddUserToSvr(String strUser, String password, Boolean isReadOnly)
        {
            MongoServer mongoSvr = SystemManager.GetCurrentService();
            //必须使用MongoCredentials来添加用户不然的话，Password将使用明文登入到数据库中！
            //这样的话，在使用MongoCredentials登入的时候，会发生密码错误引发的认证失败
            MongoCredentials newUser = new MongoCredentials(strUser, password, true);
            if (mongoSvr.AdminDatabase.FindUser(strUser) == null)
            {
                mongoSvr.AdminDatabase.AddUser(newUser, isReadOnly);
            }
        }
        /// <summary>
        /// Remove A User From Admin database
        /// </summary>
        /// <param name="strUser">UserName</param>
        public static void RemoveUserFromSvr(String strUser)
        {
            MongoServer mongoSvr = SystemManager.GetCurrentService();
            if (mongoSvr.AdminDatabase.FindUser(strUser) != null)
            {
                mongoSvr.AdminDatabase.RemoveUser(strUser);
            }
        }
        /// <summary>
        /// Add User
        /// </summary>
        /// <param name="strUser">Username</param>
        /// <param name="password">Password</param>
        /// <param name="isReadOnly">Is ReadOnly</param>
        public static void AddUserToDB(String strUser, String password, Boolean isReadOnly)
        {
            MongoDatabase mongoDB = SystemManager.GetCurrentDataBase();
            MongoCredentials newUser = new MongoCredentials(strUser, password, false);
            if (mongoDB.FindUser(strUser) == null)
            {
                mongoDB.AddUser(newUser, isReadOnly);
            }
        }

        /// <summary>
        /// Remove User
        /// </summary>
        /// <param name="strUser">Username</param>
        public static void RemoveUserFromDB(String strUser)
        {
            MongoDatabase mongoDB = SystemManager.GetCurrentDataBase();
            if (mongoDB.FindUser(strUser) != null)
            {
                mongoDB.RemoveUser(strUser);
            }
        }
        #endregion
    }
}

