using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Reflection;
using Scotty.Properties;

using Gma.UserActivityMonitor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Scotty
{
    class Program
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static NotifyIcon TrayIcon;

        [STAThread()]
        static void Main(string[] args)
        {
            if (args.Length > 0)
                CmdStart(args);
            else
            {
                HideWindow();
                CreateTrayIcon();
                RegisterHooks();
                Application.Run();
            }
        }

        private static void HideWindow()
        {
            Console.Title = "Scotty";
            IntPtr hWnd = FindWindow(null, "Scotty");
            ShowWindow(hWnd, 0);
        }

        private static void CreateTrayIcon()
        {
            TrayIcon = new NotifyIcon();
            TrayIcon.Icon = Resources.screenshot;
            TrayIcon.Visible = true;
        }

        private static void CmdStart(string[] args)
        {
            foreach(string arg in args)
                switch(arg)
                {
                    // Screenshot and upload
                    case "-scrot":
                        break;
                }
        }

        private static void RegisterHooks()
        {
            HookManager.KeyDown += HookManager_KeyDown;
            HookManager.KeyUp += HookManager_KeyUp;
        }

        private static void UnregisterHooks()
        {
            HookManager.KeyDown -= HookManager_KeyDown;
            HookManager.KeyUp -= HookManager_KeyUp;
        }

        private static bool CtrlDown = false;

        static void HookManager_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.PrintScreen:

                    Image scrot = null;
                    if (CtrlDown) scrot = SnippingTool.Snip();
                    else while (scrot == null) { scrot = GetScreenshot(); }

                    scrot.Save("scotty.png");

                    string url = "http://a.pomf.se/" + UploadScreenshot();
                    scrot.Dispose();

                    System.Threading.Thread.Sleep(1000);
                    Clipboard.SetText(url);
                    url = null;
                    TrayIcon.ShowBalloonTip(1000, null, "Url copied to clipboard!", ToolTipIcon.None);
                    break;

                case Keys.LControlKey:
                    CtrlDown = false;
                    break;

                case Keys.RControlKey:
                    CtrlDown = false;
                    break;
            }
        }

        public static void HookManager_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.LControlKey:
                    CtrlDown = true;
                    break;

                case Keys.RControlKey:
                    CtrlDown = true;
                    break;
            }
        }

        private static Image GetScreenshot()
        {
            Image scrot = null;

            if (Clipboard.ContainsImage())
                scrot = Clipboard.GetImage();

            return scrot;
        }

        private static string UploadScreenshot()
        {
            string fileUrl = null;
            byte[] fileByteArray = GetFileByteArray("scotty.png");
            File.Delete("scotty.png");

            byte[] xArray = Encoding.ASCII.GetBytes("------BOUNDARYBOUNDARY----\r\ncontent-disposition: form-data; name=\"id\"\r\n\r\n\r\n------BOUNDARYBOUNDARY----\r\ncontent-disposition: form-data; name=\"files[]\"; filename=\"scotty.png\"\r\nContent-type: image/png\r\n\r\n");
            byte[] boundaryByteArray = Encoding.ASCII.GetBytes("\r\n------BOUNDARYBOUNDARY----");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://pomf.se/upload.php");
            request.Method = "POST";

            //Client
            request.Accept = "*/*";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/32.0.1700.102 Safari/537.36";
                
            //Entity
            request.ContentLength = fileByteArray.Length + xArray.Length + boundaryByteArray.Length;
            request.ContentType = "multipart/form-data; boundary=----BOUNDARYBOUNDARY----";
                
            //Miscellaneous                
            request.Referer = "http://pomf.se/";

            //Transport
            request.KeepAlive = true;

            string responseContent = null;

            try
            {
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(xArray, 0, xArray.Length);
                requestStream.Write(fileByteArray, 0, fileByteArray.Length);
                requestStream.Write(boundaryByteArray, 0, boundaryByteArray.Length);
                requestStream.Close();

                WebResponse response = request.GetResponse();
                requestStream = response.GetResponseStream();

                StreamReader responseReader = new StreamReader(requestStream);
                responseContent = responseReader.ReadToEnd();

                responseReader.Close();
                requestStream.Close();
                response.Close();

                responseReader.Dispose();
                requestStream.Dispose();
                response.Dispose();
                request = null;
                xArray = null;
                fileByteArray = null;
                boundaryByteArray = null;
            }
            catch (Exception e)
            {
                Console.Write(e);
            }

            if(responseContent != null)
            {
                JObject json = JObject.Parse(responseContent);
                if (Convert.ToBoolean(json["success"]))
                {
                    JArray files = (JArray)json["files"];
                    fileUrl = (string)files[0]["url"];
                    files = null;
                }
                json = null;
            }
                     
            return fileUrl;
        }

        private static byte[] GetFileByteArray(string filename)
        {
            FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);

            byte[] fileByteArrayData = new byte[fileStream.Length];
            fileStream.Read(fileByteArrayData, 0, System.Convert.ToInt32(fileStream.Length));
            fileStream.Close();
            fileStream.Dispose();

            return fileByteArrayData;
        }
    }
}
