using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Mysoft.MAP2.Common.Trace;
using Mysoft.ESB.Asynchronous;
using Mysoft.ESB.Asynchronous.HandlerResults;

using Mysoft.Map.Data;
using DataInspection.Business;
using DataInspection.Helper;
using Mysoft.Map.Extensions;
using Mysoft.Map.Extensions.DAL;
using System.Configuration;
using Mysoft.ESB.Entity;
using Mysoft.MAP2.Platform.DataAccess;
namespace DataInspection
{
    /// <summary>
    /// <!-- 数据巡检：作业操作类 key:与数据库OperationType字段一致。 value=这个类的命名空间 ，可以根据更加自己的喜好调整。
    ///	<add key="837C4C4F-A10E-4397-9D24-44718817B8F0" value="DataInspection.MyCommand,DataInspection"/>
    /// </summary>
    public class MyCommand : AsyncOperationCommand
    {
        public MyCommand(IAsyncService asyncService, IOrganizationConfiguration config)
            : base(asyncService, config)
        {
        }
        protected override AsyncHandlerResult InternalExecute(AsyncEvent asyncEvent)
        {
            try
            {
                this.DoWork();
            }
            catch (Exception exception)
            {
                // 异步作业不允许抛异常，只记录日志。
                MyTrace.Write(TraceCategory.AsyncService, TraceLevel.Error,
                         "自定义作业操作：处理异常: " + DateTime.Now.ToString() + exception.ToString());
            }
            return new AsyncSucceededResult();
        }
        /// <summary>
        /// 此处执行业务操作逻辑，数据库可以直接使用小平台连接ESB数据库，进行读写操作。
        /// 如果非得要自己取sql连接，请去config.ApplicationName的值（注册表键）。自己获取。
        /// </summary>
        private void DoWork()
        {
            //此处实现代码...

            //MyTrace.Write(TraceCategory.AsyncService, TraceLevel.Error,
            //             "自定义作业操作：作业测试: " + DateTime.Now.ToString());


            WriteMsg("调度开始");
            try
            {
                //获取ERP业务对象
                ERPObjectBusiness erpObjectBusiness = new ERPObjectBusiness();
                var erpObjectList = erpObjectBusiness.GetERPObjectList();

                //初始化配置数据
                //ERPConfigBusiness erpConfigBusiness = new ERPConfigBusiness();
                //var _erpConfig = erpConfigBusiness.ErpConfigs;

                string item = ConfigurationManager.AppSettings["ApplicationName"];
                if (item == null)
                    WriteMsg("ApplicationName为空");
                var strConnection = (new MyDbConnection(item)).GetConnectionString();
                //if (!string.IsNullOrEmpty(item))
                //{
                //    Initializer.UnSafeInit(strConnection);
                //}

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

                            SqlExecuteHelper itemSqlHelper = new SqlExecuteHelper(strConnection);
                            itemTable = itemSqlHelper.GetDataTable(SelectSQL, erpTable.TableName, erpObject.ObjectName);

                            //现默认两个DataTable需要对比的字段是一样的
                            DataTableHelper.CompareTable(mainTable, itemTable, out diffTable, erpTable.TableKey, erpTable.TableProps);

                            //写入日志
                            var filePath = string.Format(@"{0}\{1}\{2}\{3}\{4}", System.AppDomain.CurrentDomain.BaseDirectory, "InspectionLog", itemConfig.DisplayName, DateTime.Now.ToString("yyyyMMdd"), erpObject.ObjectName);

                            if (!Directory.Exists(filePath))
                            {
                                Directory.CreateDirectory(filePath);
                            }

                            diffTable.WriteXml(string.Format(@"{0}\{1}\{2}\{3}\{4}\{5}.xml", System.AppDomain.CurrentDomain.BaseDirectory, "InspectionLog", itemConfig.DisplayName, DateTime.Now.ToString("yyyyMMdd"), erpObject.ObjectName, erpTable.TableName));
                        }
                    }
                }
            }
            catch (Exception err)
            {
                WriteMsg(err.ToString());
            }

            WriteMsg("调度结束");
        }

        public void WriteMsg(string msg)
        {
            //string logPath = @"D:\Log.txt";
            //using (StreamWriter sw = new StreamWriter(logPath, true))
            //{
            //    String logMsg = String.Format("[{0}]{1}", DateTime.Now.ToString(), msg);
            //    sw.WriteLine(logMsg);
            //}

            new MyLogger().WriteLog(Mysoft.ESB.Entity.Enumeration.LogLevel.None, msg + DateTime.Now.ToString());

            //   MyTrace.Write(TraceCategory.AsyncService, TraceLevel.Error,
            //"" + msg + "：处理异常: " + DateTime.Now.ToString());
        }
    }
}
