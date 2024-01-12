#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Common;
using Common.Enum;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using device;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using Panuon.WPF.UI;
using SExperiment;
using SExperiment.ExperimentResult;
using SProject;
using SProject.Program;
using Application = System.Windows.Application;
using LineStyle = OxyPlot.LineStyle;
using MessageBoxIcon = Panuon.WPF.UI.MessageBoxIcon;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

#endregion

namespace CurveTool.ViewModel;

public partial class XlsxToPcrViewModel : ObservableObject
{
    //[ObservableProperty] private string _excelPath = Environment.CurrentDirectory + @"\Pcr\";
    [ObservableProperty] private string _excelPath = @"C:\Users\63214\Desktop\666.xlsx";
    [ObservableProperty] private string _excelFolderPath = @"C:\Users\63214\Desktop\Excel";
    [ObservableProperty] private string _pcrFolderPath = @"C:\Users\63214\Desktop\Excel";
    [ObservableProperty] private string _newPcrFolderPath = @"C:\Users\63214\Desktop\Excel";
    [ObservableProperty] private Experiment _experiment;
    [ObservableProperty] private double _ctBefore = 0;
    [ObservableProperty] private double _ctAfter = 0;
    [ObservableProperty] private double _schedule = 0;

    [ObservableProperty] private ObservableCollection<List<string>> _listExcelRawCurves = new();

    [ObservableProperty] private ObservableCollection<List<string>> _listExcelAmpCurves = new();

    [ObservableProperty] private ObservableCollection<List<string>> _listPcrRawCurves = new();

    [ObservableProperty] private ObservableCollection<List<string>> _listPcrAmpCurves = new();

    [ObservableProperty] private List<string> _selectedCurve;

    [ObservableProperty] private PlotModel _pcrRawCurveChangeBefore;
    [ObservableProperty] private PlotModel _pcrRawCurveChangeAfter;
    [ObservableProperty] private PlotModel _pcrAmplificationCurveChangeBefore;
    [ObservableProperty] private PlotModel _pcrAmplificationCurveChangeAfter;

    [ObservableProperty] private string _newPcrPath = Environment.CurrentDirectory + @"\Pcr\02.pcr";

    //[ObservableProperty] private string _pcrPath = Environment.CurrentDirectory + @"\Pcr\";
    [ObservableProperty] private string _pcrPath = @"C:\Users\63214\Desktop\22.pcr";
    [ObservableProperty] private List<double>[,] _rawData = new List<double>[96, 6];
    [ObservableProperty] private List<double>[,] _meltrawData = new List<double>[96, 6];
    //[ObservableProperty] private AsyncRelayCommand _batchGenerateNewPcrFileCommand;


    public XlsxToPcrViewModel()
    {
        // 检查文件夹是否存在
        if (!Directory.Exists(Environment.CurrentDirectory + @"\Pcr\"))
            // 如果文件夹不存在，创建它
            Directory.CreateDirectory(Environment.CurrentDirectory + @"\Pcr\");

        #region 初始化曲线

        var list = new ObservableCollection<List<string>> { new() { "666" } };
        PlotModel rawchangebefore;
        InitPcrCurve(out rawchangebefore, "调整前原始曲线", list[0]);
        PcrRawCurveChangeBefore = rawchangebefore;


        PlotModel rawchangebefore2;
        InitPcrCurve(out rawchangebefore2, "调整后原始曲线", list[0]);
        PcrRawCurveChangeAfter = rawchangebefore2;

        #endregion

        //BatchGenerateNewPcrFileCommand = new AsyncRelayCommand(BatchGenerateNewPcrFile);
    }

    #region 选择文件与导出

    [RelayCommand]
    public void OpenExcel()
    {
        var openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true) ExcelPath = openFileDialog.FileName;
    }

    [RelayCommand]
    public void OpenExcelFolder()
    {
        var dialog = new FolderBrowserDialog();
        dialog.Description = "请选择文件路径";
        if (dialog.ShowDialog() == DialogResult.OK) ExcelFolderPath = dialog.SelectedPath;
    }

    [RelayCommand]
    public void OpenPcrFolder()
    {
        var dialog = new FolderBrowserDialog();
        dialog.Description = "请选择Pcr文件路径";
        if (dialog.ShowDialog() == DialogResult.OK) PcrFolderPath = dialog.SelectedPath;
    }

    [RelayCommand]
    public void OpenNewPcrFolder()
    {
        var dialog = new FolderBrowserDialog();
        dialog.Description = "请选择新Pcr文件路径";
        if (dialog.ShowDialog() == DialogResult.OK) NewPcrFolderPath = dialog.SelectedPath;
    }

    [RelayCommand]
    public void OpenPcr()
    {
        var openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true) PcrPath = openFileDialog.FileName;
    }

    [RelayCommand]
    public void Export()
    {
        //配置文件目录
        string dict = null;
        var sfd = new SaveFileDialog
        {
            Title = "请选择导出配置文件...", //对话框标题
            Filter = "Pcr Files(*.pcr)|*.pcr", //文件格式过滤器
            FilterIndex = 1, //默认选中的过滤器
            FileName = "newPcr", //默认文件名
            DefaultExt = "pcr", //默认扩展名
            InitialDirectory = dict, //指定初始的目录
            OverwritePrompt = true, //文件已存在警告
            AddExtension = true //若用户省略扩展名将自动添加扩展名
        };
        if (sfd.ShowDialog() == true) NewPcrPath = sfd.FileName;
    }

    #endregion

    [RelayCommand]
    public async Task BatchGenerateNewPcrFile()
    {
        await Task.Run(async () =>
        {
            // 检查文件夹是否存在
            if (Directory.Exists(ExcelFolderPath))
            {
                try
                {
                    // 获取文件夹中的所有.xlsx文件
                    var xlsxFiles = Directory.GetFiles(ExcelFolderPath, "*.xlsx");
                    for (var q = 0; q < xlsxFiles.Length; q++)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 在UI线程上更新CollectionView的SourceCollection
                            ListPcrAmpCurves.Clear();
                            ListExcelAmpCurves.Clear();
                            ListPcrRawCurves.Clear();
                            ListExcelRawCurves.Clear();
                        });


                        //获取原始数据字节数组,及原始曲线
                        await GetRawData(false, false);
                        //获取调整前扩增曲线
                        await Task.Run(async () =>
                        {
                            foreach (var well in Experiment.Wells)
                                if (well.Sample != null)
                                    await AMPCurveCal(well, well.Project, false, "调整前");
                        });

                        //读取excel
                        //await ReadExcelData(xlsxFiles[q]);
                        if (await ReadExcelData(xlsxFiles[q], "原始曲线") == 1)
                        {
                            await Task.Run(async () =>
                            {
                                foreach (var well in Experiment.Wells)
                                    if (well.Sample != null)
                                        for (var i = 0; i < ListExcelRawCurves.Count; i++)
                                        for (var j = 0; j < well.Targets.Count; j++)
                                            if (well.CellName == ListExcelRawCurves[i][0] &&
                                                well.Targets[j].Dye == ListExcelRawCurves[i][2])
                                            {
                                                well.Sample.SampleName = ListExcelRawCurves[i][1]; //替换样本名称
                                                var wellIndex = GetWellIndex(ListExcelRawCurves[i][0]) - 1;
                                                List<double> sublist = new();
                                                for (int k = 4; k < ListExcelRawCurves[i].Count; k++)
                                                    sublist.Add(Convert.ToDouble(ListExcelRawCurves[i][k]));

                                                var target = well.Targets[j].ChannelNo - 1;
                                                sublist.Sort();
                                                RawData[wellIndex, target] = sublist;
                                            }


                                var list = Experiment.Device.RawData;
                                for (var i = 0; i < list.Count; i++)
                                {
                                    FluorescenceData[,] array2 = list[i];
                                    for (var k = 0; k < 96; k++)
                                    for (var l = 0; l < 6; l++)
                                        if (Experiment.Device.Calibration.Count == 6)
                                        {
                                            var num = RawData[k, l][i];
                                            var array3 = Experiment.Device.Calibration[l];
                                            if (array3.Count() == 98 && num > -50.0 && array3[k] != 255 &&
                                                array3[k] != 0 &&
                                                Experiment.Device.DarkCurrent[l] != 255)
                                            {
                                                var num2 = array3[k] / 100.0;
                                                var num3 = (array3[96] * 256 + array3[97]) / 1000.0;
                                                num = num / num3 / num2 + Experiment.Device.DarkCurrent[l] / 10.0;
                                            }

                                            array2[k, l] = DeviceUtility.Conversion(num);
                                            list[i][k, l] = array2[k, l];
                                        }
                                }
                            });
                        }


                        if (File.Exists(ChangeFileExtension(xlsxFiles[q], ".pcr")))
                        {
                            File.Delete(ChangeFileExtension(xlsxFiles[q], ".pcr"));
                            Console.WriteLine("文件已删除。");
                        }

                        Experiment.Save(ChangeFileExtension(xlsxFiles[q], ".pcr"));

                        Schedule = (double)(q + 1) / xlsxFiles.Length * 100d;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxX.Show(Application.Current.MainWindow, "新Pcr文件已生成！", "提示", MessageBoxButton.OK,
                            MessageBoxIcon.Success, DefaultButton.YesOK, 5);
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxX.Show(Application.Current.MainWindow, ex.Message, "错误提示", MessageBoxButton.OK,
                            MessageBoxIcon.Error, DefaultButton.YesOK);
                    });
                    return;
                }
            }
            else
            {
                Console.WriteLine("指定的文件夹路径不存在.");
            }
        });
    }

    public string ChangeFileExtension(string filePath, string newExtension)
    {
        if (File.Exists(filePath))
        {
            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var newFilePath = Path.Combine(directory, fileName + newExtension);
            return newFilePath;
        }
        else
        {
            Console.WriteLine("文件路径不存在.");
            return filePath;
        }
    }

    [RelayCommand]
    public async Task GenerateNewPcrFile()
    {
        List<FluorescenceData[,]> list;
        try
        {
            ListPcrAmpCurves.Clear();
            ListExcelAmpCurves.Clear();
            ListPcrRawCurves.Clear();
            ListExcelRawCurves.Clear();

            #region 获取原始曲线数据并替换

            //获取原始数据字节数组,及原始曲线
            await GetRawData(false, false);
            //获取调整前扩增曲线
            foreach (var well in Experiment.Wells)
                if (well.Sample != null)
                    await AMPCurveCal(well, well.Project, false, "调整前");


            //读取excel
            //ReadExcelData(ExcelPath);
            if (await ReadExcelData(ExcelPath, "原始曲线") == 1)
            {
                if (File.Exists(NewPcrPath))
                {
                    File.Delete(NewPcrPath);
                    Console.WriteLine("文件已删除。");
                }

                foreach (var well in Experiment.Wells)
                {
                    if (well.Sample != null)
                    {
                        for (var i = 0; i < ListExcelRawCurves.Count; i++)
                        {
                            for (var j = 0; j < well.Targets.Count; j++)
                            {
                                if (well.CellName == ListExcelRawCurves[i][0] &&
                                    well.Targets[j].Dye == ListExcelRawCurves[i][2])
                                {
                                    well.Sample.SampleName = ListExcelRawCurves[i][1]; //替换样本名称

                                    var wellIndex = GetWellIndex(ListExcelRawCurves[i][0]) - 1;
                                    List<double> sublist = new();
                                    for (int k = 4; k < ListExcelRawCurves[i].Count; k++)
                                    {
                                        sublist.Add(Convert.ToDouble(ListExcelRawCurves[i][k]));
                                    }

                                    var target = well.Targets[j].ChannelNo - 1;
                                    //sublist.Sort();
                                    RawData[wellIndex, target] = sublist;
                                }
                            }
                        }
                    }
                }

                list = Experiment.Device.RawData;
                for (var i = 0; i < list.Count; i++)
                {
                    FluorescenceData[,] array2 = list[i];
                    for (var k = 0; k < 96; k++)
                    {
                        for (var l = 0; l < 6; l++)
                        {
                            if (Experiment.Device.Calibration.Count == 6)
                            {
                                double num = RawData[k, l][i];
                                byte[] array3 = Experiment.Device.Calibration[l];
                                if (array3.Count() == 98 && num > -50.0 && array3[k] != 255 && array3[k] != 0 &&
                                    Experiment.Device.DarkCurrent[l] != 255)
                                {
                                    var num2 = array3[k] / 100.0;
                                    var num3 = (array3[96] * 256 + array3[97]) / 1000.0;
                                    num = num / num3 / num2 + Experiment.Device.DarkCurrent[l] / 10.0;
                                }

                                array2[k, l] = DeviceUtility.Conversion(num);
                                list[i][k, l] = array2[k, l];
                            }
                        }
                    }
                }
            }

            #endregion

            #region 获取溶解曲线并替换

            ListExcelRawCurves.Clear();
            //获取原始数据字节数组,及原始曲线
            await GetRawData(false, true);


            //读取excel
            if (await ReadExcelData(ExcelPath, "熔解曲线") == 1)
            {
                if (File.Exists(NewPcrPath))
                {
                    File.Delete(NewPcrPath);
                    Console.WriteLine("文件已删除。");
                }

                foreach (var well in Experiment.Wells)
                {
                    if (well.Sample != null)
                    {
                        for (var i = 0; i < ListExcelRawCurves.Count; i++)
                        {
                            for (var j = 0; j < well.Targets.Count; j++)
                            {
                                if (well.CellName == ListExcelRawCurves[i][0] &&
                                    well.Targets[j].Dye == ListExcelRawCurves[i][2])
                                {
                                    well.Sample.SampleName = ListExcelRawCurves[i][1]; //替换样本名称

                                    var wellIndex = GetWellIndex(ListExcelRawCurves[i][0]) - 1;
                                    List<double> sublist = new();
                                    for (int k = 4; k < ListExcelRawCurves[i].Count; k++)
                                    {
                                        sublist.Add(Convert.ToDouble(ListExcelRawCurves[i][k]));
                                    }

                                    var target = well.Targets[j].ChannelNo - 1;
                                    //sublist.Sort();
                                    MeltrawData[wellIndex, target] = sublist;
                                }
                            }
                        }
                    }
                }

                list = Experiment.Device.RawMeltingData;
                for (var i = 0; i < 151; i++)
                {
                    FluorescenceData[,] array2 = list[i];
                    for (var k = 0; k < 96; k++)
                    {
                        for (var l = 0; l < 6; l++)
                        {
                            BadPoint badPoint =
                                this.Experiment.BadPoints[k.ToString("00") + (l + 1).ToString()] as BadPoint;

                            if (badPoint != null)
                            {
                                if (true)
                                {
                                    Func<int, bool> qq90 = null;
                                    Func<int, bool> qq91 = null;
                                    IEnumerable<int> rawMeltPoints = badPoint.RawMeltPoints;
                                    Func<int, bool> func;
                                    if ((func = qq90) == null) func = qq90 = (int s) => s == i;

                                    if (rawMeltPoints.Where(func).Count<int>() != 0)
                                    {
                                        //num = -100.0;
                                    }
                                }
                            }

                            if (Experiment.Device.Calibration.Count == 6)
                            {
                                double num = MeltrawData[k, l][i];
                                byte[] array3 = Experiment.Device.Calibration[l];
                                if (array3.Count() == 98 && num > -50.0 && array3[k] != 255 && array3[k] != 0 &&
                                    Experiment.Device.DarkCurrent[l] != 255)
                                {
                                    var num2 = array3[k] / 100.0;
                                    var num3 = (array3[96] * 256 + array3[97]) / 1000.0;
                                    num = num / num3 / num2 + Experiment.Device.DarkCurrent[l] / 10.0;
                                }

                                array2[k, l] = DeviceUtility.Conversion(num);
                                list[i][k, l] = array2[k, l];
                            }
                        }
                    }
                }
            }

            #endregion


            Experiment.Save(NewPcrPath);
            MessageBoxX.Show(Application.Current.MainWindow, "新Pcr文件已生成！", "提示", MessageBoxButton.OK,
                MessageBoxIcon.Success, DefaultButton.YesOK, 5);

            #region 获取调整后的扩增曲线

            Experiment = Experiment.Load(NewPcrPath, false);
            RawDataToRawCurve(false, true);
            //获取调整前扩增曲线
            foreach (var well in Experiment.Wells)
            {
                if (well.Sample != null)
                {
                    AMPCurveCal(well, well.Project, false, "调整后");
                }
            }

            #endregion
        }
        catch (Exception ex)
        {
            MessageBoxX.Show(Application.Current.MainWindow, ex.Message, "错误提示", MessageBoxButton.OK,
                MessageBoxIcon.Error, DefaultButton.YesOK);
            return;
        }
    }

    private async Task MeltingRawCurveCal(List<double>[,] RawData, bool RealtimeCal)
    {
        double num = double.NegativeInfinity;
        double num2 = double.PositiveInfinity;
        foreach (Well well in this.Experiment.CurrentSubset.Wells)
        {
            if (well.Sample != null)
            {
                foreach (SampleTargetItem sampleTargetItem in well.Sample.Items)
                {
                    if (sampleTargetItem.TubeNo == well.MultiTubeID)
                    {
                        int num3 = this.Experiment.Wells.IndexOf(well);
                        int channelNo = sampleTargetItem.ChannelNo;
                        ((MeltingTargetResult)sampleTargetItem.Result).RawMeltingCurve.CurvePoint.Clear();
                        for (int i = 0; i < RawData[num3, channelNo - 1].Count; i++)
                        {
                            Dot dot = new Dot();
                            if (RealtimeCal)
                            {
                                dot.X = this.Experiment.Device.ScanStartTemperature[i];
                            }
                            else
                            {
                                double[,] array = this.Experiment.Device.MeltingTemperatureData[i];
                                dot.X = array[num3, channelNo - 1];
                                if (dot.X > num)
                                {
                                    num = dot.X;
                                }

                                if (dot.X < num2)
                                {
                                    num2 = dot.X;
                                }
                            }

                            dot.Y = RawData[num3, channelNo - 1][i];
                            if ((RealtimeCal || i != 0 || dot.Y <= 2000.0) && dot.Y >= -50.0)
                            {
                                ((MeltingTargetResult)sampleTargetItem.Result).RawMeltingCurve.CurvePoint.Add(dot);
                            }
                        }
                    }
                }
            }
        }

        if (!RealtimeCal)
        {
            MeltingSegment meltingSegment = this.Experiment.Device.CurrentProfile.Programs
                .Where((BaseSegment s) => s is MeltingSegment).Cast<MeltingSegment>().FirstOrDefault<MeltingSegment>();
            double num4 = this.IntervalCal(meltingSegment);
            bool flag = false;
            if (meltingSegment.EndTemp < meltingSegment.StartTemp)
            {
                double endTemp = meltingSegment.EndTemp;
                meltingSegment.EndTemp = meltingSegment.StartTemp;
                meltingSegment.StartTemp = endTemp;
                flag = true;
            }

            if (!double.IsNegativeInfinity(num) && !flag && num - meltingSegment.EndTemp < -0.5)
            {
                meltingSegment.EndTemp = meltingSegment.StartTemp +
                                         (double)((int)Math.Round((num - meltingSegment.StartTemp) / num4)) * num4;
            }

            if (!double.IsPositiveInfinity(num2) && flag && num2 - meltingSegment.StartTemp > 0.5)
            {
                meltingSegment.StartTemp = meltingSegment.EndTemp -
                                           (double)((int)Math.Round((meltingSegment.EndTemp - num2) / num4)) * num4;
            }

            foreach (Well well2 in this.Experiment.CurrentSubset.Wells)
            {
                if (well2.Sample != null)
                {
                    foreach (SampleTargetItem sampleTargetItem2 in well2.Sample.Items)
                    {
                        if (sampleTargetItem2.TubeNo == well2.MultiTubeID)
                        {
                            if (meltingSegment.ScanMode == 0)
                            {
                                ((MeltingTargetResult)sampleTargetItem2.Result).RawMeltingCurve.CurvePoint.RemoveAt(
                                    ((MeltingTargetResult)sampleTargetItem2.Result).RawMeltingCurve.CurvePoint.Count -
                                    1);
                                ((MeltingTargetResult)sampleTargetItem2.Result).RawMeltingCurve.CurvePoint.RemoveAt(0);
                            }

                            if (flag)
                            {
                                ((MeltingTargetResult)sampleTargetItem2.Result).RawMeltingCurve.CurvePoint =
                                    ((MeltingTargetResult)sampleTargetItem2.Result).RawMeltingCurve.CurvePoint
                                    .OrderBy((Dot s) => s.X).ToList<Dot>();
                            }

                            if (((MeltingTargetResult)sampleTargetItem2.Result).RawMeltingCurve.CurvePoint.Count >= 3)
                            {
                                ((MeltingTargetResult)sampleTargetItem2.Result).RawMeltingCurve =
                                    await MathCommon.CurveInsert(
                                        ((MeltingTargetResult)sampleTargetItem2.Result).RawMeltingCurve,
                                        meltingSegment.StartTemp, meltingSegment.EndTemp, num4);
                            }
                        }
                    }
                }
            }
        }
    }

    private double IntervalCal(MeltingSegment meltSeg)
    {
        double num;
        if (meltSeg.ScanMode == 0)
        {
            switch ((int)Math.Round(meltSeg.Rate * 100.0))
            {
                case 1:
                    num = 0.1;
                    break;
                case 2:
                    num = 0.2;
                    break;
                case 3:
                    num = 0.3;
                    break;
                case 4:
                    num = 0.3;
                    break;
                default:
                    num = 0.4;
                    break;
            }
        }
        else
        {
            num = meltSeg.Interval;
        }

        return num;
    }

    public async Task RawDataToRawCurve(bool RealtimeCal, bool isMeltingRawData)
    {
        List<FluorescenceData[,]> list;
        if (isMeltingRawData)
            list = Experiment.Device.RawMeltingData;
        else
            list = Experiment.Device.RawData;

        var array = new List<double>[96, 6];
        for (var n = 0; n < 96; n++)
        for (var j = 0; j < 6; j++)
            array[n, j] = new List<double>();

        int i;
        Func<int, bool> qq90 = null;
        Func<int, bool> qq91 = null;
        int i2;
        for (i = 0; i < list.Count; i = i2 + 1)
        {
            var array2 = list[i];
            for (var k = 0; k < 96; k++)
            for (var l = 0; l < 6; l++)
            {
                var num = MathCommon.Conversion(array2[k, l]);
                var badPoint = Experiment.BadPoints[k.ToString("00") + (l + 1).ToString()] as BadPoint;
                if (badPoint != null)
                {
                    if (isMeltingRawData)
                    {
                        IEnumerable<int> rawMeltPoints = badPoint.RawMeltPoints;
                        Func<int, bool> func;
                        if ((func = qq90) == null) func = qq90 = (int s) => s == i;

                        if (rawMeltPoints.Where(func).Count<int>() != 0) num = -100.0;
                    }
                    else
                    {
                        IEnumerable<int> rawPoints = badPoint.RawPoints;
                        Func<int, bool> func2;
                        if ((func2 = qq91) == null) func2 = qq91 = (int s) => s == i;

                        if (rawPoints.Where(func2).Count<int>() != 0) num = -100.0;
                    }
                }

                if (Experiment.Device.Calibration.Count == 6)
                {
                    var array3 = Experiment.Device.Calibration[l];
                    if (array3.Count<byte>() == 98 && num > -50.0 && array3[k] != 255 && array3[k] != 0 &&
                        Experiment.Device.DarkCurrent[l] != 255)
                    {
                        var num2 = (double)array3[k] / 100.0;
                        var num3 = (double)((int)array3[96] * 256 + (int)array3[97]) / 1000.0;
                        num = (num - (double)Experiment.Device.DarkCurrent[l] / 10.0) * num2 * num3;
                    }
                }

                if (num > -50.0 && num < 0.1) num = 0.1;

                array[k, l].Add(num);
            }

            i2 = i;
        }

        if (isMeltingRawData)
        {
            this.MeltingRawCurveCal(array, RealtimeCal);
        }
        else
        {
            RawCurveCal(array, RealtimeCal, "调整后");
        }

        if (!RealtimeCal || Experiment.Type == EProjectType.FastCal)
        {
            var curve = new Curve();
            var curve2 = new Curve();
            foreach (var well in Experiment.CurrentSubset.Wells)
                if (well.Sample != null)
                {
                    var crossTalk = Experiment.CurrentSubset.GetSubsetParamter(well.Project).CrossTalk;
                    foreach (var sampleTargetItem in well.Sample.Items)
                        if (sampleTargetItem.TubeNo == well.MultiTubeID)
                        {
                            if (isMeltingRawData)
                            {
                                //curve = ((MeltingTargetResult)sampleTargetItem.Result).RawMeltingCurve;
                            }
                            else
                            {
                                curve = sampleTargetItem.Result.RawCurve;
                            }

                            for (var m = 0; m < curve.CurvePoint.Count; m++)
                                foreach (var crossTalkItem in crossTalk.Items)
                                foreach (var sampleTargetItem2 in well.Sample.Items)
                                    if (sampleTargetItem2.ChannelNo == crossTalkItem.ChannelNo &&
                                        sampleTargetItem2.TubeNo == well.MultiTubeID)
                                    {
                                        if (isMeltingRawData)
                                        {
                                            //curve2 = ((MeltingTargetResult)sampleTargetItem2.Result).RawMeltingCurve;
                                        }
                                        else
                                        {
                                            curve2 = sampleTargetItem2.Result.RawCurve;
                                        }

                                        if (m < curve2.CurvePoint.Count)
                                        {
                                            curve.CurvePoint[m].Y = curve.CurvePoint[m].Y - curve2.CurvePoint[m].Y *
                                                crossTalkItem.Value[sampleTargetItem.ChannelNo - 1] / 100.0;
                                            break;
                                        }
                                    }
                        }
                }
        }
    }

    [RelayCommand]
    public void ChangeCurve()
    {
        //调整前原始曲线
        Task.Run(() =>
        {
            for (var i = 0; i < ListPcrRawCurves.Count; i++)
                if (SelectedCurve[0] == ListPcrRawCurves[i][0] && SelectedCurve[2] == ListPcrRawCurves[i][1])
                {
                    PlotModel rawchangebefore;
                    InitPcrCurve(out rawchangebefore,
                        "调整前原始曲线" + " " + ListPcrRawCurves[i][0] + "-" + ListPcrRawCurves[i][1] + "-" +
                        ListPcrRawCurves[i][2], ListPcrRawCurves[i]);
                    PcrRawCurveChangeBefore = rawchangebefore;

                    var lineSeries = new LineSeries();
                    for (var j = 3; j < ListPcrRawCurves[i].Count; j++)
                        lineSeries.Points.Add(new DataPoint(j - 2, Convert.ToDouble(ListPcrRawCurves[i][j])));

                    PcrRawCurveChangeBefore.Series.Add(lineSeries);
                    PcrRawCurveChangeBefore.InvalidatePlot(true);
                }
        });

        //调整前扩增曲线
        Task.Run(() =>
        {
            for (var i = 0; i < ListPcrAmpCurves.Count; i++)
                if (SelectedCurve[0] == ListPcrAmpCurves[i][0] && SelectedCurve[2] == ListPcrAmpCurves[i][1])
                {
                    PlotModel ampchangebefore;
                    InitPcrCurve(out ampchangebefore,
                        "调整前扩增曲线" + " " + ListPcrAmpCurves[i][0] + "-" + ListPcrAmpCurves[i][1] + "-" +
                        ListPcrAmpCurves[i][2], ListPcrAmpCurves[i]);
                    PcrAmplificationCurveChangeBefore = ampchangebefore;

                    var lineSeriesAmp = new LineSeries();
                    for (var j = 3; j < ListPcrAmpCurves[i].Count; j++)
                        lineSeriesAmp.Points.Add(new DataPoint(j - 2, Convert.ToDouble(ListPcrAmpCurves[i][j])));

                    PcrAmplificationCurveChangeBefore.Series.Add(lineSeriesAmp);


                    foreach (var well in Experiment.Wells)
                        if (well.Sample != null)
                            for (var l = 0; l < well.Project.BasicOption.Items.Count; l++)
                                if (well.Project.BasicOption.Items[l].TargetName == ListPcrAmpCurves[i][2])
                                {
                                    var lineSeriesCt = new LineSeries();
                                    for (var k = 1; k < 41; k++)
                                        lineSeriesCt.Points.Add(new DataPoint(k,
                                            well.Project.BasicOption.Items[l].Threshold));

                                    PcrAmplificationCurveChangeBefore.Series.Add(lineSeriesCt);
                                    PcrAmplificationCurveChangeBefore.InvalidatePlot(true);

                                    //计算ct值
                                    var curve = new Curve();
                                    for (var j = 3; j < ListPcrAmpCurves[i].Count; j++)
                                        curve.CurvePoint.Add(new Dot()
                                            { X = j - 2, Y = Convert.ToDouble(ListPcrAmpCurves[i][j]) });

                                    CtBefore = MathCommon.Ct_cal(curve,
                                        well.Project.BasicOption.Items[l].Threshold);
                                    CtBefore = double.Parse(CtBefore.ToString("F2"));
                                    return;
                                }
                }
        });

        //调整后原始曲线
        Task.Run(() =>
        {
            for (var i = 0; i < ListExcelRawCurves.Count; i++)
                if (SelectedCurve[0] == ListExcelRawCurves[i][0] && SelectedCurve[2] == ListExcelRawCurves[i][2])
                {
                    PlotModel rawchangebefore;
                    InitPcrCurve(out rawchangebefore,
                        "调整后原始曲线" + " " + ListExcelRawCurves[i][0] + "-" + ListExcelRawCurves[i][2] + "-" +
                        ListExcelRawCurves[i][3],
                        ListExcelRawCurves[i]);
                    PcrRawCurveChangeAfter = rawchangebefore;

                    var lineSeries = new LineSeries();
                    for (var j = 4; j < ListExcelRawCurves[i].Count; j++)
                        lineSeries.Points.Add(new DataPoint(j - 2, Convert.ToDouble(ListExcelRawCurves[i][j])));

                    PcrRawCurveChangeAfter.Series.Add(lineSeries);
                    PcrRawCurveChangeAfter.InvalidatePlot(true);
                }
        });


        //调整后扩增曲线
        Task.Run(() =>
        {
            for (var i = 0; i < ListExcelAmpCurves.Count; i++)
                if (SelectedCurve[0] == ListExcelAmpCurves[i][0] && SelectedCurve[2] == ListExcelAmpCurves[i][1])
                {
                    PlotModel ampchangeafter;
                    InitPcrCurve(out ampchangeafter,
                        "调整后扩增曲线" + " " + ListExcelAmpCurves[i][0] + "-" + ListExcelAmpCurves[i][1] + "-" +
                        ListExcelAmpCurves[i][2],
                        ListExcelAmpCurves[i]);
                    PcrAmplificationCurveChangeAfter = ampchangeafter;

                    var lineSeriesAmp = new LineSeries();
                    for (var j = 3; j < ListExcelAmpCurves[i].Count; j++)
                        lineSeriesAmp.Points.Add(new DataPoint(j - 2, Convert.ToDouble(ListExcelAmpCurves[i][j])));

                    PcrAmplificationCurveChangeAfter.Series.Add(lineSeriesAmp);

                    foreach (var well in Experiment.Wells)
                        if (well.Sample != null)
                            for (var l = 0; l < well.Project.BasicOption.Items.Count; l++)
                                if (well.Project.BasicOption.Items[l].TargetName == ListExcelAmpCurves[i][2])
                                {
                                    var lineSeriesCt = new LineSeries();
                                    for (var k = 1; k < 41; k++)
                                        lineSeriesCt.Points.Add(new DataPoint(k,
                                            well.Project.BasicOption.Items[l].Threshold));

                                    PcrAmplificationCurveChangeAfter.Series.Add(lineSeriesCt);
                                    PcrAmplificationCurveChangeAfter.InvalidatePlot(true);

                                    //计算ct值
                                    var curve = new Curve();
                                    for (var j = 3; j < ListExcelAmpCurves[i].Count; j++)
                                        curve.CurvePoint.Add(new Dot()
                                            { X = j - 2, Y = Convert.ToDouble(ListExcelAmpCurves[i][j]) });

                                    CtAfter = MathCommon.Ct_cal(curve, well.Project.BasicOption.Items[l].Threshold);
                                    CtAfter = double.Parse(CtAfter.ToString("F2"));

                                    return;
                                }
                }
        });
    }

    public async Task AMPCurveCal(Well w, Project prj, bool realtime, string change)
    {
        using (IEnumerator<SampleTargetItem> enumerator = w.Sample.Items.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                var ch = enumerator.Current;
                if (ch.TubeNo == w.MultiTubeID)
                {
                    var curve = new Curve();
                    if (ch.Result.RawCurve.CurvePoint.Count >= 3)
                    {
                        Func<SampleTargetItem, bool> qw90 = null;
                        for (var i = 0; i < ch.Result.RawCurve.CurvePoint.Count; i++)
                        {
                            var dot = new Dot(ch.Result.RawCurve.CurvePoint[i]);
                            if (Experiment.ExperimentSetting.AMPAlgorithm == EAMPAlgorithm.Subtraction &&
                                Experiment.ExperimentSetting.RoxCheck)
                            {
                                IEnumerable<SampleTargetItem> items = w.Sample.Items;
                                Func<SampleTargetItem, bool> func;
                                if ((func = qw90) == null)
                                    func = qw90 = (SampleTargetItem s) => s.TubeNo == ch.TubeNo && s.ChannelNo == 3;

                                var sampleTargetItem =
                                    items.Where(func).FirstOrDefault<SampleTargetItem>();
                                if (sampleTargetItem != null &&
                                    i < sampleTargetItem.Result.RawCurve.CurvePoint.Count)
                                    dot.Y /= sampleTargetItem.Result.RawCurve.CurvePoint[i].Y;
                            }

                            curve.CurvePoint.Add(dot);
                        }

                        var item = Experiment.CurrentSubset.GetSubsetParamter(prj).BasicOption
                            .GetItem(ch.TubeNo, ch.ChannelNo);
                        int num;
                        int num2;
                        EOptionzationMode eoptionzationMode;
                        if (realtime)
                        {
                            if (Experiment.Type == EProjectType.FastCal)
                            {
                                num = item.BeginBaseline;
                                num2 = item.EndBaseline;
                                eoptionzationMode = item.OptimizationMode;
                            }
                            else
                            {
                                num = 6;
                                num2 = 12;
                                eoptionzationMode = EOptionzationMode.OPTIMIZATION_AUTO;
                            }
                        }
                        else
                        {
                            if (item.EndBaseline > curve.CurvePoint.Count)
                            {
                                item.BeginBaseline = 2;
                                item.EndBaseline = curve.CurvePoint.Count;
                                item.OptimizationMode = EOptionzationMode.OPTIMIZATION_MANUAL;
                            }

                            num = item.BeginBaseline;
                            num2 = item.EndBaseline;
                            eoptionzationMode = item.OptimizationMode;
                        }

                        if (Experiment.Type != EProjectType.FastCal || curve.CurvePoint.Count >= num2)
                        {
                            if (item.DigitalFilter == EDigitalFilter.High)
                                for (var j = 0; j < 3; j++)
                                    curve = await MathCommon.CurveSmooth(curve);

                            if (Experiment.Type == EProjectType.TQ)
                            {
                                var num3 = (curve.CurvePoint[num - 1].Y + curve.CurvePoint[num2 - 1].Y) / 2.0;
                                for (var k = 0; k < curve.CurvePoint.Count; k++)
                                    curve.CurvePoint[k].Y = 2.0 * num3 - curve.CurvePoint[k].Y;
                            }

                            var curveParameter =
                                (Subset.CurveParameter)Experiment.CurrentSubset.CurveParameters[
                                    w.ID.ToString("00") + ch.ChannelNo.ToString()];
                            var flag = false;
                            if (!realtime && curveParameter != null && curveParameter.Use)
                            {
                                num = curveParameter.BeginBaseline;
                                num2 = curveParameter.EndBaseline;
                                if (eoptionzationMode != EOptionzationMode.OPTIMIZATION_NA)
                                    await MathCommon.Baseline(num, num2, curve);

                                flag = true;
                            }

                            if (eoptionzationMode == EOptionzationMode.OPTIMIZATION_MANUAL && !flag)
                                await MathCommon.Baseline(num, num2, curve);

                            if (eoptionzationMode == EOptionzationMode.OPTIMIZATION_AUTO && !flag)
                            {
                                var num4 = Math.Pow(2.0, 0.04);
                                int num5;
                                if (Experiment.Type == EProjectType.IA)
                                    num5 = num;
                                else
                                    num5 = num2;

                                num2 = 0;
                                for (var l = num5; l < curve.CurvePoint.Count - 2; l++)
                                    if (curve.CurvePoint[l].Y / curve.CurvePoint[l - 1].Y > num4 &&
                                        curve.CurvePoint[l + 1].Y / curve.CurvePoint[l].Y > num4 &&
                                        curve.CurvePoint[l + 2].Y / curve.CurvePoint[l + 1].Y > num4 &&
                                        curve.CurvePoint[l].Y - curve.CurvePoint[l - 1].Y > 1.0 &&
                                        curve.CurvePoint[l + 1].Y - curve.CurvePoint[l].Y > 1.0 &&
                                        curve.CurvePoint[l + 2].Y - curve.CurvePoint[l + 1].Y > 1.0)
                                    {
                                        int num6;
                                        if (l - 5 < num5)
                                            num6 = num5;
                                        else
                                            num6 = l - 5;

                                        if (num6 < 2) num6 = 2;

                                        var num7 = curve.CurvePoint[num6 - 1].Y / curve.CurvePoint[num6 - 2].Y;
                                        if (curve.CurvePoint[l].Y / curve.CurvePoint[l - 1].Y - num7 > num4 - 1.0 &&
                                            curve.CurvePoint[l + 1].Y / curve.CurvePoint[l].Y - num7 > num4 - 1.0 &&
                                            curve.CurvePoint[l + 2].Y / curve.CurvePoint[l + 1].Y - num7 >
                                            num4 - 1.0)
                                        {
                                            if (curve.CurvePoint[l + 2].Y / curve.CurvePoint[l + 1].Y >
                                                Math.Pow(2.0, 0.1) &&
                                                curve.CurvePoint[l + 2].Y - curve.CurvePoint[l + 1].Y > 2.5)
                                                num2 = l + 1 - 5;
                                            else
                                                num2 = l + 1 - 8;

                                            if (num2 <= 1) num2 = 1;

                                            if (l >= 5 && l < 12) num2 = l + 1 - 3;

                                            if (l > 1 && l < 5) num2 = l + 1 - 2;

                                            if (l == 1)
                                            {
                                                num2 = 1;
                                                break;
                                            }

                                            break;
                                        }
                                    }

                                if (num2 == 0)
                                {
                                    var count = curve.CurvePoint.Count;
                                    if (realtime && Experiment.Type != EProjectType.FastCal)
                                        num2 = 12;
                                    else
                                        num2 = item.EndBaseline;

                                    var flag2 = false;
                                    if (num2 + 9 < count)
                                    {
                                        var list = new List<double>();
                                        for (var m = num2 + 5; m < count; m++)
                                            list.Add((curve.CurvePoint[m - 1].Y - curve.CurvePoint[m - 7].Y) /
                                                     curve.CurvePoint[m - 7].Y);

                                        var num8 = 0.025;
                                        for (var n = 1; n < list.Count; n++)
                                        {
                                            int num9;
                                            if (n < 8)
                                                num9 = 0;
                                            else
                                                num9 = n - 8;

                                            if (list[n] - list[num9] >= num8 &&
                                                (n + 1 >= list.Count || list[n + 1] - list[num9] >= num8) &&
                                                (n + 2 >= list.Count || list[n + 2] - list[num9] >= num8))
                                            {
                                                var num10 = num2 + 5 + n - 1;
                                                if (num10 >= curve.CurvePoint.Count - 2 ||
                                                    (curve.CurvePoint[num10].Y / curve.CurvePoint[num10 - 1].Y >
                                                     1.008 &&
                                                     curve.CurvePoint[num10 + 1].Y / curve.CurvePoint[num10].Y >
                                                     1.008 &&
                                                     curve.CurvePoint[num10 + 2].Y / curve.CurvePoint[num10 + 1].Y >
                                                     1.008 &&
                                                     curve.CurvePoint[num10].Y - curve.CurvePoint[num10 - 1].Y >
                                                     0.3 &&
                                                     curve.CurvePoint[num10 + 1].Y - curve.CurvePoint[num10].Y >
                                                     0.3 && curve.CurvePoint[num10 + 2].Y -
                                                     curve.CurvePoint[num10 + 1].Y > 0.3))
                                                {
                                                    var num11 = num2 + 5 + n - 8;
                                                    if (num11 < 1) num11 = 1;

                                                    num2 = num11;
                                                    flag2 = true;
                                                    break;
                                                }
                                            }
                                        }

                                        if (!flag2) num2 = curve.CurvePoint.Count;
                                    }
                                }

                                if (num2 > item.BeginBaseline)
                                {
                                    num = item.BeginBaseline;
                                }
                                else
                                {
                                    num = num2 - 1;
                                    if (num < 1) num = 1;
                                }

                                await MathCommon.Baseline(num, num2, curve);
                                if (Experiment.Type == EProjectType.IA &&
                                    Experiment.ExperimentSetting.AMPAlgorithm == EAMPAlgorithm.Subtraction)
                                {
                                    var num12 = 0.0;
                                    for (var num13 = num - 1; num13 < num2; num13++)
                                        num12 += curve.CurvePoint[num13].Y;

                                    var num14 = num12 / (double)(num2 - num + 1) + item.Threshold;
                                    var flag3 = true;
                                    for (var num15 = num - 1; num15 < curve.CurvePoint.Count; num15++)
                                        if (curve.CurvePoint[num15].Y > num14)
                                        {
                                            flag3 = false;
                                            break;
                                        }

                                    if (flag3)
                                    {
                                        num2 = curve.CurvePoint.Count;
                                        await MathCommon.Baseline(num, num2, curve);
                                    }
                                }
                            }

                            var num16 = 0.0;
                            for (var num17 = num - 1; num17 < num2; num17++) num16 += curve.CurvePoint[num17].Y;

                            num16 /= (double)(num2 - num + 1);
                            if (curveParameter != null && !curveParameter.Use)
                            {
                                curveParameter.BeginBaseline = num;
                                curveParameter.EndBaseline = num2;
                                if (Experiment.ExperimentSetting.AMPAlgorithm == EAMPAlgorithm.Subtraction &&
                                    ConfigReader.GetInstance().GetBackgroundFluorescenceMethod())
                                    curveParameter.Threshold = num16 * item.Threshold;
                            }

                            switch (Experiment.ExperimentSetting.AMPAlgorithm)
                            {
                                case EAMPAlgorithm.Default:
                                {
                                    for (var num18 = 0; num18 < curve.CurvePoint.Count; num18++)
                                        curve.CurvePoint[num18].Y = Math.Log10(curve.CurvePoint[num18].Y / num16) /
                                                                    Math.Log10(2.0);

                                    num16 = 0.0;
                                    for (var num19 = num - 1; num19 < num2; num19++)
                                        num16 += curve.CurvePoint[num19].Y;

                                    num16 /= (double)(num2 - num + 1);
                                    for (var num20 = 0; num20 < curve.CurvePoint.Count; num20++)
                                        curve.CurvePoint[num20].Y = curve.CurvePoint[num20].Y - num16;

                                    break;
                                }
                                case EAMPAlgorithm.Subtraction:
                                {
                                    for (var num21 = 0; num21 < curve.CurvePoint.Count; num21++)
                                        curve.CurvePoint[num21].Y = curve.CurvePoint[num21].Y - num16;

                                    break;
                                }
                                case EAMPAlgorithm.RelativeLinear:
                                {
                                    for (var num22 = 0; num22 < curve.CurvePoint.Count; num22++)
                                        curve.CurvePoint[num22].Y = curve.CurvePoint[num22].Y / num16 - 1.0;

                                    break;
                                }
                            }

                            if (item.DigitalFilter == EDigitalFilter.Normal)
                                for (var num23 = 0; num23 < 3; num23++)
                                    curve = await MathCommon.CurveSmooth(curve);

                            if (item.DigitalFilter == EDigitalFilter.High)
                            {
                                for (var num24 = 0; num24 < curve.CurvePoint.Count; num24++)
                                    if (num24 < num2 || curve.CurvePoint[num24].Y < curve.CurvePoint[num2 - 1].Y)
                                        curve.CurvePoint[num24].Y = 0.0;

                                var num25 = 0.06;
                                if (Experiment.ExperimentSetting.AMPAlgorithm == EAMPAlgorithm.RelativeLinear)
                                    num25 = Math.Pow(2.0, num25) - 1.0;

                                if (Experiment.ExperimentSetting.AMPAlgorithm == EAMPAlgorithm.Subtraction)
                                    num25 = (Math.Pow(2.0, num25) - 1.0) * num16;

                                var num26 = 0;
                                for (var num27 = 0; num27 < curve.CurvePoint.Count; num27++)
                                    if (curve.CurvePoint[num27].Y >= num25)
                                    {
                                        num26 = num27;
                                        break;
                                    }

                                if (num26 > 0)
                                {
                                    var flag4 = false;
                                    for (var num28 = num26 - 1; num28 >= 0; num28--)
                                        if (!flag4)
                                        {
                                            var num29 = curve.CurvePoint[num28 + 1].Y * 0.6;
                                            if (num29 <= num25 / 12.0) flag4 = true;

                                            curve.CurvePoint[num28].Y = num29;
                                        }
                                        else
                                        {
                                            curve.CurvePoint[num28].Y = 0.0;
                                        }

                                    for (var num30 = 0; num30 < 3; num30++)
                                        curve = await MathCommon.CurveSmooth(curve);
                                }
                                else
                                {
                                    var count2 = curve.CurvePoint.Count;
                                    for (var num31 = 0; num31 < count2; num31++) curve.CurvePoint[num31].Y = 0.0;
                                }
                            }

                            if (item.AMPGain > 0.0 && item.AMPGain != 1.0)
                                foreach (var dot2 in curve.CurvePoint)
                                    dot2.Y *= item.AMPGain;

                            ch.Result.RnValue = curve.CurvePoint[curve.CurvePoint.Count - 1].Y;
                            ch.Result.AMPCurve = curve;

                            List<string> listAmp = new();
                            listAmp.Add(w.CellName);
                            listAmp.Add(ch.Dye);
                            listAmp.Add(ch.TargetName);
                            for (var i = 0; i < curve.CurvePoint.Count(); i++)
                                listAmp.Add(curve.CurvePoint[i].Y.ToString());

                            if (change == "调整前")
                                ListPcrAmpCurves.Add(listAmp);
                            else
                                ListExcelAmpCurves.Add(listAmp);
                        }
                    }
                }
            }
        }

        await Task.Delay(0);
    }


    public async Task<int> ReadExcelData(string xlsxPath, string type = "原始曲线")
    {
        try
        {
            using (var fs = new FileStream(xlsxPath, FileMode.Open, FileAccess.ReadWrite))
            {
                // 使用XSSFWorkbook打开.xlsx文件（如果是.xls文件，使用HSSFWorkbook）
                IWorkbook workbook = new XSSFWorkbook(fs);

                // 获取指定工作表
                var sheet = workbook.GetSheet("实验数据");

                var startRaw = 0;
                var endRaw = 0;
                var startColumn = 0;
                var endColumn = 0;
                var targetValue = "原始曲线"; // 想要查找的单元格值
                var targetMeltValue = "原始熔解曲线"; // 想要查找的单元格值
                int startMeltColumn = 0;
                int endMeltColumn = 0;
                bool ismelt = false; //是否存在熔解峰值曲线
                List<string> listCurvePoit = new();
                List<List<string>> listCurves = new();
                for (var rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);

                    if (row != null)
                        for (var colIndex = 0; colIndex < row.LastCellNum; colIndex++)
                        {
                            var cell = row.GetCell(colIndex);

                            if (cell != null)
                            {
                                var cellValue = cell.ToString(); // 获取单元格的值

                                if (cellValue == targetValue)
                                {
                                    // 4. 获取单元格的行列
                                    startRaw = cell.RowIndex;
                                    startColumn = cell.ColumnIndex;
                                }

                                if (cellValue == targetMeltValue)
                                {
                                    startMeltColumn = cell.ColumnIndex;
                                }

                                if (cellValue == "扩增曲线")
                                {
                                    endMeltColumn = cell.ColumnIndex;
                                }

                                if (cellValue == "熔解峰值曲线")
                                {
                                    // 4. 获取单元格的行列
                                    endColumn = cell.ColumnIndex;
                                    ismelt = true;
                                }
                            }
                        }
                }

                for (var rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);

                    if (row != null)
                        for (var colIndex = 0; colIndex < row.LastCellNum; colIndex++)
                        {
                            var cell = row.GetCell(colIndex);

                            if (cell != null)
                            {
                                var cellValue = cell.ToString(); // 获取单元格的值

                                if (cellValue == "[Instrument]")
                                    // 4. 获取单元格的行列
                                    endRaw = cell.RowIndex - 1;
                            }
                        }
                }

                if (sheet != null)
                {
                    if (type == "熔解曲线") //有熔解曲线，取熔解曲线值
                    {
                        for (var i = startRaw + 1; i < endRaw; i++)
                        {
                            listCurvePoit = new List<string>();
                            listCurvePoit.Add(sheet.GetRow(i).GetCell(0).ToString());
                            listCurvePoit.Add(sheet.GetRow(i).GetCell(2).ToString());
                            listCurvePoit.Add(sheet.GetRow(i).GetCell(6).ToString());
                            listCurvePoit.Add(sheet.GetRow(i).GetCell(7).ToString());

                            for (var j = startMeltColumn + 1; j < endMeltColumn; j++)
                            {
                                listCurvePoit.Add(sheet.GetRow(i).GetCell(j).ToString());
                            }

                            listCurves.Add(listCurvePoit);
                        }
                    }
                    else //无熔解曲线，取原始曲线的值
                    {
                        for (var i = startRaw + 1; i < endRaw; i++)
                        {
                            listCurvePoit = new List<string>();
                            listCurvePoit.Add(sheet.GetRow(i).GetCell(0).ToString());
                            listCurvePoit.Add(sheet.GetRow(i).GetCell(2).ToString());
                            listCurvePoit.Add(sheet.GetRow(i).GetCell(6).ToString());
                            listCurvePoit.Add(sheet.GetRow(i).GetCell(7).ToString());

                            if (ismelt)
                            {
                                for (var j = startColumn + 1; j < endColumn; j++)
                                {
                                    listCurvePoit.Add(sheet.GetRow(i).GetCell(j).ToString());
                                }
                            }
                            else
                            {
                                for (var j = startColumn + 1; j < sheet.GetRow(i).LastCellNum; j++)
                                {
                                    listCurvePoit.Add(sheet.GetRow(i).GetCell(j).ToString());
                                }
                            }

                            listCurves.Add(listCurvePoit);
                        }
                    }
                }


                ismelt = false; //恢复默认的认为没有溶解曲线
                ListExcelRawCurves = new ObservableCollection<List<string>>(listCurves);
                if (sheet.GetRow(1).GetCell(1).CellType == CellType.Numeric)
                    Experiment.Device.StartTime = sheet.GetRow(1).GetCell(1).DateCellValue;
                else
                    Experiment.Device.StartTime = Convert.ToDateTime(sheet.GetRow(1).GetCell(1).StringCellValue);

                if (sheet.GetRow(2).GetCell(1).CellType == CellType.Numeric)
                    Experiment.Device.StopTime = sheet.GetRow(2).GetCell(1).DateCellValue;
                else
                    Experiment.Device.StopTime = Convert.ToDateTime(sheet.GetRow(2).GetCell(1).StringCellValue);

                Experiment.Device.RS232Port.DeviceNo = sheet.GetRow(endRaw + 3).GetCell(1).ToString();
                await Task.Delay(0);
                return 1;
            }
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBoxX.Show(Application.Current.MainWindow, ex.Message, "错误提示", MessageBoxButton.OK,
                    MessageBoxIcon.Error, DefaultButton.YesOK);
            });
            return 0;
        }
    }

    public void InitPcrCurve(out PlotModel curveModel, string curveName, List<string> list)
    {
        curveModel = new PlotModel();
        curveModel.Title = curveName;
        // 设置颜色，不知道为什么要设置，但是不设置这个十字叉不会出现
        var linearColorAxis1 = new LinearColorAxis();
        linearColorAxis1.Maximum = 1;
        linearColorAxis1.Minimum = -1;
        curveModel.Axes.Add(linearColorAxis1);
        // 设置坐标轴
        var linearAxis1 = new LinearAxis();
        var linearAxis2 = new LinearAxis();

        linearAxis1.Angle = 0;
        linearAxis1.MajorGridlineStyle = LineStyle.Solid;
        linearAxis1.MinorGridlineStyle = LineStyle.Dot;
        linearAxis1.Position = AxisPosition.Left;
        linearAxis1.FontSize = 16;
        linearAxis1.Maximum = Convert.ToDouble(list.Last().ToString()) * 1.1d;
        linearAxis1.Minimum = -0.1;
        linearAxis1.IsZoomEnabled = false;
        linearAxis1.IsPanEnabled = false;
        linearAxis1.Title = "Fluorescence";
        linearAxis1.Key = "Fluorescence"; //key与曲线的y轴相关联
        curveModel.Axes.Add(linearAxis1);


        linearAxis2.MajorGridlineStyle = LineStyle.Solid;
        linearAxis2.MinorGridlineStyle = LineStyle.Dot;
        linearAxis2.Angle = 0;
        linearAxis2.Maximum = 40;
        linearAxis2.Minimum = 1;
        linearAxis2.Position = AxisPosition.Bottom;
        linearAxis2.Title = "CycleTime";
        linearAxis2.IsZoomEnabled = false;
        linearAxis2.IsPanEnabled = false;
        linearAxis2.TitleFontSize = 16;
        curveModel.Axes.Add(linearAxis2);


        var legend1 = new Legend();
        legend1.LegendPlacement = LegendPlacement.Inside;
        legend1.LegendFontSize = 16;
        legend1.LegendPosition = LegendPosition.TopLeft;
        curveModel.Legends.Add(legend1);
    }

    public int GetWellIndex(string position)
    {
        if (position.Length < 2) throw new ArgumentException("Invalid position identifier");

        var letter = position[0];
        var number = int.Parse(position.Substring(1));

        if (letter < 'A' || letter > 'H' || number < 1 || number > 12)
            throw new ArgumentException("Invalid position identifier");

        var letterValue = letter - 'A' + 1;
        var integerValue = (letterValue - 1) * 12 + number;

        return integerValue;
    }

    /// <summary>
    /// 根据原始字节数组计算原始曲线
    /// </summary>
    /// <param name="RawData"></param>
    /// <param name="RealtimeCal"></param>
    public async Task RawCurveCal(List<double>[,] RawData, bool RealtimeCal, string change)
    {
        foreach (var well in Experiment.CurrentSubset.Wells)
            if (well.Sample != null)
                foreach (var sampleTargetItem in well.Sample.Items)
                {
                    var listRaw = new List<string>();
                    if (sampleTargetItem.TubeNo == well.MultiTubeID)
                    {
                        var num = Experiment.Wells.IndexOf(well);
                        var channelNo = sampleTargetItem.ChannelNo;
                        sampleTargetItem.Result.RawCurve.CurvePoint.Clear();
                        var count = RawData[num, channelNo - 1].Count;
                        for (var i = 0; i < count; i++)
                        {
                            var dot = new Dot();
                            dot.X = (double)(i + 1);
                            dot.Y = RawData[num, channelNo - 1][i];
                            if ((RealtimeCal || i != 0) && dot.Y >= -50.0)
                                sampleTargetItem.Result.RawCurve.CurvePoint.Add(dot);
                        }

                        if (!RealtimeCal && sampleTargetItem.Result.RawCurve.CurvePoint.Count < count &&
                            sampleTargetItem.Result.RawCurve.CurvePoint.Count >= 3)
                        {
                            sampleTargetItem.Result.RawCurve = await MathCommon.CurveInsert(
                                sampleTargetItem.Result.RawCurve,
                                1.0,
                                (double)count, 1.0);

                            listRaw.Add(well.CellName);
                            listRaw.Add(sampleTargetItem.Dye);
                            listRaw.Add(sampleTargetItem.TargetName);
                            for (var i = 0; i < sampleTargetItem.Result.RawCurve.CurvePoint.Count; i++)
                                listRaw.Add(sampleTargetItem.Result.RawCurve.CurvePoint[i].Y.ToString());
                        }

                        if (change == "调整前") ListPcrRawCurves.Add(listRaw);
                    }
                }
    }


    /// <summary>
    /// 获取原始字节数组
    /// </summary>
    /// <param name="RealtimeCal"></param>
    /// <param name="isMeltingRawData"></param>
    public async Task GetRawData(bool RealtimeCal, bool isMeltingRawData)
    {
        await Task.Run(() =>
        {
            Experiment = Experiment.Load(PcrPath, false);

            #region GetRawData

            List<FluorescenceData[,]> list;
            if (isMeltingRawData)
                list = Experiment.Device.RawMeltingData;
            else
                list = Experiment.Device.RawData;

            var array = new List<double>[96, 6];
            for (var n = 0; n < 96; n++)
            for (var j = 0; j < 6; j++)
                array[n, j] = new List<double>();

            int i;
            Func<int, bool> qq90 = null;
            Func<int, bool> qq91 = null;
            for (i = 0; i < list.Count; i++)
            {
                var array2 = list[i];
                for (var k = 0; k < 96; k++)
                for (var l = 0; l < 6; l++)
                {
                    var num = DeviceUtility.Conversion(array2[k, l]);
                    var badPoint = Experiment.BadPoints[k.ToString("00") + (l + 1)] as BadPoint;
                    if (badPoint != null)
                    {
                        if (isMeltingRawData)
                        {
                            IEnumerable<int> rawMeltPoints = badPoint.RawMeltPoints;
                            Func<int, bool> func;
                            if ((func = qq90) == null) func = qq90 = s => s == i;

                            if (rawMeltPoints.Where(func).Count() != 0) num = -100.0;
                        }
                        else
                        {
                            IEnumerable<int> rawPoints = badPoint.RawPoints;
                            Func<int, bool> func2;
                            if ((func2 = qq91) == null) func2 = qq91 = s => s == i;

                            if (rawPoints.Where(func2).Count() != 0) num = -100.0;
                        }
                    }

                    if (Experiment.Device.Calibration.Count == 6)
                    {
                        var array3 = Experiment.Device.Calibration[l];
                        if (array3.Count() == 98 && num > -50.0 && array3[k] != 255 && array3[k] != 0 &&
                            Experiment.Device.DarkCurrent[l] != 255)
                        {
                            var num2 = array3[k] / 100.0;
                            var num3 = (array3[96] * 256 + array3[97]) / 1000.0;
                            num = (num - Experiment.Device.DarkCurrent[l] / 10.0) * num2 * num3;
                        }
                    }

                    if (num > -50.0 && num < 0.1) num = 0.1;

                    array[k, l].Add(num);
                }
            }

            Task.Run(() => { RawCurveCal(array, false, "调整前"); });

            if (isMeltingRawData)
            {
                MeltrawData = array;
            }
            else
            {
                RawData = array;
            }

            #endregion
        });
    }


    [RelayCommand]
    public async Task ModifyPcrProgram()
    {
        await Task.Run(async () =>
        {
            // 检查文件夹是否存在
            if (Directory.Exists(PcrFolderPath))
            {
                try
                {
                    // 获取文件夹中的所有.pcr文件
                    var pcrFiles = Directory.GetFiles(PcrFolderPath, "*.pcr");
                    for (var q = 0; q < pcrFiles.Length; q++)
                    {
                        Experiment = Experiment.Load(pcrFiles[q], false);


                        for (int i = 0; i < Experiment.Program.Programs.Count - 1; i++)
                        {
                            var segment = Experiment.Program.Programs[i] as Segment;
                            if (segment.Name == "程序段1")
                            {
                                segment.Items[0].Temperature = 50;
                                segment.Items[0].HoldMinute = 2;

                                segment.Items[1].Temperature = 95;
                                segment.Items[1].HoldMinute = 5;
                            }
                            else if (segment.Name == "程序段2")
                            {
                                segment.Items[0].Temperature = 95;
                                segment.Items[0].HoldSecond = 10;

                                segment.Items[1].Temperature = 71;
                                segment.Items[1].HoldSecond = 25;

                                segment.Items[2].Temperature = 75;
                                segment.Items[2].HoldSecond = 30;
                            }
                            else if (segment.Name == "程序段3")
                            {
                                segment.Items[0].Temperature = 95;
                                segment.Items[0].HoldSecond = 10;

                                segment.Items[1].Temperature = 61;
                                segment.Items[1].HoldSecond = 25;

                                segment.Items[2].Temperature = 75;
                                segment.Items[2].HoldSecond = 25;
                            }
                            else if (segment.Name == "程序段4")
                            {
                                segment.Items[0].Temperature = 95;
                                segment.Items[0].HoldMinute = 2;

                                segment.Items[1].Temperature = 40;
                                segment.Items[1].HoldMinute = 2;
                            }
                        }

                        for (int i = 4; i < Experiment.Program.Programs.Count; i++)
                        {
                            var segment = Experiment.Program.Programs[i] as MeltingSegment;
                            if (segment.Name == "熔解程序段")
                            {
                                segment.StartTemp = 40;

                                segment.EndTemp = 80;
                            }
                        }

                        string newPcrName = pcrFiles[q].Substring(pcrFiles[q].LastIndexOf("\\"));
                        //NewPcrFolderPath += newPcrName;

                        if (File.Exists(ChangeFileExtension(NewPcrFolderPath + newPcrName, ".pcr")))
                        {
                            File.Delete(ChangeFileExtension(NewPcrFolderPath + newPcrName, ".pcr"));
                            Console.WriteLine("文件已删除。");
                        }

                        Experiment.Save(ChangeFileExtension(NewPcrFolderPath + newPcrName, ".pcr"));

                        //Schedule = (double)(q + 1) / pcrFiles.Length * 100d;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxX.Show(Application.Current.MainWindow, "新Pcr文件已生成！", "提示", MessageBoxButton.OK,
                            MessageBoxIcon.Success, DefaultButton.YesOK, 5);
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxX.Show(Application.Current.MainWindow, ex.Message, "错误提示", MessageBoxButton.OK,
                            MessageBoxIcon.Error, DefaultButton.YesOK);
                    });
                    return;
                }
            }
            else
            {
                Console.WriteLine("指定的文件夹路径不存在.");
            }
        });
    }
}

public class MathCommon
{
    // Token: 0x06000045 RID: 69 RVA: 0x00009677 File Offset: 0x00007877
    internal static double Conversion(FluorescenceData FData)
    {
        return DeviceUtility.Conversion(FData);
    }

    // Token: 0x06000046 RID: 70 RVA: 0x0000967F File Offset: 0x0000787F
    public static FluorescenceData Conversion(double value, bool div = false)
    {
        return DeviceUtility.Conversion(value, div);
    }

    // Token: 0x06000047 RID: 71 RVA: 0x00009688 File Offset: 0x00007888
    internal static Task<double> Insert(double x0, double x1, double x2, double y0, double y1, double y2, double x)
    {
        if (x == x0) return Task.FromResult(y0);

        if (x == x1) return Task.FromResult(y1);

        if (x == x2) return Task.FromResult(y2);

        return Task.FromResult((x - x0) * (x - x1) * (x - x2) * (y0 / ((x0 - x1) * (x0 - x2) * (x - x0)) +
                                                                 y1 / ((x1 - x0) * (x1 - x2) * (x - x1)) +
                                                                 y2 / ((x2 - x0) * (x2 - x1) * (x - x2))));
    }

    // Token: 0x06000048 RID: 72 RVA: 0x000096EC File Offset: 0x000078EC
    internal static async Task Baseline(int bb, int ee, Curve curve)
    {
        if (bb == ee) return;

        var num = (curve.CurvePoint[ee - 1].Y - curve.CurvePoint[bb - 1].Y) / (double)(ee - bb);
        var num2 = (curve.CurvePoint[bb - 1].Y * (double)ee - curve.CurvePoint[ee - 1].Y * (double)bb) /
                   (double)(ee - bb);
        var num3 = num * (double)((bb + ee) / 2) + num2;
        for (var i = 0; i < curve.CurvePoint.Count; i++)
            curve.CurvePoint[i].Y = curve.CurvePoint[i].Y - (num * (double)(i + 1) + num2 - num3);
    }

    // Token: 0x06000049 RID: 73 RVA: 0x000097AC File Offset: 0x000079AC
    internal static double Ct_cal(Curve curve, double threshold, double defaultValue = double.PositiveInfinity)
    {
        var num = 0;
        for (var i = 0; i < curve.CurvePoint.Count - 1; i++)
            if (curve.CurvePoint[i].Y < threshold && curve.CurvePoint[i + 1].Y >= threshold &&
                (i + 2 >= curve.CurvePoint.Count || curve.CurvePoint[i + 2].Y >= threshold) &&
                (i + 3 >= curve.CurvePoint.Count || curve.CurvePoint[i + 3].Y >= threshold))
                num = i + 1;

        if (num == 0) return defaultValue;

        var num2 = curve.CurvePoint[num].Y - curve.CurvePoint[num - 1].Y;
        var num3 = curve.CurvePoint[num].Y - num2 * curve.CurvePoint[num].X;
        return (threshold - num3) / num2;
    }

    // Token: 0x0600004A RID: 74 RVA: 0x000098B8 File Offset: 0x00007AB8
    public static double SD_cal(List<double> arr)
    {
        var num = 0.0;
        var num2 = arr.Average();
        foreach (var num3 in arr) num += (num3 - num2) * (num3 - num2);

        num = Math.Sqrt(num / (double)(arr.Count - 1));
        return num;
    }

    // Token: 0x0600004B RID: 75 RVA: 0x0000992C File Offset: 0x00007B2C
    internal static Task<Curve> CurveSmooth(Curve curve)
    {
        var array = new double[] { 31.0, 9.0, -3.0, -5.0, 3.0 };
        var array2 = new double[] { 9.0, 13.0, 12.0, 6.0, -5.0 };
        var array3 = new double[] { -3.0, 12.0, 17.0, 12.0, -3.0 };
        var array4 = new double[] { -5.0, 6.0, 12.0, 13.0, 9.0 };
        var array5 = new double[] { 3.0, -5.0, -3.0, 9.0, 31.0 };
        var count = curve.CurvePoint.Count;
        if (count < 10) return Task.FromResult(curve);

        var curve2 = new Curve();
        for (var i = 0; i < count; i++)
        {
            var dot = new Dot();
            dot.X = curve.CurvePoint[i].X;
            dot.Y = 0.0;
            double[] array6;
            int num;
            if (i == 0)
            {
                array6 = array;
                num = 0;
            }
            else if (i == 1)
            {
                array6 = array2;
                num = 1;
            }
            else if (i == count - 2)
            {
                array6 = array4;
                num = 3;
            }
            else if (i == count - 1)
            {
                array6 = array5;
                num = 4;
            }
            else
            {
                array6 = array3;
                num = 2;
            }

            for (var j = 0; j < 5; j++) dot.Y += curve.CurvePoint[i - num + j].Y * array6[j];

            dot.Y /= 35.0;
            curve2.CurvePoint.Add(dot);
        }

        return Task.FromResult(curve2);
    }

    // Token: 0x0600004C RID: 76 RVA: 0x00009AA4 File Offset: 0x00007CA4
    internal static Curve Derivation(Curve curve)
    {
        var array = new double[] { -25.0, 48.0, -36.0, 16.0, -3.0 };
        var array2 = new double[] { -3.0, -10.0, 18.0, -6.0, 1.0 };
        var array3 = new double[] { 1.0, -8.0, 0.0, 8.0, -1.0 };
        var array4 = new double[] { -1.0, 6.0, -18.0, 10.0, 3.0 };
        var array5 = new double[] { 3.0, -16.0, 36.0, -48.0, 25.0 };
        var count = curve.CurvePoint.Count;
        if (count < 10) return curve;

        var num = (curve.CurvePoint[curve.CurvePoint.Count - 1].X - curve.CurvePoint[0].X) /
                  (double)(curve.CurvePoint.Count - 1);
        var curve2 = new Curve();
        for (var i = 0; i < count; i++)
        {
            var dot = new Dot();
            dot.X = curve.CurvePoint[i].X;
            dot.Y = 0.0;
            double[] array6;
            int num2;
            if (i == 0)
            {
                array6 = array;
                num2 = 0;
            }
            else if (i == 1)
            {
                array6 = array2;
                num2 = 1;
            }
            else if (i == count - 2)
            {
                array6 = array4;
                num2 = 3;
            }
            else if (i == count - 1)
            {
                array6 = array5;
                num2 = 4;
            }
            else
            {
                array6 = array3;
                num2 = 2;
            }

            for (var j = 0; j < 5; j++) dot.Y += curve.CurvePoint[i - num2 + j].Y * array6[j];

            dot.Y = -dot.Y / (12.0 * num);
            curve2.CurvePoint.Add(dot);
        }

        return curve2;
    }

    // Token: 0x0600004D RID: 77 RVA: 0x00009C60 File Offset: 0x00007E60
    internal static Curve DerivationTwo(Curve curve, int dotNum)
    {
        var array = new double[] { 2.0, -1.0, -2.0, -1.0, 2.0, 7.0 };
        var array2 = new double[] { 5.0, 0.0, -3.0, -4.0, -3.0, 0.0, 5.0, 42.0 };
        var array3 = new double[] { 28.0, 7.0, -8.0, -17.0, -20.0, -17.0, -8.0, 7.0, 28.0, 462.0 };
        var array4 = new double[]
        {
            15.0, 6.0, -1.0, -6.0, -9.0, -10.0, -9.0, -6.0, -1.0, 6.0,
            15.0, 429.0
        };
        var array5 = new double[]
        {
            22.0, 11.0, 2.0, -5.0, -10.0, -13.0, -14.0, -13.0, -10.0, -5.0,
            2.0, 11.0, 22.0, 1001.0
        };
        var array6 = new double[]
        {
            91.0, 52.0, 19.0, -8.0, -29.0, -48.0, -53.0, -56.0, -53.0, -48.0,
            -29.0, -8.0, 19.0, 52.0, 91.0, 6188.0
        };
        var array7 = new double[]
        {
            40.0, 25.0, 12.0, 1.0, -8.0, -15.0, -20.0, -23.0, -24.0, -23.0,
            -20.0, -15.0, -8.0, 1.0, 12.0, 25.0, 40.0, 3976.0
        };
        var count = curve.CurvePoint.Count;
        if (count < dotNum) return curve;

        var num = (curve.CurvePoint[curve.CurvePoint.Count - 1].X - curve.CurvePoint[0].X) /
                  (double)(curve.CurvePoint.Count - 1);
        var curve2 = new Curve();
        double[] array8;
        int num2;
        switch (dotNum)
        {
            case 5:
                array8 = array;
                num2 = 2;
                goto IL_153;
            case 7:
                array8 = array2;
                num2 = 3;
                goto IL_153;
            case 9:
                array8 = array3;
                num2 = 4;
                goto IL_153;
            case 11:
                array8 = array4;
                num2 = 5;
                goto IL_153;
            case 13:
                array8 = array5;
                num2 = 6;
                goto IL_153;
            case 15:
                array8 = array6;
                num2 = 7;
                goto IL_153;
        }

        array8 = array7;
        num2 = 8;
        IL_153:
        for (var i = num2; i < count - num2; i++)
        {
            var dot = new Dot();
            dot.X = curve.CurvePoint[i].X;
            dot.Y = 0.0;
            for (var j = 0; j < dotNum; j++) dot.Y += curve.CurvePoint[i - num2 + j].Y * array8[j];

            dot.Y /= array8[dotNum] * num * num;
            curve2.CurvePoint.Add(dot);
        }

        return curve2;
    }

    // Token: 0x0600004E RID: 78 RVA: 0x00009E74 File Offset: 0x00008074
    internal static double[] Fitting(double[] x, double[] y, int count)
    {
        var array = new double[3];
        var num = 0.0;
        var num2 = 0.0;
        var num3 = 0.0;
        var num4 = 0.0;
        var num5 = 0.0;
        var num6 = 0.0;
        var num7 = 0.0;
        var num8 = 0.0;
        for (var i = 0; i < count; i++)
        {
            num += 1.0;
            num2 += x[i];
            num3 += x[i] * x[i];
            num4 += x[i] * x[i] * x[i];
            num5 += x[i] * x[i] * x[i] * x[i];
            num6 += y[i];
            num7 += x[i] * y[i];
            num8 += x[i] * x[i] * y[i];
        }

        var num9 = num2 * num2 - num * num3;
        var num10 = num2 * num3 - num * num4;
        var num11 = num3 * num3 - num2 * num4;
        var num12 = num3 * num4 - num2 * num5;
        var num13 = num2 * num6 - num * num7;
        var num14 = num3 * num7 - num2 * num8;
        array[2] = (num11 * num13 - num9 * num14) / (num10 * num11 - num9 * num12);
        array[1] = (num13 - num10 * array[2]) / num9;
        array[0] = (num6 - num2 * array[1] - num3 * array[2]) / num;
        return array;
    }

    // Token: 0x0600004F RID: 79 RVA: 0x00009FDC File Offset: 0x000081DC
    public static double Distance(double x1, double y1, double x2, double y2)
    {
        return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    // Token: 0x06000050 RID: 80 RVA: 0x00009FF4 File Offset: 0x000081F4
    public static async Task<Curve> CurveInsert(Curve curve, double begin, double end, double interval)
    {
        var count = curve.CurvePoint.Count;
        if (count < 6) return curve;

        var curve2 = new Curve();
        var num = 0;
        var array = new double[count];
        var array2 = new double[count];
        for (var i = 0; i < count; i++)
        {
            array[i] = curve.CurvePoint[i].X;
            array2[i] = curve.CurvePoint[i].Y;
        }

        while (begin <= end + 0.0001)
        {
            while (num != count - 1 && begin >= array[num]) num++;

            int num2;
            if (num <= 2)
                num2 = 0;
            else
                num2 = num - 2;

            var dot = new Dot();
            dot.X = begin;
            dot.Y = await Insert(array[num2], array[num2 + 1], array[num2 + 2], array2[num2], array2[num2 + 1],
                array2[num2 + 2], begin);
            curve2.CurvePoint.Add(dot);
            begin += interval;
        }

        return curve2;
    }

    // Token: 0x06000051 RID: 81 RVA: 0x0000A0EC File Offset: 0x000082EC
    public static Curve Filter(Curve curve, int num)
    {
        var array = new double[3];
        var count = curve.CurvePoint.Count;
        if (count < 2 * num + 1) return curve;

        var array2 = new double[2 * num + 1];
        var array3 = new double[2 * num + 1];
        var array4 = new double[count];
        var array5 = new double[count];
        for (var i = 0; i < count; i++)
        {
            array4[i] = curve.CurvePoint[i].X;
            array5[i] = curve.CurvePoint[i].Y;
        }

        for (var j = 0; j < count; j++)
        {
            if (j < num)
                for (var k = 0; k < 2 * num + 1; k++)
                {
                    array2[k] = array4[k];
                    array3[k] = array5[k];
                }
            else if (j >= num && j < count - num)
                for (var l = 0; l < 2 * num + 1; l++)
                {
                    array2[l] = array4[j - num + l];
                    array3[l] = array5[j - num + l];
                }
            else
                for (var m = 0; m < 2 * num + 1; m++)
                {
                    array2[m] = array4[count - 2 * num - 1 + m];
                    array3[m] = array5[count - 2 * num - 1 + m];
                }

            array = Fitting(array2, array3, 2 * num + 1);
            curve.CurvePoint[j].Y = array[0] + array[1] * array4[j] + array[2] * array4[j] * array4[j];
        }

        return curve;
    }
}