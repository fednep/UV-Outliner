using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Windows.Data;
using Microsoft.Win32;
using System.Windows;

namespace UVOutliner
{

    public class SerialKeyChecker : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (SerialKey.VerifyKey((string)value))
                return "1";

            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return "";
        }
    }

    public class SerialKey
    {        
        private static char[] ValidLetters = new char[32] {'2','3','4','5','6','7','8','9','Q','W','E','R','T','Y','U','P','A','S','D','F','G','H','J','K','L','Z','X','C','V','B','N','M'};
        private static RegistryKey registryKey = Registry.CurrentUser;

        public static int TimesRun = 0;

        public static void CheckTimesRun()
        {
            if (!IsSoftwareRegistered())
            {
                RegistryKey outlinerKey = registryKey.OpenSubKey("Software\\UVOutliner\\", true);
                if (outlinerKey == null)
                {
                    TimesRun = 20;
                    return;
                }

                TimesRun = Math.Max(0, (int)outlinerKey.GetValue("Times", 0)) + 1;
                outlinerKey.SetValue("Times", TimesRun);
                outlinerKey.Close();
            }
        }

        public static void RegisterSerialKey(string Key)
        {
            if (VerifyKey(Key) == false)
                return;

            RegistryKey outlinerKey = registryKey.OpenSubKey("Software\\UVOutliner\\", true);
            if (outlinerKey == null)
                return;

            outlinerKey.SetValue("Serial", Key);
            outlinerKey.Close();
        }

        public static bool IsSoftwareRegistered()
        {
            return true;
            /*
            RegistryKey outlinerKey = registryKey.OpenSubKey("Software\\UVOutliner\\", false);
            if (outlinerKey == null)
                return false;

            string key = (string)outlinerKey.GetValue("Serial", "");
            return VerifyKey(key); 
             */
        }

        public static bool VerifyKey(string Key)
        {
            Key = Key.Trim();
            Key = Key.Replace("-", "");
            
            if (Key.Length != 20)
                return false;

            for (int i = 0; i < Key.Length; i++)
                if (IndexOf(Key[i], ValidLetters) == -1)
                    return false;

            byte[] unencData = new byte[10];
            for (int i = 0; i < 5; i++)
            {
                int data = StrToInt(Key.Substring(i*4, 4));
                int ddata = decode(data);

                unencData[i*2] = (byte)((ddata >> 8) & 0xFF);
                unencData[i*2 + 1] = (byte)(ddata & 0xFF);
            }
           
            byte[] finalHash = GetHash(new byte[] {unencData[0], unencData[1], unencData[2], 0});

            for (int i = 0; i < 7; i++)
                if (finalHash[i] != unencData[i + 3])
                    return false;

            return true;
        }

        private static byte[] GetHash(byte[] whatToHash)
        {
            SHA1CryptoServiceProvider cryptoTransformSHA1 = new SHA1CryptoServiceProvider();
            return cryptoTransformSHA1.ComputeHash(whatToHash);
        }

        private static string getHash(string text)
        {
            byte[] buffer = Encoding.Default.GetBytes(text);
            SHA1CryptoServiceProvider cryptoTransformSHA1 = 
            new SHA1CryptoServiceProvider();
            string strHash = "";
            
            byte[] hash = cryptoTransformSHA1.ComputeHash(buffer);
            for (int i = 0; i < hash.Length; i++)
            {
                strHash += (char)hash[i];
            }

            return strHash;
        }

        private static int decode(int data)
        {
            long s = 1;
            long t = data;
            long u = 20521; // public key

            while (u > 0)
            {
                if ((u & 1) != 0)
                    s = (s * t) % 565153;

                u >>= 1;
                t = (t * t) % 565153;
            }

            return (int)s;
        }

        private static int StrToInt(string numStr)
        {
            int res = 0;
            for (int i = 0; i < 4; i++)
            {
                int num = IndexOf(numStr[3 - i], ValidLetters);
                res <<= 5;
                res += num;
            }

            return res;
        }

        private static int IndexOf(char search, char[] array)
        {
            for (int i = 0; i < array.Length; i++)
                if (array[i] == search)
                    return i;

            return -1;
        }

        internal static bool ShowReminderIfNecessary(MainWindow parentWnd)
        {
            if (!IsSoftwareRegistered())
            {
                parentWnd.DimBorder.Visibility = Visibility.Visible;
                try
                {
                    wnd_LicenseReminder lRem = new wnd_LicenseReminder();
                    lRem.Owner = parentWnd;
                    return (bool)lRem.ShowDialog();
                }
                finally
                {
                    parentWnd.DimBorder.Visibility = Visibility.Hidden;
                }

            }

            return true;
        }
    }
}
