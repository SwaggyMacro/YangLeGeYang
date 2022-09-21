using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace SheepSheep
{
    public partial class Form1 : Form
    {
        private int passWay = 0;
        private int stateGame = 0;
        private int stateTopic = 0;
        private string costTime = "10";
        private string matchInfo;
        public Form1()
        {
            InitializeComponent();
            init();
        }

        private void init()
        {
            this.comboBox1.SelectedIndex = 0;
            Thread t = new Thread(() => getWechatToken());
            t.Start();
        }

        private void getWechatToken()
        {
            string token = "";
            token = WcToken.GetTokenFromWechat();
            if (token.Equals("false"))
            {
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show(this, "未检测到\"微信->羊了个羊\"，请重新登陆微信并打开羊了个羊游戏。\n仍然显示此提示的话请自行抓包获取Token。", "Tips:", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }));
            }
            else
            {
                this.Invoke(new Action(() =>
                {
                    this.textBox1.Text = token;
                    MessageBox.Show(this, "检测到Token，已自动填写，请直接\"羊它！\"即可", "Tips:", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            }
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.passWay = this.comboBox1.SelectedIndex;
            if (this.passWay == 1)
            {
                string ret = string.Empty;
                InputDialog.Show(out ret, "请输入过关耗时: ");
                this.costTime = ret;
                Console.WriteLine(ret);
            }
        }

        private void passTopicGame(int passTimes)
        {
            string apiUrl = string.Format("https://cat-match.easygame2021.com/sheep/v1/game/topic_game_over?rank_score=1&rank_state=1&rank_time={0}&rank_role=1&skin=1", costTime);
            for (int i = 0; i < passTimes; i++)
            {
                if (stateTopic == 0)
                {
                    return;
                }
                if (passWay == 0)
                {
                    try
                    {
                        Random r = new Random();
                        costTime = r.Next(0, 3000).ToString();
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
                        request.Method = "GET";
                        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36 Edg/105.0.1343.33";
                        request.Host = "cat-match.easygame2021.com";
                        request.Headers.Add("t", this.textBox1.Text);
                        request.Timeout = 5000;
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        Stream myResponseStream = response.GetResponseStream();
                        StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                        string retString = myStreamReader.ReadToEnd();
                        myStreamReader.Close();
                        myResponseStream.Close();
                        if (retString.Contains("\"err_code\":0"))
                        {
                            this.Invoke(new Action(() =>
                            {
                                toolStripStatusLabel2.Text = "通关次数: " + (i + 1).ToString();
                            }));
                            Console.WriteLine(retString);
                        }
                    }
                    catch (Exception ex)
                    {
                        //throw ex;
                    }

                }
                if (i == passTimes - 1)
                {
                    this.Invoke(new Action(() =>
                    {
                        if (stateGame == 0)
                        {
                            stateTopic = 0;
                            this.button1.Text = "羊它！";
                        }
                    }));
                }
            }
        }
        private void getMatchInfo() {
            try
            {
                string ret = HttpGet("https://cat-match.easygame2021.com/sheep/v1/game/personal_info?");
                ret = HttpGet("https://cat-match.easygame2021.com/sheep/v1/game/map_info_ex?matchType=3");
                JObject jsonMapMd5 = JObject.Parse(ret);
                string mapMd5 = (string)jsonMapMd5["data"]["map_md5"][1];
                string mapInfo = getMapInfo(mapMd5);
                JObject jsonLevelData = JObject.Parse(mapInfo);
                var levelData = jsonLevelData["levelData"];
                JArray seq = new JArray();
                int length = 0;
                foreach (var item in levelData)
                {
                    foreach (var value in item)
                    {
                        length += value.Count();
                    }

                }
                for (int i = 0; i < length; i++)
                {
                    seq.Add(JObject.Parse($"{{\"chessIndex\": {(i > 127 ? 127 : i)}, \"timeTag\": {(i > 127 ? 127 : i)}}}"));
                }
                var data = new Packer().Pack("=BB", default, 8, 3);
                foreach (var item in seq)
                {
                    int c = (int)item["chessIndex"], t = (int)item["timeTag"];
                    var tmp = new Packer().Pack("=BBBBBB", default, 34, 4, 8, c, 16, t);
                    data = Combine(data, tmp);
                }
                matchInfo = Convert.ToBase64String(data);
            }
            catch
            {

            }
        }
        private string getMapInfo(string mapMd5)
        {
            Uri url = new Uri($"https://cat-match-static.easygame2021.com/maps/{mapMd5}.txt");
            Stream stream = WebRequest.Create(url).GetResponse().GetResponseStream();
            StreamReader content = new StreamReader(stream);
            return content.ReadToEnd();
        }
        private string HttpGet(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36 Edg/105.0.1343.33";
            request.Host = "cat-match.easygame2021.com";
            request.Headers.Add("t", this.textBox1.Text);
            request.Timeout = 5000;
            //request.ContentType = "application/json;charset=utf-8";
            Console.WriteLine(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }
        private string passGame() {
            string apiUrl = string.Format("https://cat-match.easygame2021.com/sheep/v1/game/game_over_ex?rank_score=1&rank_state=1&rank_time={0}&rank_role=1&skin=1", costTime);
            try
            {
                Random r = new Random();
                costTime = r.Next(0, 3000).ToString();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
                request.Method = "POST";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36 Edg/105.0.1343.33";
                request.Host = "cat-match.easygame2021.com";
                request.Headers.Add("t", this.textBox1.Text);
                request.Timeout = 5000;
                request.ContentType = "application/json;charset=utf-8";
                string postParam = postParam = $"{{\"rank_score\":1,\"rank_state\":1,\"rank_time\":{costTime},\"rank_role\":1,\"skin\":1,\"MatchPlayInfo\":\"{matchInfo}\"}}";
                byte[] postBody = Encoding.UTF8.GetBytes(postParam);
                request.ContentLength = postBody.Length;
                Stream postStream = request.GetRequestStream();
                postStream.Write(postBody, 0, postBody.Length);
                postStream.Close();
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
                Console.WriteLine(retString);
                return retString;
            }
            catch (Exception ex)
            {
                //throw ex;
                return "";
            }
        }
        private void passTheGame(int passTimes)
        {
            //string apiUrl = string.Format("https://cat-match.easygame2021.com/sheep/v1/game/game_over?rank_score=1&rank_state=1&rank_time={0}&rank_role=1&skin=1", costTime);
            for (int i = 0; i < passTimes; i++)
            {
                if (stateGame == 0)
                {
                    return;
                }
                if (passWay == 0)
                {
                    getMatchInfo();
                    if (passGame().Contains("\"err_code\":0"))
                    {
                        this.Invoke(new Action(() =>
                        {
                            toolStripStatusLabel1.Text = "加入次数: " + (i + 1).ToString();
                        }));
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!this.textBox1.Text.Equals(""))
            {
                if (stateGame == 0)
                {
                    stateGame = 1;
                    stateTopic = 1;
                    Thread t = new Thread(() => passTheGame(int.Parse(this.textBox2.Text)));
                    t.Start();
                    Thread tt = new Thread(() => passTopicGame(int.Parse(this.textBox3.Text)));
                    tt.Start();
                    this.button1.Text = "停止羊！";
                    MessageBox.Show(this, "开始羊咯！", "Tips:", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    stateGame = 0;
                    stateTopic = 0;
                    this.button1.Text = "羊它！";
                }
            }
            else
            {
                MessageBox.Show(this, "未填写Token，请点击\"获取Token\"后再\"羊它！\"", "Tips:", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\b')
            {
                if ((e.KeyChar < '0') || (e.KeyChar > '9'))
                {
                    e.Handled = true;
                }
            }
        }
        private void label4_GetToken() {
            this.Invoke(new Action(() =>
            {
                label4.Enabled = false;
                label4.Text = "获取中...";
            }));
            getWechatToken();
            this.Invoke(new Action(() =>
            {
                label4.Enabled = true;
                label4.Text = "获取Token";
            }));
        }
        private void label4_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(() => label4_GetToken());
            t.Start();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/SwaggyMacro/YangLeGeYang");
        }
        
        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] ret = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }

        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
    }
}
