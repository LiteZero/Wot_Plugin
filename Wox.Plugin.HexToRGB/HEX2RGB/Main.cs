using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wox.Plugin;

namespace Wox.Plugin.HexToRGB
{
    public class Main : IPlugin
    {
        private PluginInitContext _context;
        private string _previewDirPath = Path.Combine(Path.GetTempPath(), @"Wox.HexToRGB.Previews\");
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
                        Title = "HEX转RGB颜色转换器",
                        SubTitle = "请输入HEX颜色值，格式: #FFFFFF 或 #FFFFFFFF (RGBA)",
                        IcoPath = "Images/icon.png",
                        Action = _ => { return false; }
                    });
                    return results;
                }

                string search = query.Search.Trim();
                var colorData = ParseHexInput(search);

                if (colorData == null)
                {
                    results.Add(new Result
                    {
                        Title = "输入格式错误",
                        SubTitle = "请输入正确的HEX格式，如: #FFFFFF 或 #FFFFFFFF",
                        IcoPath = "Images/icon.png",
                        Action = _ => { return false; }
                    });
                    return results;
                }

                int r = colorData.Item1;
                int g = colorData.Item2;
                int b = colorData.Item3;
                float? a = colorData.Item4;

                string colorType = a.HasValue ? "RGBA" : "RGB";
                string rgbOutput = a.HasValue ? 
                    $"RGBA({r}, {g}, {b}, {a.Value:F2})" : 
                    $"RGB({r}, {g}, {b})";

                string hexInput = search.Length > 7 ? search.Substring(0, 7) : search;

                // 生成颜色预览图
                string previewPath = CreateColorPreview(r, g, b, a);

                // 主要结果 - RGB格式
                results.Add(new Result
                {
                    Title = $"{colorType}: {rgbOutput}",
                    SubTitle = $"点击复制 {hexInput} 的{colorType}值",
                    IcoPath = previewPath, // 使用颜色预览图
                    Action = _ =>
                    {
                        CopyToClipboard(rgbOutput);
                        _context.API.ShowMsg("复制成功", $"{colorType}值 {rgbOutput} 已复制到剪贴板", previewPath);
                        return true;
                    }
                });

                // 备用结果 - 逗号分隔格式
                string commaOutput = a.HasValue ? 
                    $"{r}, {g}, {b}, {a.Value:F2}" : 
                    $"{r}, {g}, {b}";

                results.Add(new Result
                {
                    Title = $"{colorType}(逗号分隔): {commaOutput}",
                    SubTitle = $"点击复制逗号分隔的{colorType}值",
                    IcoPath = previewPath, // 使用颜色预览图
                    Action = _ =>
                    {
                        CopyToClipboard(commaOutput);
                        _context.API.ShowMsg("复制成功", $"{colorType}值 {commaOutput} 已复制到剪贴板", previewPath);
                        return true;
                    }
                });

                // CSS格式结果
                string cssOutput = a.HasValue ? 
                    $"rgba({r}, {g}, {b}, {a.Value:F2})" : 
                    $"rgb({r}, {g}, {b})";

                results.Add(new Result
                {
                    Title = $"CSS格式: {cssOutput}",
                    SubTitle = "点击复制CSS格式的颜色值",
                    IcoPath = previewPath, // 使用颜色预览图
                    Action = _ =>
                    {
                        CopyToClipboard(cssOutput);
                        _context.API.ShowMsg("复制成功", $"CSS值 {cssOutput} 已复制到剪贴板", previewPath);
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

        private Tuple<int, int, int, float?> ParseHexInput(string input)
        {
            try
            {
                // 移除#号并统一处理
                string hex = input.Replace("#", "").ToUpper();

                // 根据长度处理不同的HEX格式
                switch (hex.Length)
                {
                    case 3: // #RGB
                        return ParseShortHex(hex, false);
                    
                    case 4: // #RGBA
                        return ParseShortHex(hex, true);
                    
                    case 6: // #RRGGBB
                        return ParseFullHex(hex, false);
                    
                    case 8: // #RRGGBBAA
                        return ParseFullHex(hex, true);
                    
                    default:
                        return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Tuple<int, int, int, float?> ParseShortHex(string hex, bool hasAlpha)
        {
            // 3位或4位HEX：每个字符重复一次
            if (hex.Length != (hasAlpha ? 4 : 3))
                return null;

            try
            {
                int r = Convert.ToInt32(hex[0].ToString() + hex[0].ToString(), 16);
                int g = Convert.ToInt32(hex[1].ToString() + hex[1].ToString(), 16);
                int b = Convert.ToInt32(hex[2].ToString() + hex[2].ToString(), 16);
                float? a = hasAlpha ? Convert.ToInt32(hex[3].ToString() + hex[3].ToString(), 16) / 255f : (float?)null;

                return new Tuple<int, int, int, float?>(r, g, b, a);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Tuple<int, int, int, float?> ParseFullHex(string hex, bool hasAlpha)
        {
            // 6位或8位HEX：每两个字符一组
            if (hex.Length != (hasAlpha ? 8 : 6))
                return null;

            try
            {
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                float? a = hasAlpha ? Convert.ToInt32(hex.Substring(6, 2), 16) / 255f : (float?)null;

                return new Tuple<int, int, int, float?>(r, g, b, a);
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

                // 使用WPF的RenderTargetBitmap创建图像
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    // 创建颜色画笔
                    byte alpha = a.HasValue ? (byte)(a.Value * 255) : (byte)255;
                    var color = Color.FromArgb(alpha, (byte)r, (byte)g, (byte)b);
                    var brush = new SolidColorBrush(color);
                    
                    // 绘制矩形
                    context.DrawRectangle(brush, null, new Rect(0, 0, IMG_SIZE, IMG_SIZE));
                }

                // 渲染到位图
                var bitmap = new RenderTargetBitmap(IMG_SIZE, IMG_SIZE, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(visual);

                // 保存为PNG文件
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(stream);
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
                // 使用 WPF 的剪贴板 API
                System.Windows.Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                _context.API.ShowMsg("复制失败", $"无法复制到剪贴板: {ex.Message}", "Images/icon.png");
            }
        }
    }
}