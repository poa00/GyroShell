﻿using GyroShell.Library.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using static GyroShell.Library.Helpers.Win32.Win32Interop;
using static GyroShell.Library.Interfaces.AUMIDIPropertyStore;

namespace GyroShell.Services.Helpers
{
    internal class AppHelperService : IAppHelperService
    {
        private Dictionary<string, string> m_pkgFamilyMap;
        private PackageManager m_pkgManager;
        private IBitmapHelperService m_bmpHelper;

        public AppHelperService(IBitmapHelperService bitmapHelper)
        {
            m_pkgFamilyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            m_pkgManager = new PackageManager();
            m_bmpHelper = bitmapHelper;

            IEnumerable<Package> packages = m_pkgManager.FindPackagesForUser(null);
            foreach (Package package in packages)
            {
                m_pkgFamilyMap[package.Id.FamilyName] = package.Id.FullName;
            }
        }

        public bool IsUwpWindow(IntPtr hWnd) =>
            GetPackageFromAppHandle(hWnd) != null;

        public Bitmap GetUwpOrWin32Icon(IntPtr hwnd, int targetSize)
        {
            if (IsUwpWindow(hwnd))
            {
                return GetGdiBitmapFromUwpApp(hwnd);
            }
            else
            {
                return GetGdiBitmapFromWin32App(hwnd, targetSize);
            }
        }

        public string GetWindowTitle(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);

            GetWindowText(hWnd, sb, sb.Capacity);

            return sb.ToString();
        }

        #region UWP Helper
        public string GetUwpAppIconPath(IntPtr hWnd)
        {
            string normalPath = Uri.UnescapeDataString(Uri.UnescapeDataString(GetPackageFromAppHandle(hWnd).Logo.AbsolutePath)).Replace("/", "\\");
            string finalPath = GetUwpExtraIcons(normalPath, GetWindowTitle(hWnd), normalPath);

            return finalPath;
        }

        public Package GetPackageFromAppHandle(IntPtr hWnd)
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

                if (m_pkgFamilyMap.TryGetValue(packageFamilyName, out string packageFullName))
                {
                    return m_pkgManager.FindPackageForUser(null, packageFullName);
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine("UWPWindowHelper => AUMID Filter: " + e.Message);
            }

            return null;
        }
        #endregion UWP Helper

        private string GetUwpExtraIcons(string path, string appName, string normalPath)
        {
            string[] pathParts = path.Split('\\');
            string rootAssetsFolder = string.Join("\\", pathParts.Take(pathParts.Length - 1));

            string[] allFiles = Directory.GetFiles(rootAssetsFolder);
            foreach (string filePath in allFiles)
            {
                if (Path.GetFileName(filePath).Contains("StoreLogo.scale-100"))
                {
                    string e = filePath.Replace(" ", "").ToLower();
                    if (e.Contains(appName.Replace(" ", "").ToLower()))
                    {
                        return filePath;
                    }
                }
            }

            return normalPath;
        }

        private Bitmap GetGdiBitmapFromUwpApp(IntPtr hWnd)
        {
            try
            {
                string iconPath = GetUwpAppIconPath(hWnd);

                using (Bitmap temp = m_bmpHelper.LoadBitmapFromPath(iconPath))
                {
                    return m_bmpHelper.RemoveTransparentPadding(temp);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }

        private Bitmap GetGdiBitmapFromWin32App(IntPtr hwnd, int targetSize)
        {
            try
            {
                GetWindowThreadProcessId(hwnd, out uint pid);
                Process proc = Process.GetProcessById((int)pid);
                Icon icon = Icon.ExtractAssociatedIcon(proc.MainModule.FileName);

                if (icon != null)
                {
                    Bitmap resizedIcon = new Bitmap(icon.ToBitmap(), new Size(targetSize, targetSize));
                    return resizedIcon;
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetHandleIcon => GetIcon: " + ex.Message);
                return null;
            }
        }
    }
}