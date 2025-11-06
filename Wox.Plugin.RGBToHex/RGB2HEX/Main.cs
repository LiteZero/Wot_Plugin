using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Wox.Plugin;

namespace Wox.Plugin.RGBToHex
{
    public class Main : IPlugin
    {
        private PluginInitContext _context;
        private string _previewDirPath = Path.Combine(Path.GetTempPath(), @"Wox.RGBToHex.Previews\");
        private const int IMG_SIZE = 32;

        public void Init(PluginInitContext context)
        {
            _context = context;

            // 创建预览图目录
            if (!Directory.Exists(_previewDirPath))
            {
                Directory.CreateDirectory(_previewDirPath);
            }
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();

            try
            {
                // 如果没有输入，显示使用说明
                if (string.IsNullOrWhiteSpace(query.Search))
                {
                    results.Add(new Result
                    {
                        Title = "RGB转HEX颜色转换器",
                        SubTitle = "请输入RGB颜色值，格式: 255,255,255 或 255,255,255,1.0 (RGBA)",
                        IcoPath = "Images/icon.png",
                        Action = _ => { return false; }
                    });
                    return results;
                }

                string search = query.Search.Trim();
                var colorData = ParseColorInput(search);

                if (colorData == null)
                {
                    results.Add(new Result
                    {
                        Title = "输入格式错误",
                        SubTitle = "请输入正确的RGB格式，如: 255,255,255 或 255,255,255,1.0",
                        IcoPath = "Images/icon.png",
                        Action = _ => { return false; }
                    });
                    return results;
                }

                int r = colorData.Item1;
                int g = colorData.Item2;
                int b = colorData.Item3;
                float? a = colorData.Item4;

                string hexColor = RgbToHex(r, g, b, a);

                if (string.IsNullOrEmpty(hexColor))
                {
                    results.Add(new Result
                    {
                        Title = "颜色转换失败",
                        SubTitle = "请检查输入值是否有效(RGB:0-255, Alpha:0-1)",
                        IcoPath = "Images/icon.png",
                        Action = _ => { return false; }
                    });
                    return results;
                }

                string colorType = a.HasValue ? "RGBA" : "RGB";
                string inputStr = a.HasValue ? $"RGBA({r},{g},{b},{a})" : $"RGB({r},{g},{b})";

                // 生成颜色预览图
                string previewPath = CreateColorPreview(r, g, b, a);

                // 主要结果 - 带#的HEX值
                results.Add(new Result
                {
                    Title = $"HEX: {hexColor}",
                    SubTitle = $"点击复制 {colorType} {inputStr} 的HEX值",
                    IcoPath = previewPath,
                    Action = _ =>
                    {
                        CopyToClipboard(hexColor);
                        _context.API.ShowMsg("复制成功", $"HEX值 {hexColor} 已复制到剪贴板", "Images/icon.png");
                        return true;
                    }
                });
            }
            catch (Exception ex)
            {
                results.Add(new Result
                {
                    Title = "处理过程中发生错误",
                    SubTitle = $"错误详情: {ex.Message}",
                    IcoPath = "Images/icon.png",
                    Action = _ => { return false; }
                });
            }

            return results;
        }

        private Tuple<int, int, int, float?> ParseColorInput(string input)
        {
            try
            {
                // 移除空格并分割
                string[] parts = input.Replace(" ", "").Split(',');

                if (parts.Length == 3)
                {
                    // RGB格式
                    int r = int.Parse(parts[0]);
                    int g = int.Parse(parts[1]);
                    int b = int.Parse(parts[2]);
                    return new Tuple<int, int, int, float?>(r, g, b, null);
                }
                else if (parts.Length == 4)
                {
                    // RGBA格式
                    int r = int.Parse(parts[0]);
                    int g = int.Parse(parts[1]);
                    int b = int.Parse(parts[2]);
                    float a = float.Parse(parts[3], CultureInfo.InvariantCulture);
                    return new Tuple<int, int, int, float?>(r, g, b, a);
                }
            }
            catch (Exception)
            {
                // 解析失败
            }

            return null;
        }

        private string RgbToHex(int r, int g, int b, float? a = null)
        {
            try
            {
                // 确保RGB值在0-255范围内
                r = Math.Max(0, Math.Min(255, r));
                g = Math.Max(0, Math.Min(255, g));
                b = Math.Max(0, Math.Min(255, b));

                // 转换为HEX
                string hexColor = $"#{r:X2}{g:X2}{b:X2}";

                // 处理Alpha通道
                if (a.HasValue)
                {
                    float alpha = Math.Max(0, Math.Min(1, a.Value));
                    int alphaInt = (int)(alpha * 255);
                    hexColor += $"{alphaInt:X2}";
                }

                return hexColor.ToUpper();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string CreateColorPreview(int r, int g, int b, float? a = null)
        {
            try
            {
                // 生成文件名
                string fileName = a.HasValue ?
                    $"color_{r}_{g}_{b}_{(int)(a.Value * 255)}.png" :
                    $"color_{r}_{g}_{b}.png";

                string filePath = Path.Combine(_previewDirPath, fileName);

                // 如果预览图已存在，直接返回路径
                if (File.Exists(filePath))
                    return filePath;

                // 创建颜色预览图
                using (Bitmap bitmap = new Bitmap(IMG_SIZE, IMG_SIZE))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    // 设置颜色（忽略Alpha通道，因为预览图不支持透明度）
                    System.Drawing.Color color = System.Drawing.Color.FromArgb(r, g, b);
                    graphics.Clear(color);

                    // 保存预览图
                    bitmap.Save(filePath, ImageFormat.Png);
                }

                return filePath;
            }
            catch (Exception)
            {
                // 如果生成预览失败，返回默认图标路径
                return "Images/icon.png";
            }
        }

        private void CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                _context.API.ShowMsg("复制失败", $"无法复制到剪贴板: {ex.Message}", "Images/icon.png");
            }
        }
    }
}