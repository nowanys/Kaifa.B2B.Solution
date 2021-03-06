﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Excel;
using System.Data;
using System.Data.SqlClient;

namespace Kaifa.B2B.VendorAlloc
{
    public class CalendarProcess
    {
        private string _excelFile;
        private string _connectionstring;
        public const string WAREHOUSE = "WMWHSE1";
        private string _warehouse = "WMWHSE1";
        public CalendarProcess(string excelFile, string connectionstring,string warehouse)
        {
            _excelFile = excelFile;
            _warehouse = warehouse;
            _connectionstring = connectionstring;
        }
        
        public void Read() {
            using (FileStream stream = File.Open(_excelFile, FileMode.Open, FileAccess.Read)) {
                IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                excelReader.IsFirstRowAsColumnNames = true;
                DataSet result = excelReader.AsDataSet();
                DataTable dt = result.Tables[0];
                excelReader.Close();
                stream.Close();
                if (result.Tables.Count > 0 && dt.Rows.Count > 0)
                {
                    using (SqlConnection conn = new SqlConnection(_connectionstring))
                    {
                        conn.Open();
                       
                        SqlTransaction trx = conn.BeginTransaction();
                        try
                        {
                            
                            foreach (DataRow dr in dt.Rows)
                            {
                                object year = dr["财务年"];
                                if (year == null || string.IsNullOrEmpty(year.ToString()))
                                {
                                    continue;
                                }
                                SqlCommand cmd = conn.CreateCommand();
                                cmd.Transaction = trx;
                                cmd.CommandText = string.Format(@"INSERT INTO [{7}].[STXCALENDAR]
                                                    ([WHSEID]
                                                    ,[FISCALYEAR]
                                                    ,[QUARTER]
                                                    ,[WEEK]
                                                    ,[FROM_DATE]
                                                    ,[TO_DATE]
                                                    ,[ADDWHO]
                                                    )
                                                    VALUES
                                                          ('{0}'
                                                          ,'{1}'
                                                          ,'{2}'
                                                          ,'{3}'
                                                          ,'{4}'
                                                          ,'{5}'
                                                          ,'{6}'
                                                          )"

                                    , _warehouse
                                    , dr["财务年"]
                                    , dr["季度"]
                                    , dr["第几周"]
                                    , dr["开始日期"]
                                    , dr["结束日期"]
                                    , _warehouse
                                    ,_warehouse
                                   
                                    );

                                //Site	Prime Part	Alternate Part	Vendor Number	Allocation Percentage	Start Date Active	End Date Active	Planner Code	Non ASIC Indicator
                                cmd.ExecuteNonQuery();
                            }
                            trx.Commit();
                            conn.Close();

                            MailClient.SendCalendarNotificationMail(dt, _excelFile, string.Empty);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            trx.Rollback();
                            conn.Close();

                            MailClient.SendCalendarNotificationMail(dt, _excelFile, e.Message);
                        }
                    }
                }


            }

        }

    }
}
