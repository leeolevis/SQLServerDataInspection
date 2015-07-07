using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using DataInspection.Entity;
using DataInspection.Helper;

namespace DataInspection.Business
{
    public class ERPObjectBusiness
    {
        //保存路径文件
        string ERPObjectPath = System.AppDomain.CurrentDomain.BaseDirectory + "\\ERPObject.xml";

        public List<ERPObject> GetERPObjectList()
        {
            List<ERPObject> result = new List<ERPObject>();
            try
            {
                XElement xe = XElement.Load(ERPObjectPath);

                result = XmlSerializerExtensions.FromXml<List<ERPObject>>(xe.ToString());
            }
            catch (Exception erro)
            {
                throw erro;
            }
            return result;
        }
    }
}
