using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Unispect
{
    public static class Serializer
    {
        public static void Save(string filePath, object objectToSerialize)
        {
            try
            {
                using (Stream stream = File.Open(filePath, FileMode.Create))
                {
                    var bin = new BinaryFormatter();
                    bin.Serialize(stream, objectToSerialize);
                }
            }
            catch (IOException)
            {
            }
        }
        public static T Load<T>(string filePath) where T : new()
        {
            var result = new T();

            try
            {
                using (Stream stream = File.Open(filePath, FileMode.Open))
                {
                    var bin = new BinaryFormatter();
                    result = (T)bin.Deserialize(stream);
                }
            }
            catch (IOException)
            {
            }

            return result;
        }
    }
}