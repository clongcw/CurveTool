#region

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

#endregion

namespace CurveTool.ViewModel;

public partial class XlsxToExpViewModel : ObservableObject
{
    [ObservableProperty] private string _excelPath = Environment.CurrentDirectory + @"\Exp\";
    [ObservableProperty] private string _expPath = Environment.CurrentDirectory + @"\Exp\";
    [ObservableProperty] private string _newExpPath = Environment.CurrentDirectory + @"\Exp\";

    public XlsxToExpViewModel()
    {
        // 检查文件夹是否存在
        if (!Directory.Exists(Environment.CurrentDirectory + @"\Exp\"))
            // 如果文件夹不存在，创建它
            Directory.CreateDirectory(Environment.CurrentDirectory + @"\Exp\");
    }

    #region Excel

    [RelayCommand]
    public void OpenExcel()
    {
        var openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true) ExcelPath = openFileDialog.FileName;
    }

    [RelayCommand]
    public void OpenExp()
    {
        var openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true) ExpPath = openFileDialog.FileName;
    }

    [RelayCommand]
    public void Export()
    {
        //配置文件目录
        string dict = null;
        var sfd = new SaveFileDialog
        {
            Title = "请选择导出配置文件...", //对话框标题
            Filter = "Exp Files(*.exp)|*.exp", //文件格式过滤器
            FilterIndex = 1, //默认选中的过滤器
            FileName = "newExp", //默认文件名
            DefaultExt = "exp", //默认扩展名
            InitialDirectory = dict, //指定初始的目录
            OverwritePrompt = true, //文件已存在警告
            AddExtension = true //若用户省略扩展名将自动添加扩展名
        };
        if (sfd.ShowDialog() == true) NewExpPath = sfd.FileName;
    }


    [RelayCommand]
    public void GenerateNewExpFile()
    {
        if (File.Exists(NewExpPath))
        {
            File.Delete(NewExpPath);
            Console.WriteLine("文件已删除。");
        }

        // 指定要读取的工作表名称
        List<string> listSheet = new List<string> { "Raw Data", "Calibrated Data", "Amplification Data" };
        string json = File.ReadAllText(ExpPath);
        foreach (string sheetName in listSheet)
            try
            {
                // 使用FileStream打开Excel文件
                using (FileStream fs = new FileStream(ExcelPath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // 使用XSSFWorkbook打开.xlsx文件（如果是.xls文件，使用HSSFWorkbook）
                    IWorkbook workbook = new XSSFWorkbook(fs); 

                    // 获取指定工作表
                    ISheet sheet = workbook.GetSheet(sheetName);

                    if (sheet != null)
                    {
                        var newDataList = new List<string>();

                        // 获取指定sheet的内容并添加到list集合
                        // 遍历列
                        for (int columnIndex = 1; columnIndex < sheet.GetRow(0).LastCellNum; columnIndex++)
                        {
                            string data = "-1," + sheet.GetRow(0).GetCell(columnIndex) + ",";
                            // 遍历行
                            for (int row = 1; row <= sheet.LastRowNum; row++)
                            {
                                IRow currentRow = sheet.GetRow(row);

                                if (currentRow != null)
                                {
                                    ICell cell = currentRow.GetCell(columnIndex);

                                    if (cell != null)
                                    {
                                        // 获取单元格的值（假设它是文本）
                                        string cellValue = Convert.ToDouble(cell.ToString()).ToString("F3") + " ";
                                        data += cellValue;
                                    }
                                }
                            }

                            newDataList.Add(data.TrimEnd()); //删除最后一个空格
                        }

                        // 使用正则表达式查找目标 JSON 结构
                        string pattern = $@"\{{[^{{}}]*""Name"":\s*""{sheetName}""[^{{}}]*\}}";
                        Match match = Regex.Match(json, pattern, RegexOptions.Singleline);
                        if (match.Success)
                        {
                            // 获取匹配的 JSON 结构
                            var jsonStructure = match.Value;

                            // 解析 JSON 数据
                            var jObject = JObject.Parse(jsonStructure);

                            // 查找 "DataList" 数组并替换内容
                            var dataList = (JArray)jObject["DataList"];

                            // 替换 "DataList" 的内容
                            dataList.ReplaceAll(newDataList.Select(item => new JValue(item)));

                            // 更新 JSON 结构
                            var updatedJsonStructure = jObject.ToString(Formatting.Indented);

                            // 替换原始文本中的 JSON 结构
                            json = Regex.Replace(json, pattern, updatedJsonStructure);
                        }
                        else
                        {
                            Console.WriteLine("未找到目标结构.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("工作表 " + sheetName + " 不存在.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxX.Show(Application.Current.MainWindow, ex.Message, "错误提示", MessageBoxButton.OK,
                    MessageBoxIcon.Error, DefaultButton.YesOK);
                return;
            }

        // 保存更新后的文本文件
        File.WriteAllTextAsync(NewExpPath, json);
        MessageBoxX.Show(Application.Current.MainWindow, "新Exp文件已生成！", "提示", MessageBoxButton.OK,
            MessageBoxIcon.Success, DefaultButton.YesOK, 5);
    }

    #endregion
}