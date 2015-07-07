using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.IO;
using DataInspection.Business;
using System.Configuration;
using DataInspection.Entity;
using Mysoft.MAP2.Platform.DataAccess;
using Mysoft.Map.Extensions;
using Mysoft.Map.Extensions.DAL;
using Mysoft.ESB.Entity;
using DataInspection.Helper;
using System.Data;

using SqlExecuteHelper = DataInspection.Helper.SqlExecuteHelper;

namespace DataInspection
{
    class Program
    {
        static void Main(string[] args)
        {
	        ExeInspection();
        }


        private static void ExeInspection()
        {
            //此处实现代码...

            //MyTrace.Write(TraceCategory.AsyncService, TraceLevel.Error,
            //             "自定义作业操作：作业测试: " + DateTime.Now.ToString());


            WriteMsg("调度开始");
            //获取ERP业务对象
            ERPObjectBusiness erpObjectBusiness = new ERPObjectBusiness();
            var erpObjectList = erpObjectBusiness.GetERPObjectList();

            //初始化配置数据
            //ERPConfigBusiness erpConfigBusiness = new ERPConfigBusiness();
            //var _erpConfig = erpConfigBusiness.ErpConfigs;

            string item = ConfigurationManager.AppSettings["ApplicationName"];
            var strConnection = (new MyDbConnection(item)).GetConnectionString();

            WriteMsg(strConnection);
            if (!string.IsNullOrEmpty(item))
            {
                Initializer.UnSafeInit(strConnection);
            }

            string str = "SELECT ProviderID,ProviderName,DisplayName,Description,ProviderType, IsMainSite AS Protocol,DbServer,DBUserName,DbPassword,DbName FROM esb_Provider WHERE (1=1)";
            var _erpConfig = CPQuery.From(str).ToList<EsbProvider>();


            ////获取主系统数据
            var mainConfig = _erpConfig.SingleOrDefault(t => t.Protocol == "1");

            strConnection = string.Format("server={0};uid={1};pwd={2};database={3}", mainConfig.DbServer, mainConfig.DBUserName, mainConfig.DbPassword, mainConfig.DbName);

            SqlExecuteHelper sqlHelper = new SqlExecuteHelper(strConnection);

            //获取子系统数据
            var otherConfig = _erpConfig.Where(t => t.Protocol == "0" && t.DbName != mainConfig.DbName);
            otherConfig = DataTableHelper.DistinctBy(otherConfig, p => p.DbName);


            string SelectSQL = string.Empty;

            //业务系统
            DataSet _dsResult = new DataSet();
            foreach (var erpObject in erpObjectList)
            {
                foreach (var erpTable in erpObject.ERPTables)
                {
                    //得到主库数据
                    SelectSQL = string.Format("SELECT {0},'' as CheckAction FROM {1}", "*", erpTable.TableName);

                    DataTable mainTable = new DataTable();
                    mainTable = sqlHelper.GetDataTable(SelectSQL, erpTable.TableName, erpObject.ObjectName);

                    DataTable itemTable = new DataTable();

                    foreach (var itemConfig in otherConfig)
                    {
                        DataTable diffTable = new DataTable();
                        //得到子库数据

                        strConnection = string.Format("server={0};uid={1};pwd={2};database={3}", itemConfig.DbServer, itemConfig.DBUserName, itemConfig.DbPassword, itemConfig.DbName);

                        WriteMsg(strConnection);
                        SqlExecuteHelper itemSqlHelper = new SqlExecuteHelper(strConnection);
                        itemTable = itemSqlHelper.GetDataTable(SelectSQL, erpTable.TableName, erpObject.ObjectName);

                        //现默认两个DataTable需要对比的字段是一样的
                        DataTableHelper.CompareTable(mainTable, itemTable, out diffTable, erpTable.TableKey, erpTable.TableProps);

                        //写入日志
                        var filePath = string.Format(@"{0}\{1}\{2}\{3}", "巡检异常", itemConfig.DisplayName, DateTime.Now.ToString("yyyyMMdd"), erpObject.ObjectName);

                        if (!Directory.Exists(filePath))
                        {
                            Directory.CreateDirectory(filePath);
                        }

                        diffTable.WriteXml(string.Format(@"{0}\{1}\{2}\{3}\{4}.xml", "巡检异常", itemConfig.DisplayName, DateTime.Now.ToString("yyyyMMdd"), erpObject.ObjectName, erpTable.TableName));
                    }
                }
            }

            WriteMsg("调度结束");
        }

        public static void WriteMsg(string msg)
        {
            string logPath = @"D:\Log.txt";
            using (StreamWriter sw = new StreamWriter(logPath, true))
            {
                String logMsg = String.Format("[{0}]{1}", DateTime.Now.ToString(), msg);
                sw.WriteLine(logMsg);
            }
        }
    }
}
