#region

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CurveTool.View;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

#endregion

namespace CurveTool.ViewModel;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private object _content;
    [ObservableProperty] private XlsxToExpView _exp;
    [ObservableProperty] private XlsxToExpViewModel _expContent;
    [ObservableProperty] private XlsxToPcrView _pcr;
    [ObservableProperty] private XlsxToPcrViewModel _pcrContent;


    public MainViewModel()
    {
        Exp = new XlsxToExpView();
        ExpContent = new XlsxToExpViewModel();
        Exp.DataContext = ExpContent;

        Pcr = new XlsxToPcrView();
        PcrContent = new XlsxToPcrViewModel();
        Pcr.DataContext = PcrContent;

        Content = Pcr;
    }

    [RelayCommand]
    public void SelectionChanged(object listboxitem)
    {
        var viewname = string.Empty;

        if (listboxitem as ListBoxItem != null)
        {
            var textBlock = FindVisualChild<TextBlock>(listboxitem as ListBoxItem);
            if (textBlock != null) viewname = textBlock.Text;
        }

        switch (viewname)
        {
            case "HongShi":
                Content = Pcr;
                break;
            case "iGenPad":
                Content = Exp;
                break;

        }
    }


    public T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child != null && child is T) return (T)child;

            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null) return childOfChild;
        }

        return null;
    }
}