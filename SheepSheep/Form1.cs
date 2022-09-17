using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SheepSheep
{
    public partial class Form1 : Form
    {
        private int passWay = 0;
        private int state = 0;
        private string costTime = "10";
        public Form1()
        {
            InitializeComponent();
            init();
        }

        private void init() {
            this.comboBox1.SelectedIndex = 0;
            string token = "";
            try
            {
                token = Marshal.PtrToStringAnsi(GetTokenFromWechat());
            }
            catch { }
            if (token.Equals("false"))
            {
                MessageBox.Show(this, "未检测到\"微信->羊了个羊\"，请重新登陆微信并打开羊了个羊游戏。\n仍然显示此提示的话请自行抓包获取Token。", "Tips:", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else {
                this.textBox1.Text = token;
                MessageBox.Show(this, "检测到Token，已自动填写，请直接\"羊它！\"即可", "Tips:", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        [DllImport("GetTokenFromWechat.dll")]
        static extern IntPtr GetTokenFromWechat();
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.passWay = this.comboBox1.SelectedIndex;
            if (this.passWay == 1) {
                string ret = string.Empty;
                InputDialog.Show(out ret, "请输入过关耗时: ");
                this.costTime = ret;
                Console.WriteLine(ret);
            }
        }

        private void passTheGame(int passTimes) {
            string apiUrl = string.Format("https://cat-match.easygame2021.com/sheep/v1/game/game_over?rank_score=1&rank_state=1&rank_time={0}&rank_role=1&skin=1", costTime);
            string json = "";
            for (int i = 0; i < passTimes; i++)
            {
                if (state == 0) {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show(this, "已停止羊！", "Tips:", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));
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
                                toolStripStatusLabel1.Text = "通关次数: " + i.ToString();
                            }));
                            Console.WriteLine(retString);
                        }
                    }
                    catch (Exception ex) {
                        //throw ex;
                    }
                    
                }
                if (i == passTimes - 1) {
                    this.Invoke(new Action(() =>
                    {
                        state = 0;
                        this.button1.Text = "羊它！";
                    }));
                }
            }
            

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (state == 0)
            {
                state = 1;
                Thread t = new Thread(() => passTheGame(int.Parse(this.textBox2.Text)));
                t.Start();
                this.button1.Text = "停止羊！";
                MessageBox.Show(this, "开始羊咯！", "Tips:", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else {
                state = 0;
                this.button1.Text = "羊它！";
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
    }
}
