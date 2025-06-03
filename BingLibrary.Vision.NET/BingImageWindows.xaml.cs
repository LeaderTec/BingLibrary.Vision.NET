using System.Windows;
using System.Windows.Controls;

namespace BingLibrary.Vision
{
    /// <summary>
    /// BingImageWindows.xaml 的交互逻辑
    /// </summary>
    public partial class BingImageWindows : System.Windows.Controls.UserControl
    {
        public BingImageWindows()
        {
            InitializeComponent();
        }

        public ShowModes ShowMode
        {
            get { return (ShowModes)GetValue(ShowModeProperty); }
            set
            {
                SetValue(ShowModeProperty, value);

                RowDefinition row1 = new RowDefinition();
                RowDefinition row2 = new RowDefinition();
                RowDefinition row3 = new RowDefinition();
                ColumnDefinition col1 = new ColumnDefinition();
                ColumnDefinition col2 = new ColumnDefinition();
                ColumnDefinition col3 = new ColumnDefinition();
                ColumnDefinition col4 = new ColumnDefinition();
                grid.Children.Clear();
                grid.RowDefinitions.Clear();
                grid.ColumnDefinitions.Clear();
                BingImageWindow[] bingImageWindows = new BingImageWindow[9];
                for (int i = 1; i <= 9; i++)
                {
                    bingImageWindows[i - 1] = new BingImageWindow();
                    bingImageWindows[i - 1].Name = "win" + i;
                    bingImageWindows[i - 1].Margin = new Thickness(1);
                    UpdateBingImageWindowData(i, bingImageWindows[i - 1].WindowData);
                }

                switch (value)
                {
                    case ShowModes.One:
                        grid.Children.Add(bingImageWindows[0]);
                        Grid.SetRow(bingImageWindows[0], 0);
                        Grid.SetColumn(bingImageWindows[0], 0);
                        break;

                    case ShowModes.Two_OneRow:
                        grid.ColumnDefinitions.Add(col1);
                        grid.ColumnDefinitions.Add(col2);
                        grid.Children.Add(bingImageWindows[0]);
                        Grid.SetRow(bingImageWindows[0], 0);
                        Grid.SetColumn(bingImageWindows[0], 0);

                        grid.Children.Add(bingImageWindows[1]);
                        Grid.SetRow(bingImageWindows[1], 0);
                        Grid.SetColumn(bingImageWindows[1], 1);

                        break;

                    case ShowModes.Two_TwoRow:
                        grid.RowDefinitions.Add(row1);
                        grid.RowDefinitions.Add(row2);
                        grid.Children.Add(bingImageWindows[0]);
                        Grid.SetRow(bingImageWindows[0], 0);
                        Grid.SetColumn(bingImageWindows[0], 0);

                        grid.Children.Add(bingImageWindows[1]);
                        Grid.SetRow(bingImageWindows[1], 1);
                        Grid.SetColumn(bingImageWindows[1], 0);

                        break;

                    case ShowModes.Three_OneRow:
                        grid.ColumnDefinitions.Add(col1);
                        grid.ColumnDefinitions.Add(col2);
                        grid.ColumnDefinitions.Add(col3);

                        grid.Children.Add(bingImageWindows[0]);
                        Grid.SetRow(bingImageWindows[0], 0);
                        Grid.SetColumn(bingImageWindows[0], 0);

                        grid.Children.Add(bingImageWindows[1]);
                        Grid.SetRow(bingImageWindows[1], 0);
                        Grid.SetColumn(bingImageWindows[1], 1);

                        grid.Children.Add(bingImageWindows[2]);
                        Grid.SetRow(bingImageWindows[2], 0);
                        Grid.SetColumn(bingImageWindows[2], 2);

                        break;

                    case ShowModes.Three_TwoRow1:
                        grid.ColumnDefinitions.Add(col1);
                        grid.ColumnDefinitions.Add(col2);
                        grid.RowDefinitions.Add(row1);
                        grid.RowDefinitions.Add(row2);

                        grid.Children.Add(bingImageWindows[0]);
                        Grid.SetRow(bingImageWindows[0], 0);
                        Grid.SetColumn(bingImageWindows[0], 0);
                        Grid.SetRowSpan(bingImageWindows[0], 2);

                        grid.Children.Add(bingImageWindows[1]);
                        Grid.SetRow(bingImageWindows[1], 0);
                        Grid.SetColumn(bingImageWindows[1], 1);

                        grid.Children.Add(bingImageWindows[2]);
                        Grid.SetRow(bingImageWindows[2], 1);
                        Grid.SetColumn(bingImageWindows[2], 1);

                        break;

                    case ShowModes.Three_TwoRow2:
                        grid.ColumnDefinitions.Add(col1);
                        grid.ColumnDefinitions.Add(col2);
                        grid.RowDefinitions.Add(row1);
                        grid.RowDefinitions.Add(row2);

                        grid.Children.Add(bingImageWindows[0]);
                        Grid.SetRow(bingImageWindows[0], 0);
                        Grid.SetColumn(bingImageWindows[0], 0);
                        Grid.SetColumnSpan(bingImageWindows[0], 2);

                        grid.Children.Add(bingImageWindows[1]);
                        Grid.SetRow(bingImageWindows[1], 1);
                        Grid.SetColumn(bingImageWindows[1], 0);

                        grid.Children.Add(bingImageWindows[2]);
                        Grid.SetRow(bingImageWindows[2], 1);
                        Grid.SetColumn(bingImageWindows[2], 1);

                        break;

                    case ShowModes.Four:
                        grid.ColumnDefinitions.Add(col1);
                        grid.ColumnDefinitions.Add(col2);
                        grid.RowDefinitions.Add(row1);
                        grid.RowDefinitions.Add(row2);

                        grid.Children.Add(bingImageWindows[0]);
                        Grid.SetRow(bingImageWindows[0], 0);
                        Grid.SetColumn(bingImageWindows[0], 0);

                        grid.Children.Add(bingImageWindows[1]);
                        Grid.SetRow(bingImageWindows[1], 0);
                        Grid.SetColumn(bingImageWindows[1], 1);

                        grid.Children.Add(bingImageWindows[2]);
                        Grid.SetRow(bingImageWindows[2], 1);
                        Grid.SetColumn(bingImageWindows[2], 0);

                        grid.Children.Add(bingImageWindows[3]);
                        Grid.SetRow(bingImageWindows[3], 1);
                        Grid.SetColumn(bingImageWindows[3], 1);

                        break;

                    case ShowModes.Five:
                        grid.ColumnDefinitions.Add(col1);
                        grid.ColumnDefinitions.Add(col2);
                        grid.ColumnDefinitions.Add(col3);
                        grid.RowDefinitions.Add(row1);
                        grid.RowDefinitions.Add(row2);

                        grid.Children.Add(bingImageWindows[0]);
                        Grid.SetRow(bingImageWindows[0], 0);
                        Grid.SetColumn(bingImageWindows[0], 0);
                        Grid.SetColumnSpan(bingImageWindows[0], 2);

                        grid.Children.Add(bingImageWindows[1]);
                        Grid.SetRow(bingImageWindows[1], 0);
                        Grid.SetColumn(bingImageWindows[1], 2);

                        grid.Children.Add(bingImageWindows[2]);
                        Grid.SetRow(bingImageWindows[2], 1);
                        Grid.SetColumn(bingImageWindows[2], 0);

                        grid.Children.Add(bingImageWindows[3]);
                        Grid.SetRow(bingImageWindows[3], 1);
                        Grid.SetColumn(bingImageWindows[3], 1);

                        grid.Children.Add(bingImageWindows[4]);
                        Grid.SetRow(bingImageWindows[4], 1);
                        Grid.SetColumn(bingImageWindows[4], 2);

                        break;

                    case ShowModes.Six:
                        grid.ColumnDefinitions.Add(col1);
                        grid.ColumnDefinitions.Add(col2);
                        grid.ColumnDefinitions.Add(col3);
                        grid.RowDefinitions.Add(row1);
                        grid.RowDefinitions.Add(row2);

                        grid.Children.Add(bingImageWindows[0]);
                        Grid.SetRow(bingImageWindows[0], 0);
                        Grid.SetColumn(bingImageWindows[0], 0);

                        grid.Children.Add(bingImageWindows[1]);
                        Grid.SetRow(bingImageWindows[1], 0);
                        Grid.SetColumn(bingImageWindows[1], 1);

                        grid.Children.Add(bingImageWindows[2]);
                        Grid.SetRow(bingImageWindows[2], 0);
                        Grid.SetColumn(bingImageWindows[2], 2);

                        grid.Children.Add(bingImageWindows[3]);
                        Grid.SetRow(bingImageWindows[3], 1);
                        Grid.SetColumn(bingImageWindows[3], 0);

                        grid.Children.Add(bingImageWindows[4]);
                        Grid.SetRow(bingImageWindows[4], 1);
                        Grid.SetColumn(bingImageWindows[4], 1);

                        grid.Children.Add(bingImageWindows[5]);
                        Grid.SetRow(bingImageWindows[5], 1);
                        Grid.SetColumn(bingImageWindows[5], 2);

                        break;

                    case ShowModes.Seven:
                        grid.ColumnDefinitions.Add(col1);
                        grid.ColumnDefinitions.Add(col2);
                        grid.ColumnDefinitions.Add(col3);
                        grid.ColumnDefinitions.Add(col4);
                        grid.RowDefinitions.Add(row1);
                        grid.RowDefinitions.Add(row2);

                        grid.Children.Add(bingImageWindows[0]);
                        Grid.SetRow(bingImageWindows[0], 0);
                        Grid.SetColumn(bingImageWindows[0], 0);
                        Grid.SetColumnSpan(bingImageWindows[0], 2);

                        grid.Children.Add(bingImageWindows[1]);
                        Grid.SetRow(bingImageWindows[1], 0);
                        Grid.SetColumn(bingImageWindows[1], 2);

                        grid.Children.Add(bingImageWindows[2]);
                        Grid.SetRow(bingImageWindows[2], 0);
                        Grid.SetColumn(bingImageWindows[2], 3);

                        grid.Children.Add(bingImageWindows[3]);
                        Grid.SetRow(bingImageWindows[3], 1);
                        Grid.SetColumn(bingImageWindows[3], 0);

                        grid.Children.Add(bingImageWindows[4]);
                        Grid.SetRow(bingImageWindows[4], 1);
                        Grid.SetColumn(bingImageWindows[4], 1);

                        grid.Children.Add(bingImageWindows[5]);
                        Grid.SetRow(bingImageWindows[5], 1);
                        Grid.SetColumn(bingImageWindows[5], 2);

                        grid.Children.Add(bingImageWindows[6]);
                        Grid.SetRow(bingImageWindows[6], 1);
                        Grid.SetColumn(bingImageWindows[6], 3);

                        break;

                    case ShowModes.Eight:
                        grid.ColumnDefinitions.Add(col1);
                        grid.ColumnDefinitions.Add(col2);
                        grid.ColumnDefinitions.Add(col3);
                        grid.ColumnDefinitions.Add(col4);
                        grid.RowDefinitions.Add(row1);
                        grid.RowDefinitions.Add(row2);

                        grid.Children.Add(bingImageWindows[0]);
                        Grid.SetRow(bingImageWindows[0], 0);
                        Grid.SetColumn(bingImageWindows[0], 0);

                        grid.Children.Add(bingImageWindows[1]);
                        Grid.SetRow(bingImageWindows[1], 0);
                        Grid.SetColumn(bingImageWindows[1], 1);

                        grid.Children.Add(bingImageWindows[2]);
                        Grid.SetRow(bingImageWindows[2], 0);
                        Grid.SetColumn(bingImageWindows[2], 2);

                        grid.Children.Add(bingImageWindows[3]);
                        Grid.SetRow(bingImageWindows[3], 0);
                        Grid.SetColumn(bingImageWindows[3], 3);

                        grid.Children.Add(bingImageWindows[4]);
                        Grid.SetRow(bingImageWindows[4], 1);
                        Grid.SetColumn(bingImageWindows[4], 0);

                        grid.Children.Add(bingImageWindows[5]);
                        Grid.SetRow(bingImageWindows[5], 1);
                        Grid.SetColumn(bingImageWindows[5], 1);

                        grid.Children.Add(bingImageWindows[6]);
                        Grid.SetRow(bingImageWindows[6], 1);
                        Grid.SetColumn(bingImageWindows[6], 2);

                        grid.Children.Add(bingImageWindows[7]);
                        Grid.SetRow(bingImageWindows[7], 1);
                        Grid.SetColumn(bingImageWindows[7], 3);

                        break;

                    case ShowModes.Nine:
                        grid.ColumnDefinitions.Add(col1);
                        grid.ColumnDefinitions.Add(col2);
                        grid.ColumnDefinitions.Add(col3);
                        grid.RowDefinitions.Add(row1);
                        grid.RowDefinitions.Add(row2);
                        grid.RowDefinitions.Add(row3);

                        grid.Children.Add(bingImageWindows[0]);
                        Grid.SetRow(bingImageWindows[0], 0);
                        Grid.SetColumn(bingImageWindows[0], 0);

                        grid.Children.Add(bingImageWindows[1]);
                        Grid.SetRow(bingImageWindows[1], 0);
                        Grid.SetColumn(bingImageWindows[1], 1);

                        grid.Children.Add(bingImageWindows[2]);
                        Grid.SetRow(bingImageWindows[2], 0);
                        Grid.SetColumn(bingImageWindows[2], 2);

                        grid.Children.Add(bingImageWindows[3]);
                        Grid.SetRow(bingImageWindows[3], 1);
                        Grid.SetColumn(bingImageWindows[3], 0);

                        grid.Children.Add(bingImageWindows[4]);
                        Grid.SetRow(bingImageWindows[4], 1);
                        Grid.SetColumn(bingImageWindows[4], 1);

                        grid.Children.Add(bingImageWindows[5]);
                        Grid.SetRow(bingImageWindows[5], 1);
                        Grid.SetColumn(bingImageWindows[5], 2);

                        grid.Children.Add(bingImageWindows[6]);
                        Grid.SetRow(bingImageWindows[6], 2);
                        Grid.SetColumn(bingImageWindows[6], 0);

                        grid.Children.Add(bingImageWindows[7]);
                        Grid.SetRow(bingImageWindows[7], 2);
                        Grid.SetColumn(bingImageWindows[7], 1);

                        grid.Children.Add(bingImageWindows[8]);
                        Grid.SetRow(bingImageWindows[8], 2);
                        Grid.SetColumn(bingImageWindows[8], 2);

                        break;
                }
            }
        }

        public static readonly DependencyProperty ShowModeProperty =
            DependencyProperty.Register("ShowMode", typeof(ShowModes), typeof(BingImageWindows), new PropertyMetadata(ShowModes.One));

        public void UpdateBingImageWindowData(int key, BingImageWindowData bingImageWindowData)
        {
            if (!BingImageWindowDatas.bingImageWindowDatas.ContainsKey(key))
                BingImageWindowDatas.bingImageWindowDatas.Add(key, bingImageWindowData);
            else
            {
                BingImageWindowDatas.bingImageWindowDatas.Remove(key);
                BingImageWindowDatas.bingImageWindowDatas.Add(key, bingImageWindowData);
            }
        }

        //这里等到初始化完成后再更新属性，否则无效
        private void grid_Loaded(object sender, RoutedEventArgs e)
        {
            ShowMode = ShowMode;
        }
    }

    public enum ShowModes
    {
        One,
        Two_OneRow,
        Two_TwoRow,
        Three_OneRow,
        Three_TwoRow1,
        Three_TwoRow2,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
    }
}