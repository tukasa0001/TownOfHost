using System;
using System.IO;
using System.Reflection;

namespace TownOfHostForE.Modules
{
    internal class CheckWhiteList
    {
        public static bool CheckWhiteListData()
        {
            bool returnBool = false;
            string resourceName = "TownOfHost_ForE.dll.ForhiteListEngine.dll";

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Logger.Info("Embedded DLL not found.","whitelist");
                    return false;
                }

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                string tempFilePath = Path.GetTempFileName();
                File.WriteAllBytes(tempFilePath, buffer);

                Assembly embeddedAssembly = Assembly.LoadFile(tempFilePath);

                Type utilitiesType = embeddedAssembly.GetType("ForhiteListEngine.WhiteListDll");
                MethodInfo isEvenMethod = utilitiesType.GetMethod("WhiteListEngine", BindingFlags.Public | BindingFlags.Static);

                object[] parameters = new object[] { EOSManager.Instance.friendCode };
                returnBool = (bool)isEvenMethod.Invoke(null, parameters);
            }

            return returnBool;
        }
    }
}
