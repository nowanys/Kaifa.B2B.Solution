﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
namespace Kaifa.B2B.VendorAlloc
{
    public   class CalendarTask
    {
        private Timer _timer;
        private string _strDir;
        private string _connstring;
        private string _backupDir;
        private string _warehouse;

        public CalendarTask(string strDir, string backupDir, string connstring,string warehouse)
        {
            _strDir = strDir;
            _connstring = connstring;
            _backupDir = backupDir;
            _warehouse = warehouse;

            if (!Directory.Exists(_backupDir))
            {
                Directory.CreateDirectory(_backupDir);
            }

            if (!Directory.Exists(_strDir))
            {
                Directory.CreateDirectory(_strDir);
            }

        }

        public void Start() {
            _timer = new Timer(new TimerCallback(Do));
            _timer.Change(30 * 1000, 3000);
        }
        private void Do(object obj) {
            //List<FileInfo> listFiles = new List<FileInfo>(); //保存所有的文件信息  
            try
            {
                lock (obj)
                {
                    DirectoryInfo directory = new DirectoryInfo(_strDir);
                    FileInfo[] fileInfoArray = directory.GetFiles();
                    if (fileInfoArray.Length > 0)
                    {
                        for (int i = 0; i < fileInfoArray.Length; i++)
                        {
                            FileInfo file = fileInfoArray[i];
                            if (!file.IsReadOnly && (file.Extension.ToLower() == ".xls" || file.Extension.ToLower() == ".xlsx"))
                            {
                                Console.WriteLine(file.FullName);
                                CalendarProcess calendar = new CalendarProcess(file.FullName, _connstring,_warehouse);
                                calendar.Read();
                                //Thread.Sleep(100);

                                file.MoveTo(Path.Combine(_backupDir, file.Name + DateTime.Now.ToString(".yyyyMMddHHmmssfff") + ".bk"));
                                Console.WriteLine("move...." + file.FullName);
                            }

                        }


                    }
                }
            }catch(Exception e){
                Console.WriteLine(e.Message);
                //throw e;
            }
        }
        public void Stop() {
            if (_timer != null)
            {
                _timer.Dispose();
            }
        }

    }
}
