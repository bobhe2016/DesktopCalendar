
# DesktopCalendar
一个基于 VB.NET (net2.0) 开发的高颜值、轻量级 Windows 桌面日历与时钟小工具，解决win7/WIN8中文用户没有农历日历的遗憾。支持农历显示、智能托盘/任务栏吸附模式、无缝跨日同步以及长日历面板展开，占用资源极低，完美契合 Windows 原生体验。

# 📅 DesktopCalendar (桌面精致日历小工具)

一个基于 VB.NET (net2.0) 开发的高颜值、轻量级 Windows 桌面日历与时钟小工具。支持农历显示、智能托盘/任务栏吸附模式、无缝跨日同步以及长日历面板展开，占用资源极低，完美契合 Windows 原生体验。

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)
![Language](https://img.shields.io/badge/language-VB.NET-brightgreen.svg)

---

## ✨ 核心特性

* **🎨 挂件与悬浮模式**
  * **标准挂件形态**：在桌面上提供优雅精致的日历小卡片，支持自由拖拽、透明度调节及随机色彩主题切换。
  * **原生任务栏吸附形态**：支持吸附至 Windows 任务栏右下角，提供清晰的双行（时间+日期）原生的视觉体验。
* **🔔 托盘与后台常驻**
  * 支持一键**最小化到系统托盘**。
  * **单实例进程互斥**：防止重复运行，多次启动自动唤醒已有实例。
* **📆 智能长日历展开**
  * **单击/双击快捷唤醒**：支持通过托盘或主面板展开完整的月度长日历。
  * **防遮挡定位算子**：智能检测屏幕边界与任务栏位置，自动调整弹窗坐标，确保日历卡片 100% 完整显示不被边缘截断。
* **⏰ 无缝跨日更新**
  * 采用实时定时器与日期状态比对机制，在跨日瞬间（23:59:59 ➔ 00:00:00）**无需重启程序即可自动刷新**公历、星期与农历数据。
* **🌙 准确的农历算法**
  * 内置精准的农历（阴历）与闰月转换逻辑，支持鼠标悬停（ToolTip）快速查看完整的农历信息。

---

## 🖱️ 交互指南

| 操作位置 | 操作方式 | 触发行为 |
| :--- | :--- | :--- |
| **主挂件顶部 Header** | 双击 | 最小化到系统托盘 |
| **主挂件主体区域** | 双击 | 展开 / 关闭完整月度长日历 |
| **托盘图标 (NotifyIcon)** | 单击 | 智能定位并弹出完整长日历（再次点击收起） |
| **托盘图标 (NotifyIcon)** | 双击 | 恢复并在桌面上显示主挂件 |
| **任务栏吸附模式** | 鼠标悬停 | 浮现当前时间、公历年月日、星期及农历详细提示 |
| **任务栏吸附模式** | 双击 | 退出任务栏模式，恢复桌面标准挂件形态 |

---

## 🛠️ 项目环境与编译

* **开发语言**：VB.NET (.NET Framework 2.0 )
* **开发工具**：Visual Studio 2022
* **依赖组件**：无第三方依赖，纯 Windows Win32 API + WinForms 原生实现

## 效果预览
<img width="1920" height="1080" alt="preview-3" src="https://github.com/user-attachments/assets/6e5f3f04-093b-4bd4-b901-d6177ff3e448" />
<img width="1920" height="1080" alt="preview-2" src="https://github.com/user-attachments/assets/3738fff4-8b6f-4b4d-89f5-bdc20ec547b5" />
<img width="1920" height="1080" alt="preview-1" src="https://github.com/user-attachments/assets/40d550f1-452b-4b08-b869-c55d7fb94e9c" />


