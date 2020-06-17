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
        static string Author = "asd";
        static string ProgramTitle = "osu! 镜像下载客户端";
        static string CurDLClientVer = "b20200617.1";
        static string ServerURL = "https://mirror.osu.pink/osu-update.php";
        static string DefaultUserAgent = string.Format("osu-download/{0}", CurDLClientVer);
        static bool isUnix = System.Environment.OSVersion.ToString().ToLower().Contains("unix");
        static void Debug(string type, string msg, char dot = '.')
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(string.Format("[DEBUG] {0}: {1}{2}", type, msg, dot));
            Console.ResetColor();
#endif
        }
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
            Debug("Ping", Address);
            try
            {
                Ping ping = new Ping();
                PingReply pingReply = ping.Send(Address, 1000);
                if (pingReply.Status == IPStatus.Success)
                {
                    if (pingReply.RoundtripTime <= 32767)
                    {
                        return (short)pingReply.RoundtripTime;
                    }
                }
            } catch { }
            return 1000;
        }
        static void WriteMirror(byte count, string MirrorName, short MirrorPingDelay, string MirrorAD, bool MirrorHashCheck = false)
        {
            string MirrorText = string.Format("{0}.{1} (延迟：{2}ms)", count, MirrorName, MirrorPingDelay);
            if (MirrorAD != null)
            {
                MirrorText += " " + string.Format("[{0}]", MirrorAD);
            }
            Console.ForegroundColor = MirrorHashCheck ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine(MirrorText);
            if (!MirrorHashCheck)
            {
                Console.WriteLine("警告：上面的黄色 Mirror 不具备防篡改和完整性校验功能");
            }
            Console.ResetColor();
        }
        static void AddMirrorSplitWithoutConflict(ref SortedDictionary<short, List<string[]>> MirrorDictionary, short MirrorPing, string[] MirrorSplit)
        {
            Debug("AddMirrorSplitWithoutConflict", string.Format("MirrorPing: {0}, MirrorSplit: {1}", MirrorPing, string.Join("|", MirrorSplit)));
            if (!MirrorDictionary.ContainsKey(MirrorPing))
            {
                List<string[]> tmpList = new List<string[]>
                            {
                                MirrorSplit
                            };
                MirrorDictionary.Add(MirrorPing, tmpList);
            }
            else
            {
                MirrorDictionary[MirrorPing].Add(MirrorSplit);
            }
        }
        static HttpWebRequest SendRequest(string URL, int Timeout = 10000)
        {
            Debug("SendRequest", string.Format("URL: {0}, Timeout: {1}", URL, Timeout));
            HttpWebRequest wr = WebRequest.Create(URL) as HttpWebRequest;
            wr.UserAgent = DefaultUserAgent;
            wr.Timeout = Timeout;
            return wr;
        }
        class ClientWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                Debug("ClientWebClient", address.OriginalString);
                HttpWebRequest req = base.GetWebRequest(address) as HttpWebRequest;
                req.KeepAlive = false;
                req.Timeout = 300000;
                req.UserAgent = DefaultUserAgent;
                return req;
            }
        }
        [STAThread]
        static void Main(string[] args)
        {
            string InstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!");
            string[] License = null;
            Debug("Main/ClientInfo", string.Format("isUnix: {0}, CurDLClientVer: {1}, ServerURL: {2}, DefaultUserAgent: {3}", isUnix.ToString(), CurDLClientVer, ServerURL, DefaultUserAgent));
            if (File.Exists("License"))
            {
                Debug("Main", "License found", '!');
                License = File.ReadAllText("License").Split(':');
                if (License.Length != 2)
                {
                    Debug("Main", "License is invalid");
                    License = null;
                }
            }
            Console.Title = ProgramTitle;
            Console.WriteLine(string.Format("欢迎使用由 {0} 提供的 {1}！", Author, ProgramTitle));
            Console.WriteLine("[广告/反馈] QQ群：132783429");
            Console.WriteLine(string.Format("当前镜像下载客户端版本为：{0}，客户端默认会安装到 {1}。", CurDLClientVer, InstallPath));
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
                    if (isUnix)
                    {
                        Console.WriteLine("Unix 下暂不支持选择路径，你可以在下载完成后通过默认路径来移动客户端。");
                    }
                    else
                    {
                        string SelectedDirPath = DialogDirPath().TrimEnd('\\', '/');
                        if (!string.IsNullOrEmpty(SelectedDirPath))
                        {
                            InstallPath = SelectedDirPath;
                            Console.WriteLine(string.Format("新的安装路径为：{0}", InstallPath));
                        }
                    }
                    goto recheck;
                default:
                    goto recheck;
            }
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
            try
            {
                Console.WriteLine("正在获取 Mirror...");
                HttpWebRequest MirrorRequest = SendRequest(ServerURL + "?om=1" + ((License != null) ? "&p=1" : ""));
                HttpWebResponse MirrorWebResponse = MirrorRequest.GetResponse() as HttpWebResponse;
                string MirrorResponse = new StreamReader(MirrorWebResponse.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                MirrorWebResponse.Close();
                Debug("Main/MirrorResponse", MirrorResponse);
                string OfficialMirror = null;
                SortedDictionary<short, List<string[]>> MirrorDictionary = new SortedDictionary<short, List<string[]>>();
                string[] MirrorArrResponse = MirrorResponse.Split(Environment.NewLine.ToCharArray());
                foreach (string tmp in MirrorArrResponse)
                {
                    if (tmp.StartsWith("OfficialNotice:"))
                    {
                        Console.WriteLine("来自服务器的公告：" + tmp.Replace("OfficialNotice:", ""));
                    }
                    else if (tmp.StartsWith("OfficialMirror:"))
                    {
                        // 不同于 OfficialNotice，实现使其只支持最多一个，如果有多个 OfficalMirror，那么应该会选取最后一个
                        // 2020-06-16: 服务端必须下发官方 Mirror，以保证第三方镜像可以通过官方镜像进行校验；原有服务端实现的隐藏官方 Mirror 改为客户端实现
                        OfficialMirror = tmp.Replace("OfficialMirror:", "");
                    }
                    else if (tmp.StartsWith("Mirror:"))
                    {
                        string[] MirrorSplit = tmp.Replace("Mirror:", "").Split('|');
                        short MirrorPing = Ping(new Uri(MirrorSplit[0]).Host);
                        AddMirrorSplitWithoutConflict(ref MirrorDictionary, MirrorPing, MirrorSplit);
                    }
                }
                byte count = 1;
                // 无论何种镜像类型，默认都不会开启防篡改和完整性校验功能
                string OfficialMirrorURL = null;
                bool OfficialMirrorHashCheck = false;
                byte OfficialMirrorHiddenFlag = 0;
                if (OfficialMirror != null)
                {
                    string[] OfficialMirrorSplit = OfficialMirror.Split('|');
                    OfficialMirrorURL = OfficialMirrorSplit[0];
                    string OfficialMirrorName = OfficialMirrorSplit[1];
                    if (OfficialMirrorSplit.Length > 2 && (OfficialMirrorSplit[2] == "1" || OfficialMirrorSplit[2] == "2"))
                    {
                        OfficialMirrorSplit[2] = "1";
                        OfficialMirrorHashCheck = true;
                    }
                    if (OfficialMirrorSplit.Length > 4 && byte.TryParse(OfficialMirrorSplit[4], out OfficialMirrorHiddenFlag) && OfficialMirrorHiddenFlag < 3 && OfficialMirrorHiddenFlag != 0)
                    {
                        short OfficialMirrorPing = Ping(new Uri(OfficialMirrorURL).Host);
                        if (OfficialMirrorHiddenFlag == 2)
                        {
                            string OfficialMirrorAD = (OfficialMirrorSplit.Length > 3 && !string.IsNullOrEmpty(OfficialMirrorSplit[3])) ? OfficialMirrorSplit[3] : null;
                            WriteMirror(count++, OfficialMirrorName, OfficialMirrorPing, OfficialMirrorAD, OfficialMirrorHashCheck);
                        } else
                        {
                            AddMirrorSplitWithoutConflict(ref MirrorDictionary, OfficialMirrorPing, OfficialMirrorSplit);
                        }
                    }
                    
                }
                List<string> MirrorList = new List<string>();
                List<byte> MirrorCheckList = new List<byte>();
                foreach (var tmp in MirrorDictionary)
                {
                    short MirrorPingDelay = tmp.Key;
                    foreach (var tmp2 in tmp.Value) {
                        string MirrorName = tmp2[1];
                        byte MirrorHashCheck = 0;
                        if (tmp2.Length > 2 && byte.TryParse(tmp2[2], out MirrorHashCheck))
                        {
                            if (MirrorHashCheck < 0 || MirrorHashCheck > 2)
                            {
                                MirrorHashCheck = 0;
                            }
                        }
                        string MirrorAD = (tmp2.Length > 3 && !string.IsNullOrEmpty(tmp2[3])) ? tmp2[3] : null;
                        MirrorList.Add(tmp2[0]);
                        MirrorCheckList.Add(MirrorHashCheck);
                        WriteMirror(count++, MirrorName, MirrorPingDelay, MirrorAD, (MirrorHashCheck > 0) ? true : false);
                    }
                }
                byte SelectedMirror;
                RecheckServer:
                Console.WriteLine("输入数字以选择 Mirror。");
                while (byte.TryParse(Console.ReadKey(true).KeyChar.ToString(), out SelectedMirror) != true)
                {
                    goto RecheckServer;
                }
                if (SelectedMirror >= count || SelectedMirror == 0)
                {
                    Console.WriteLine("所选择的 Mirror 不存在。");
                    goto RecheckServer;
                }
                string CurMirror = null;
                byte CurMirrorHashCheck = 0;
                if (OfficialMirror != null && OfficialMirrorHiddenFlag == 2 && SelectedMirror == 1)
                {
                    CurMirror = OfficialMirrorURL;
                    CurMirrorHashCheck = (byte)(OfficialMirrorHashCheck == true ? 1 : 0);
                }
                else
                {
                    SelectedMirror--;
                    if (OfficialMirror != null && OfficialMirrorHiddenFlag == 2)
                    {
                        SelectedMirror--;
                    }
                    CurMirror = MirrorList[SelectedMirror];
                    CurMirrorHashCheck = MirrorCheckList[SelectedMirror];
                }
                Console.WriteLine("正在检查选定的分支...");
                string CheckMirror = CurMirrorHashCheck == 2 ? CurMirror : OfficialMirrorURL;
                Debug("Main/SelectedMirror", string.Format("SelectedMirror: {0}, CheckMirror: {1}, CurMirrorHashCheck: {2}", SelectedMirror, CheckMirror, CurMirrorHashCheck));
                HttpWebRequest StreamRequest = SendRequest(CheckMirror + string.Format("osu-stream.php?s={0}", Version));
                StreamRequest.Timeout = 10000;
                HttpWebResponse StreamWebResponse = StreamRequest.GetResponse() as HttpWebResponse;
                string StreamResponse = new StreamReader(StreamWebResponse.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                StreamWebResponse.Close();
                if (string.IsNullOrEmpty(StreamResponse))
                {
                    throw new Exception("无法获取数据！");
                }
                Debug("Main/StreamResponse", StreamResponse);
                Console.WriteLine("检查完毕！");
                Stopwatch spendTime = new Stopwatch();
                spendTime.Start();
                string[] ArrResponse = StreamResponse.Split(Environment.NewLine.ToCharArray());
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
                        string FilePath = Path.Combine(InstallPath, filearr[1]);
                        if (File.Exists(FilePath))
                        {
                            if (GetFileHash(FilePath) == filearr[0].ToLower())
                            {
                                Console.WriteLine(string.Format("文件已存在且为最新版：{0}", filearr[1]));
                                continue;
                            }
                            isUpdate = "更新";
                            File.Delete(FilePath);
                        }
                        Console.WriteLine(string.Format("正在" + isUpdate + "：{0}...", filearr[1]));
                        ClientWebClient wc = new ClientWebClient();
                        wc.DownloadFile(CurMirror + (CurMirrorHashCheck > 0 ? uri : filearr[1]) + ((License != null) ? string.Format("?u={0}&h={1}",License[0],License[1]) : ""), FilePath);
                        string ServerFileHash = filearr[0].ToLower();
                        string ClientFileHash = GetFileHash(FilePath);
                        if (MirrorCheckList[SelectedMirror] > 0)
                        {
                            Debug("Main/FileCheck", string.Format("ServerFileHash: {0}, ClientFileHash: {1}", ServerFileHash, ClientFileHash));
                            if (ServerFileHash != ClientFileHash)
                            {
                                File.Delete(FilePath);
                                Console.WriteLine(string.Format(isUpdate + "失败，文件不一致：{0}", filearr[1]));
                            }
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
                if (!isUnix)
                {
                    DoneText += "，将自动打开安装路径";
                    System.Diagnostics.Process.Start(InstallPath);
                }
                DoneText += "！";
                Console.WriteLine(DoneText);
            }
            catch (Exception e)
            {
                Debug("Main/Exception", e.ToString());
                string ErrorMessage = e.Message;
                if (e is WebException we && we.Status == WebExceptionStatus.ProtocolError)
                {
                    ErrorMessage = "返回错误信息：" + new StreamReader((we.Response as HttpWebResponse).GetResponseStream(), Encoding.UTF8).ReadToEnd();
                }
                Console.WriteLine("下载失败！" + ErrorMessage);
            }
            Console.WriteLine("请按任意键继续...");
            Console.ReadKey(true);
        }
    }
}
