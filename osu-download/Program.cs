using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            try
            {
                Ping ping = new Ping();
                PingReply pingreply = ping.Send(Address, 2000);
                if (pingreply.Status == IPStatus.Success)
                {
                    if (pingreply.RoundtripTime <= 32767)
                    {
                        return (short)pingreply.RoundtripTime;
                    }
                }
            } catch { }
            return 2000;
        }
        static void WriteMirror(byte count, string MirrorName, short MirrorPingDelay, string MirrorAD)
        {
            string MirrorText = string.Format("{0}.{1} (延迟：{2}ms)", count, MirrorName, MirrorPingDelay);
            if (MirrorAD != null)
            {
                MirrorText += " " + string.Format("[{0}]", MirrorAD);
            }
            Console.WriteLine(MirrorText);
        }
        [STAThread]
        static void Main(string[] args)
        {
            string Author = "asd";
            string ProgramTitle = "osu! 镜像下载客户端";
            string CurDLClientVer = "b20180706.1";
            string InstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!");
            string[] License = null;
            if (File.Exists("License"))
            {
                License = File.ReadAllText("License").Split(':');
                if (License.Length != 2)
                {
                    License = null;
                }
            }
            Console.Title = ProgramTitle;
            Console.WriteLine(string.Format("欢迎使用由 {0} 提供的 {1}！", Author, ProgramTitle));
            Console.WriteLine("[广告/反馈] QQ群：132783429");
            Console.WriteLine(string.Format("当前下载客户端版本为：{0}，客户端默认会安装到 {1}。", CurDLClientVer, InstallPath));
            try
            {
                RegistryKey RegLM = Registry.LocalMachine;
                RegistryKey FIPSKey = RegLM.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa\FipsAlgorithmPolicy", true);
                if (FIPSKey.GetValue("Enabled").ToString() != "0")
                {
                    FIPSKey.SetValue("Enabled", 0);
                    Console.WriteLine("已自动修复导致 osu! 无限更新及工具无法使用的问题。");
                }
                FIPSKey.Close();
                RegLM.Close();
            } catch { }
            byte VerNumber;
            recheck:
            Console.WriteLine("输入数字以选择路径或分支：");
            Console.WriteLine("0 [选择路径]，1 [Latest(最新版)]，2 [Fallback(回退版)]，3 [Beta(测试版)]，4 [CuttingEdge(前沿版)]");
            while (byte.TryParse(Console.ReadKey(true).KeyChar.ToString(), out VerNumber) != true)
            {
                goto recheck;
            }
            string Version = "Stable40";
            switch (VerNumber)
            {
                case 1:
                    break;
                case 2:
                    Version = "Stable";
                    break;
                case 3:
                    Version = "Beta40";
                    break;
                case 4:
                    Version = "CuttingEdge";
                    break;
                case 0:
                    string SelectedDirPath = DialogDirPath().TrimEnd('\\', '/');
                    if (!string.IsNullOrEmpty(SelectedDirPath))
                    {
                        InstallPath = SelectedDirPath;
                        Console.WriteLine(string.Format("新的安装路径为：{0}", InstallPath));
                    }
                    goto recheck;
                default:
                    goto recheck;
            }
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
            try
            {
                Console.WriteLine("正在获取 Mirror...");
                HttpWebRequest MirrorRequest = WebRequest.Create("https://www.userpage.me/osu-update.php?" + string.Format("om=1&v={0}", CurDLClientVer) + ((License != null) ? "&p=1" : "")) as HttpWebRequest;
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
                    short OfficialMirrorPingDelay = Ping(new Uri(OfficialMirrorURL).Host);
                    string OfficialMirrorAD = (OfficialMirrorSplit.Length > 2) ? OfficialMirrorSplit[2] : null;
                    WriteMirror(count++, OfficialMirrorName, OfficialMirrorPingDelay, OfficialMirrorAD);
                }
                List<string> MirrorList = new List<string>();
                foreach (var tmp in MirrorDictionary)
                {
                    string MirrorName = tmp.Value[1];
                    short MirrorPingDelay = tmp.Key;
                    string MirrorAD = (tmp.Value.Length > 2) ? tmp.Value[2] : null;
                    MirrorList.Add(tmp.Value[0]);
                    WriteMirror(count++, MirrorName, MirrorPingDelay, MirrorAD);
                }
                byte SelectedMirror;
                recheckserver:
                Console.WriteLine("输入数字以选择 Mirror。");
                while (byte.TryParse(Console.ReadKey(true).KeyChar.ToString(), out SelectedMirror) != true)
                {
                    goto recheckserver;
                }
                if (SelectedMirror >= count || SelectedMirror == 0)
                {
                    Console.WriteLine("所选择的 Mirror 不存在。");
                    goto recheckserver;
                }
                string CurMirror = null;
                if (OfficialMirror != null && SelectedMirror == 1)
                {
                    CurMirror = OfficialMirrorURL;
                }
                else
                {
                    SelectedMirror--;
                    if (OfficialMirror != null)
                    {
                        SelectedMirror--;
                    }
                    CurMirror = MirrorList[SelectedMirror];
                }
                Console.WriteLine("正在检查选定的分支...");
                HttpWebRequest CheckRequest = WebRequest.Create(string.Format("https://www.userpage.me/osu-update.php?s={0}&v={1}", Version, CurDLClientVer)) as HttpWebRequest;
                CheckRequest.Timeout = 10000;
                HttpWebResponse CheckWebResponse = CheckRequest.GetResponse() as HttpWebResponse;
                string CheckResponse = new StreamReader(CheckWebResponse.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                CheckWebResponse.Close();
                if (string.IsNullOrEmpty(CheckResponse))
                {
                    throw new Exception("无法获取数据！");
                }
                Console.WriteLine("检查完毕！");
                Stopwatch spendTime = new Stopwatch();
                spendTime.Start();
                string[] ArrResponse = CheckResponse.Split(Environment.NewLine.ToCharArray());
                foreach (string tmp in ArrResponse)
                {
                    if (CurMirror != null && tmp.StartsWith("File:"))
                    {
                        string uri = tmp.Replace("File:", "");
                        string[] filearr = uri.Split('/');
                        string cfgpath = Path.Combine(InstallPath, "osu!.cfg");
                        if (filearr.Length < 2 || string.IsNullOrEmpty(filearr[1]))
                        {
                            throw new Exception("数据不正确！");
                        }
                        if (!Directory.Exists(InstallPath))
                        {
                            Console.WriteLine("已创建安装目录。");
                            Directory.CreateDirectory(InstallPath);
                        } else if (File.Exists(cfgpath))
                        {
                            File.Delete(cfgpath);
                            File.WriteAllText(cfgpath, string.Format("_ReleaseStream = {0}\n", Version));
                        }
                        string isUpdate = "下载";
                        string filepath = Path.Combine(InstallPath, filearr[1]);
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
                        wc.DownloadFile(CurMirror + uri + ((License != null) ? string.Format("?u={0}&h={1}",License[0],License[1]) : ""), filepath);
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
                spendTime.Stop();
                TimeSpan spendTimeSpan = spendTime.Elapsed;
                string DoneText = "全部文件已下载/更新完成，耗时：" + string.Format("{0:00}:{1:00}:{2:00}",
            spendTimeSpan.Hours, spendTimeSpan.Minutes, spendTimeSpan.Seconds);
                if (!System.Environment.OSVersion.ToString().ToLower().Contains("unix"))
                {
                    DoneText += "，将自动打开安装路径";
                    System.Diagnostics.Process.Start(InstallPath);
                }
                DoneText += "！";
                Console.WriteLine(DoneText);
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
