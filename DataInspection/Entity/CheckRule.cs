using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DataInspection.Entity
{
    /// <summary>
    /// 检查对象
    /// </summary>
    public class CheckRule
    {
        //对象名
        public string ObjectName { get; set; }

        [XmlArray]
        [XmlArrayItem("CheckItem")]
        public List<CheckItem> CheckItems { get; set; }
    }

    public class CheckItem
    {
        public string CheckName { get; set; }

        public string CheckType { get; set; }

        public bool CheckIIS { get; set; }

        public string CheckPath { get; set; }

        public string CheckNode { get; set; }

        public bool EqualNode { get; set; }

        public string NodeValue { get; set; }

        public string CheckDesc { get; set; }

        [XmlArray]
        [XmlArrayItem("CheckItemSon")]
        public List<CheckItemSon> CheckItemSons { get; set; }
    }

    public class CheckItemSon
    {
        public string CheckName { get; set; }

        public string CheckType { get; set; }

        public string CheckIIS { get; set; }

        public string CheckPath { get; set; }

        public string CheckDesc { get; set; }
    }
}
