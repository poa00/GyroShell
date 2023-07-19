﻿using System;
using System.IO;
using System.Text;
using Windows.Storage;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.ApplicationModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media.Imaging;

using static GyroShell.Interfaces.AUMIDIPropertyStore;
using static GyroShell.Helpers.Win32.Win32Interop;
using static GyroShell.Helpers.Win32.GetHandleIcon;
using Windows.Management.Deployment;
using Windows.Devices.Sensors;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using ABI.Windows.ApplicationModel.Activation;

namespace GyroShell.Helpers.WinRT
{
    internal class UWPWindowHelper
    {
        private static Dictionary<string, string> packageFamilyToFullNameMap;
        private static PackageManager packageManager;

        public UWPWindowHelper()
        {
            packageFamilyToFullNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            packageManager = new PackageManager();
            IEnumerable<Package> packages = packageManager.FindPackagesForUser(null);
            foreach (Package package in packages)
            {
                packageFamilyToFullNameMap[package.Id.FamilyName] = package.Id.FullName;
            }
        }

        public static string GetUwpAppIconPath(IntPtr hwnd)
        {
            return Uri.UnescapeDataString(Uri.UnescapeDataString(GetPackageFromAppHandle(hwnd).Result.Logo.AbsolutePath)).Replace("/", "\\");
        }

        public static async Task<Bitmap> LoadBitmapFromUwpIcon(string filePath)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
            using (Stream stream = await file.OpenStreamForReadAsync())
            {
                return new Bitmap(stream);
            }
        }

        public async static Task<SoftwareBitmapSource> ConvertBitmapToSoftwareBitmapSource(Bitmap bmp)
        {
            using (Bitmap smoothedBmp = ApplyBicubicInterpolation(bmp, bmp.Width, bmp.Height))
            {
                BitmapData data = smoothedBmp.LockBits(new Rectangle(0, 0, smoothedBmp.Width, smoothedBmp.Height), ImageLockMode.ReadOnly, smoothedBmp.PixelFormat);
                byte[] bytes = new byte[data.Stride * data.Height];
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                smoothedBmp.UnlockBits(data);

                SoftwareBitmap softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, smoothedBmp.Width, smoothedBmp.Height, BitmapAlphaMode.Premultiplied);
                softwareBitmap.CopyFromBuffer(bytes.AsBuffer());

                SoftwareBitmapSource source = new SoftwareBitmapSource();
                source.SetBitmapAsync(softwareBitmap);
                return source;
            }
        }

        public static bool IsUwpWindow(IntPtr hWnd)
        {
            if (GetPackageFromAppHandle(hWnd).Result == null) 
            { 
                return false; 
            }
            else 
            {
                return true; 
            }
        }

        public async static Task<Package> GetPackageFromAppHandle(IntPtr hWnd)
        {
            // Get the AUMID associated with the app handle
            Guid guidPropertyStore = new Guid("{886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99}");
            IPropertyStore propertyStore;
            int result = SHGetPropertyStoreForWindow(hWnd, ref guidPropertyStore, out propertyStore);
            if (result != 0)
            {
                return null;
            }

            // Get the AUMID value from the property store
            PropertyKey propertyKey = new PropertyKey(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);
            PropVariant value = new PropVariant();
            propertyStore.GetValue(ref propertyKey, out value);
            string aumid = null;

            try
            {
                if (value.VarType == (ushort)VarEnum.VT_LPWSTR)
                {
                    aumid = Marshal.PtrToStringUni(value.Data);
                }
            }
            finally
            {
                value.Dispose();
            }

            if (string.IsNullOrEmpty(aumid))
            {
                return null;
            }

            // Get the Package object from the AUMID
            try
            {
                string[] aumidParts = aumid.Split('!');
                string packageFamilyName = aumidParts[0];

                if (packageFamilyToFullNameMap.TryGetValue(packageFamilyName, out string packageFullName))
                {
                    return packageManager.FindPackageForUser(null, packageFullName);
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine("UWPWindowHelper => AUMID Filter: " + e.Message);
            }

            return null;
        }
    }
}