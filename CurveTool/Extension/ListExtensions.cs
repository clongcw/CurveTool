using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CurveTool.Extension
{
    public static class ListExtensions
    {
        public static List<T> DeepCopy<T>(this List<T> original)
        {
            // 如果原始列表为 null，返回空列表
            if (original == null)
            {
                return new List<T>();
            }

            // 创建一个内存流以进行序列化和反序列化
            using (MemoryStream stream = new MemoryStream())
            {
                // 使用二进制格式进行序列化和反序列化
                IFormatter formatter = new BinaryFormatter();

                // 序列化原始列表
                formatter.Serialize(stream, original);

                // 将流定位到开头
                stream.Seek(0, SeekOrigin.Begin);

                // 反序列化新列表
                List<T> copy = (List<T>)formatter.Deserialize(stream);

                return copy;
            }
        }
    }
}
