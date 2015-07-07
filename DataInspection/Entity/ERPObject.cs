using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DataInspection.Entity
{
    /// <summary>
    /// ERP业务对象
    /// </summary>
    public class ERPObject
    {
        //ERP对象名
        public string ObjectName { get; set; }

        [XmlArray]
        [XmlArrayItem("ERPTable")]
        public List<ERPTable> ERPTables { get; set; }
    }

    /// <summary>
    /// 业务对象的所属表
    /// </summary>
    public class ERPTable
    {
        //ERP表名
        public string TableName { get; set; }

        //表主键 唯一值
        public string TableKey { get; set; }

        //ERP表名称
        public string TableDesc { get; set; }

        [XmlArray]
        [XmlArrayItem("TableProp")]
        public List<String> TableProps { get; set; }
    }
}
