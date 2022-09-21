# YangLeGeYang

羊了个羊辅助  
**2022-09-21 修复加入羊群**
## 🏷️前言

本项目可一键通关羊了个羊，请勿滥用本程序持续对游戏服务器造成压力，仅用于学习研究使用，一切后果自负。  
**不会更新多线程版本，对游戏服务器压力过大。**

## 📥助手下载
⭐最新版本下载：[羊了个羊助手SheepSheep.exe](https://github.com/SwaggyMacro/YangLeGeYang/releases/latest/download/SheepSheep.exe)

## 🙋FAQ/常见问题
<details><summary>1. 为什么无法获取Token</summary>
① 可能是微信版本太老，请尝试安装最新版本微信，https://windows.weixin.qq.com/ 。  

② 需要在微信搜索打开羊了个羊并授权微信信息才行，不知道怎么授权的就直接玩一下第一关再
</details>

## 📚使用教程

**已更新v0.6自动获取Token版本（无需手动抓包）**  
**只需要你电脑登陆PC微信然后打开羊了个羊游戏，再打开本助手即可自动获取Token，直接开刷即可！**
1. 登陆电脑版的PC微信
2. 打开羊了个羊
3. 打开本助手
4. 一键通关
![Animation](https://user-images.githubusercontent.com/38845682/190835970-4ae6cb7c-712e-4f40-9041-21ae850a162f.gif)

## 📚抓包教程

使用前需要用抓包工具获取账号token(t)，需要一定动手能力，获取后就是傻瓜式操作。  

💻PC端抓包工具：Fiddler、Http Debugger  

📱Android端抓包工具：HttpCarry  

📱iPhone端抓包工具：Stream  

Android端抓包视频教程：[http://u.ncii.cn/LIgVd](http://u.ncii.cn/LIgVd)  
iPhone端抓包视频教程：[http://u.ncii.cn/68dxv](http://u.ncii.cn/68dxv)  

---
(仅介绍PC抓包方式)
此处以PC端（推荐PC）为例：
1. 安装登陆微信 [PC微信官网](https://windows.weixin.qq.com/)
2. 搜索羊了个羊小游戏（先不要点进去）
   ![image](https://user-images.githubusercontent.com/38845682/190594067-d2d6fcda-ae12-4e1e-ba29-6ffba33e8138.png)
3. 打开HTTP Debugger工具（首次打开提示安装证书，一定要点安装），点击左上角Home->Start开始抓包
   [点我下载 HTTP Debugger](http://kkx.xiazais.com/small/HTTPDebugger.Cracked.rar)
   ![image](https://user-images.githubusercontent.com/38845682/190595665-1542a67e-7c30-4521-8610-17e38d2783ee.png)
4. 然后点开羊了个羊小游戏，等待游戏加载完后切换到HTTP Debugger
5. 按住 CTRL+F 搜索 "easygame2021" 然后复制这个数据包中协议头的token(t)内容 （如果搜索不到的话就退出微信 重新登陆再重复抓包步骤）
   ![image](https://user-images.githubusercontent.com/38845682/190595519-9188c778-2cba-4bc4-98a7-854b4021a61b.png)
6. 将Token复制到羊了个羊助手上面，然后点击“羊它！”即可，通关次数以及通关耗时可以自己调整。
   ![image](https://user-images.githubusercontent.com/38845682/190595932-05c04298-1726-45e8-bca8-b9d56e63ae3a.png)
