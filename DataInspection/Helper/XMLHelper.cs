using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;


namespace DataInspection.Helper
{
    public class XmlHelper
    {
        private static void XmlSerializeInternal(Stream stream, object o, Encoding encoding, bool isnamespaces)
        {
            if (o == null)
                throw new ArgumentNullException("o");
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            XmlSerializer serializer = new XmlSerializer(o.GetType());

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineChars = "\r\n";
            settings.Encoding = encoding;
            settings.IndentChars = "    ";

            //不生成声明头
            settings.OmitXmlDeclaration = !isnamespaces;

            MemoryStream w = new MemoryStream();

            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                serializer.Serialize(writer, o, namespaces);
                writer.Close();

            }
        }

        /// <summary>
        /// 将一个对象序列化为XML字符串
        /// </summary>
        /// <param name="o">要序列化的对象</param>
        /// <param name="encoding">编码方式</param>
        /// <param name="isnamespaces">是否需要命名空间true：需要 false:不需要</param>
        /// <returns>序列化产生的XML字符串</returns>
        public static string XmlSerialize(object o, Encoding encoding, bool isnamespaces)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                XmlSerializeInternal(stream, o, encoding, isnamespaces);

                stream.Position = 0;
                using (StreamReader reader = new StreamReader(stream, encoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// 从XML字符串中反序列化对象
        /// </summary>
        /// <typeparam name="T">结果对象类型</typeparam>
        /// <param name="s">包含对象的XML字符串</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>反序列化得到的对象</returns>
        public static T XmlDeserialize<T>(string s, Encoding encoding)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException("s");
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(encoding.GetBytes(s)))
            {
                using (StreamReader sr = new StreamReader(ms, encoding))
                {
                    return (T)mySerializer.Deserialize(sr);
                }
            }
        }

        /// <summary>
        /// 将一个对象按XML序列化的方式写入到一个文件
        /// </summary>
        /// <param name="o">要序列化的对象</param>
        /// <param name="path">保存文件路径</param>
        /// <param name="encoding">编码方式</param>
        /// <param name="isnamespaces">是否需要命名空间true：需要 false:不需要</param>
        public static void XmlSerializeToFile(object o, string path, Encoding encoding, bool isnamespaces)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                XmlSerializeInternal(file, o, encoding, isnamespaces);
            }
        }

        /// <summary>
        /// 读入一个文件，并按XML的方式反序列化对象。
        /// </summary>
        /// <typeparam name="T">结果对象类型</typeparam>
        /// <param name="path">文件路径</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>反序列化得到的对象</returns>
        public static T XmlDeserializeFromFile<T>(string path, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            string xml = File.ReadAllText(path, encoding);
            return XmlDeserialize<T>(xml, encoding);
        }

    }

    /// <summary>
    /// XmlSerializer 扩展
    /// </summary>
    public static class XmlSerializerExtensions
    {
        #region Private fields
        private static readonly Dictionary<RuntimeTypeHandle, XmlSerializer> MsSerializers = new Dictionary<RuntimeTypeHandle, XmlSerializer>();
        #endregion

        #region Public methods
        /// <summary>
        /// 序列化对象为xml字符串
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "value"></param>
        /// <returns></returns>
        public static string ToXml<T>(this T value)
            where T : new()
        {
            var serializer = GetValue(typeof(T));
            using (var stream = new MemoryStream())
            {
                using (var writer = new XmlTextWriter(stream, new UTF8Encoding()))
                {
                    serializer.Serialize(writer, value);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
        }

        /// <summary>
        /// 反序列化xml字符串为对象
        /// </summary>
        /// <typeparam name = "T">要序列化成何种对象</typeparam>
        /// <param name = "srcString">xml字符串</param>
        /// <returns></returns>
        public static T FromXml<T>(this string srcString)
            where T : new()
        {
            var serializer = GetValue(typeof(T));
            using (var stringReader = new StringReader(srcString))
            {
                using (XmlReader reader = new XmlTextReader(stringReader))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
        }
        #endregion

        #region Private methods
        private static XmlSerializer GetValue(Type type)
        {
            XmlSerializer serializer;
            if (!MsSerializers.TryGetValue(type.TypeHandle, out serializer))
            {
                lock (MsSerializers)
                {
                    if (!MsSerializers.TryGetValue(type.TypeHandle, out serializer))
                    {
                        serializer = new XmlSerializer(type);
                        MsSerializers.Add(type.TypeHandle, serializer);
                    }
                }
            }
            return serializer;
        }
        #endregion

    }

}
