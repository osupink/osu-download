using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace osu_download
{
    class Program
    {
        static string GetFileHash(string FilePath)
        {
            MD5CryptoServiceProvider hc = new MD5CryptoServiceProvider();
            FileStream fs = new FileStream(FilePath, FileMode.Open);
            string FileHash = BitConverter.ToString(hc.ComputeHash(fs)).Replace("-", "").ToLower();
            fs.Close();
            return FileHash;
        }
        static string DialogDirPath()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
            return dialog.SelectedPath;
        }
        static short Ping(string Address)
        {
            Ping ping = new Ping();
            PingReply pingreply = ping.Send(Address, 2000);
            if (pingreply.Status == IPStatus.Success)
            {
                if (pingreply.RoundtripTime > 32767)
                {
                    return -1;
                }
                return (short)pingreply.RoundtripTime;
            }
            return -1;
        }
        [STAThread]
        static void Main(string[] args)
        {
            string Author = "asd";
            string ProgramTitle = "osu! 镜像下载客户端";
            string CurDLClientVer = "b20180110.2";
            string InstallPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\osu!";
            Console.Title = ProgramTitle;
            Console.WriteLine(string.Format("欢迎使用由 {0} 提供的 {1}！", Author, ProgramTitle));
            Console.WriteLine("[广告/反馈] QQ群：132783429");
            Console.WriteLine(string.Format("当前下载客户端版本为：{0}，绿色版客户端默认会安装到 {1}。", CurDLClientVer, InstallPath));
            Console.WriteLine("输入数字然后按下回车(Enter)以选择分支：");
            Console.WriteLine("0 [Latest(最新版)]，1 [Fallback(回退版)]，2 [Beta(测试版)]，3 [CuttingEdge(前沿版)]，4 [选择路径]");
            byte ver;
            recheck:
            while (byte.TryParse(Console.ReadKey(true).KeyChar.ToString(), out ver) != true)
            {
                Console.WriteLine("输入数字然后按下回车(Enter)以选择分支：");
                Console.WriteLine("0 [Latest(最新版)]，1 [Fallback(回退版)]，2 [Beta(测试版)]，3 [CuttingEdge(前沿版)]");
            }
            if (ver > 3)
            {
                if (ver == 4)
                {
                    string SelectedDirPath = DialogDirPath().TrimEnd('\\');
                    if (!string.IsNullOrEmpty(SelectedDirPath))
                    {
                        InstallPath = SelectedDirPath;
                        Console.WriteLine(string.Format("新的安装路径为：{0}", InstallPath));
                    }
                }
                goto recheck;
            }
            try
            {
                Console.WriteLine("正在获取 Mirror...");
                HttpWebRequest MirrorRequest = WebRequest.Create("https://www.userpage.me/osu-update.php?om=1&v=" + CurDLClientVer) as HttpWebRequest;
                MirrorRequest.Method = "GET";
                MirrorRequest.Timeout = 10000;
                HttpWebResponse MirrorWebResponse = MirrorRequest.GetResponse() as HttpWebResponse;
                string MirrorResponse = new StreamReader(MirrorWebResponse.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                MirrorWebResponse.Close();
                string OfficialMirror = null;
                SortedDictionary<short, string[]> MirrorDictionary = new SortedDictionary<short, string[]>();
                string[] MirrorArrResponse = MirrorResponse.Split(Environment.NewLine.ToCharArray());
                foreach (string tmp in MirrorArrResponse)
                {
                    if (tmp.StartsWith("OfficialMirror:"))
                    {
                        OfficialMirror = tmp.Replace("OfficialMirror:", "");
                    }
                    else if (tmp.StartsWith("Mirror:"))
                    {
                        string[] MirrorSplit = tmp.Replace("Mirror:", "").Split('|');
                        MirrorDictionary.Add(Ping(new Uri(MirrorSplit[0]).Host), MirrorSplit);
                    }
                }
                byte count = 1;
                string OfficialMirrorURL = null;
                if (OfficialMirror != null)
                {
                    string[] OfficialMirrorSplit = OfficialMirror.Split('|');
                    OfficialMirrorURL = OfficialMirrorSplit[0];
                    string OfficialMirrorName = OfficialMirrorSplit[1];
                    short OfficialPingDelay = Ping(new Uri(OfficialMirrorURL).Host);
                    string OfficialMirrorTitle = string.Format("{0}.{1} (延迟：{2}ms)", count++, OfficialMirrorName, OfficialPingDelay);
                    if (OfficialMirrorSplit.Length > 2)
                    {
                        OfficialMirrorTitle += string.Format(" [{0}]", OfficialMirrorSplit[2]);
                    }
                    Console.WriteLine(OfficialMirrorTitle);
                }
                List<string> MirrorList = new List<string>();
                foreach (var tmp in MirrorDictionary)
                {
                    string MirrorTitle = string.Format("{0} (延迟：{1}ms)", tmp.Value[1], tmp.Key);
                    if (tmp.Value.Length > 2)
                    {
                        MirrorTitle += string.Format(" [{0}]", tmp.Value[2]);
                    }
                    MirrorList.Add(tmp.Value[0]);
                    Console.WriteLine(string.Format("{0}.{1}", count++, MirrorTitle));
                }
                Console.WriteLine("输入数字以选择 Mirror。");
                byte SelectedMirror;
                recheckserver:
                while (byte.TryParse(Console.ReadKey(true).KeyChar.ToString(), out SelectedMirror) != true)
                {
                    Console.WriteLine("输入数字以选择 Mirror。");
                }
                if (SelectedMirror > count || SelectedMirror == 0)
                {
                    Console.WriteLine("所选择的 Mirror 不存在。");
                    goto recheckserver;
                }
                string CurMirror = null;
                if (OfficialMirrorURL != null && SelectedMirror == 1)
                {
                    CurMirror = OfficialMirrorURL;
                }
                else
                {
                    SelectedMirror--;
                    if (OfficialMirrorURL != null)
                    {
                        SelectedMirror--;
                    }
                    CurMirror = MirrorList[SelectedMirror];
                }
                Console.WriteLine("正在检查选定的分支...如果检查时间过久，可能是因为正在镜像该分支。");
                HttpWebRequest CheckRequest = WebRequest.Create(string.Format("https://www.userpage.me/osu-update.php?s={0}&v={1}", ver, CurDLClientVer)) as HttpWebRequest;
                CheckRequest.Method = "GET";
                CheckRequest.Timeout = 120000;
                HttpWebResponse CheckWebResponse = CheckRequest.GetResponse() as HttpWebResponse;
                string CheckResponse = new StreamReader(CheckWebResponse.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                CheckWebResponse.Close();
                if (string.IsNullOrEmpty(CheckResponse))
                {
                    throw new Exception("无法获取数据！");
                }
                Console.WriteLine("检查完毕！");
                string[] ArrResponse = CheckResponse.Split(Environment.NewLine.ToCharArray());
                foreach (string tmp in ArrResponse)
                {
                    if (CurMirror != null && tmp.StartsWith("File:"))
                    {
                        string uri = tmp.Replace("File:", "");
                        string[] filearr = uri.Split('/');
                        if (filearr.Length < 2)
                        {
                            throw new Exception("数据不正确！");
                        }
                        if (!Directory.Exists(InstallPath))
                        {
                            Console.WriteLine("已创建安装目录。");
                            Directory.CreateDirectory(InstallPath);
                        }
                        string isUpdate = "下载";
                        string filepath = InstallPath + @"\" + filearr[1];
                        if (File.Exists(filepath))
                        {
                            if (GetFileHash(filepath) == filearr[0].ToLower())
                            {
                                Console.WriteLine(string.Format("文件已存在且为最新版：{0}", filearr[1]));
                                continue;
                            }
                            isUpdate = "更新";
                            File.Delete(filepath);
                        }
                        Console.WriteLine(string.Format("正在" + isUpdate + "：{0}...", filearr[1]));
                        WebClient wc = new WebClient();
                        wc.DownloadFile(CurMirror + uri, filepath);
                        if (filearr[0].ToLower() != GetFileHash(filepath))
                        {
                            File.Delete(filepath);
                            Console.WriteLine(string.Format(isUpdate + "失败，文件不一致：{0}", filearr[1]));
                        }
                        else
                        {
                            Console.WriteLine(string.Format(isUpdate + "完成：{0}", filearr[1]));
                        }
                    }
                }
                Console.WriteLine("全部文件已下载/更新完成，将自动打开安装路径！");
                System.Diagnostics.Process.Start(InstallPath);
            }
            catch (Exception e)
            {
                string ErrorMessage = e.Message;
                if (e is WebException we)
                {
                    if (we.Status == WebExceptionStatus.ProtocolError)
                    {
                        ErrorMessage = "返回错误信息：" + new StreamReader((we.Response as HttpWebResponse).GetResponseStream(), Encoding.UTF8).ReadToEnd();
                    }
                }
                Console.WriteLine("下载失败！" + ErrorMessage);
            }
            Console.WriteLine("请按任意键继续...");
            Console.ReadKey(true);
        }
    }
}
