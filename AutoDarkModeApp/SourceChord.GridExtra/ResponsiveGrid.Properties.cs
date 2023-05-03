using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Controls;


namespace SourceChord.GridExtra
{
    public partial class ResponsiveGrid
    {
        #region ResponsiveGrid自体に設定する依存関係プロパティ
        // 各種ブレークポイントの設定用プロパティ
        public int MaxDivision
        {
            get { return (int)GetValue(MaxDivisionProperty); }
            set { SetValue(MaxDivisionProperty, value); }
        }
        // Using a DependencyProperty as the backing store for MaxDivision.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxDivisionProperty =
            DependencyProperty.Register("MaxDivision",
                                        typeof(int),
                                        typeof(ResponsiveGrid),
                                        new FrameworkPropertyMetadata(12, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public BreakPoints BreakPoints
        {
            get { return (BreakPoints)GetValue(BreakPointsProperty); }
            set { SetValue(BreakPointsProperty, value); }
        }
        // Using a DependencyProperty as the backing store for BreakPoints.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BreakPointsProperty =
            DependencyProperty.Register("BreakPoints",
                                        typeof(BreakPoints),
                                        typeof(ResponsiveGrid),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool ShowGridLines
        {
            get { return (bool)GetValue(ShowGridLinesProperty); }
            set { SetValue(ShowGridLinesProperty, value); }
        }
        // Using a DependencyProperty as the backing store for ShowGridLines.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowGridLinesProperty =
            DependencyProperty.Register("ShowGridLines", typeof(bool), typeof(ResponsiveGrid), new PropertyMetadata(false));


        #endregion



        #region 各子要素のサイズを決めるための添付プロパティ
        public static int GetXS(DependencyObject obj)
        {
            return (int)obj.GetValue(XSProperty);
        }
        public static void SetXS(DependencyObject obj, int value)
        {
            obj.SetValue(XSProperty, value);
        }
        // Using a DependencyProperty as the backing store for XS.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XSProperty =
            DependencyProperty.RegisterAttached("XS",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetSM(DependencyObject obj)
        {
            return (int)obj.GetValue(SMProperty);
        }
        public static void SetSM(DependencyObject obj, int value)
        {
            obj.SetValue(SMProperty, value);
        }
        // Using a DependencyProperty as the backing store for SM.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SMProperty =
            DependencyProperty.RegisterAttached("SM",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetMD(DependencyObject obj)
        {
            return (int)obj.GetValue(MDProperty);
        }
        public static void SetMD(DependencyObject obj, int value)
        {
            obj.SetValue(MDProperty, value);
        }
        // Using a DependencyProperty as the backing store for MD.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MDProperty =
            DependencyProperty.RegisterAttached("MD",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetLG(DependencyObject obj)
        {
            return (int)obj.GetValue(LGProperty);
        }
        public static void SetLG(DependencyObject obj, int value)
        {
            obj.SetValue(LGProperty, value);
        }
        // Using a DependencyProperty as the backing store for LG.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LGProperty =
            DependencyProperty.RegisterAttached("LG",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        #endregion

        #region Offsetプロパティ
        public static int GetXS_Offset(DependencyObject obj)
        {
            return (int)obj.GetValue(XS_OffsetProperty);
        }
        public static void SetXS_Offset(DependencyObject obj, int value)
        {
            obj.SetValue(XS_OffsetProperty, value);
        }
        // Using a DependencyProperty as the backing store for XS_Offset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XS_OffsetProperty =
            DependencyProperty.RegisterAttached("XS_Offset",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetSM_Offset(DependencyObject obj)
        {
            return (int)obj.GetValue(SM_OffsetProperty);
        }
        public static void SetSM_Offset(DependencyObject obj, int value)
        {
            obj.SetValue(SM_OffsetProperty, value);
        }
        // Using a DependencyProperty as the backing store for SM_Offset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SM_OffsetProperty =
            DependencyProperty.RegisterAttached("SM_Offset",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetMD_Offset(DependencyObject obj)
        {
            return (int)obj.GetValue(MD_OffsetProperty);
        }
        public static void SetMD_Offset(DependencyObject obj, int value)
        {
            obj.SetValue(MD_OffsetProperty, value);
        }
        // Using a DependencyProperty as the backing store for MD_Offset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MD_OffsetProperty =
            DependencyProperty.RegisterAttached("MD_Offset",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetLG_Offset(DependencyObject obj)
        {
            return (int)obj.GetValue(LG_OffsetProperty);
        }
        public static void SetLG_Offset(DependencyObject obj, int value)
        {
            obj.SetValue(LG_OffsetProperty, value);
        }
        // Using a DependencyProperty as the backing store for LG_Offset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LG_OffsetProperty =
            DependencyProperty.RegisterAttached("LG_Offset",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        #endregion


        #region Pushプロパティ
        public static int GetXS_Push(DependencyObject obj)
        {
            return (int)obj.GetValue(XS_PushProperty);
        }
        public static void SetXS_Push(DependencyObject obj, int value)
        {
            obj.SetValue(XS_PushProperty, value);
        }
        // Using a DependencyProperty as the backing store for XS_Push.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XS_PushProperty =
            DependencyProperty.RegisterAttached("XS_Push",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetSM_Push(DependencyObject obj)
        {
            return (int)obj.GetValue(SM_PushProperty);
        }
        public static void SetSM_Push(DependencyObject obj, int value)
        {
            obj.SetValue(SM_PushProperty, value);
        }
        // Using a DependencyProperty as the backing store for SM_Push.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SM_PushProperty =
            DependencyProperty.RegisterAttached("SM_Push",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetMD_Push(DependencyObject obj)
        {
            return (int)obj.GetValue(MD_PushProperty);
        }
        public static void SetMD_Push(DependencyObject obj, int value)
        {
            obj.SetValue(MD_PushProperty, value);
        }
        // Using a DependencyProperty as the backing store for MD_Push.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MD_PushProperty =
            DependencyProperty.RegisterAttached("MD_Push",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetLG_Push(DependencyObject obj)
        {
            return (int)obj.GetValue(LG_PushProperty);
        }
        public static void SetLG_Push(DependencyObject obj, int value)
        {
            obj.SetValue(LG_PushProperty, value);
        }
        // Using a DependencyProperty as the backing store for LG_Push.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LG_PushProperty =
            DependencyProperty.RegisterAttached("LG_Push",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        #endregion


        #region Pullプロパティ
        public static int GetXS_Pull(DependencyObject obj)
        {
            return (int)obj.GetValue(XS_PullProperty);
        }
        public static void SetXS_Pull(DependencyObject obj, int value)
        {
            obj.SetValue(XS_PullProperty, value);
        }
        // Using a DependencyProperty as the backing store for XS_Pull.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XS_PullProperty =
            DependencyProperty.RegisterAttached("XS_Pull",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetSM_Pull(DependencyObject obj)
        {
            return (int)obj.GetValue(SM_PullProperty);
        }
        public static void SetSM_Pull(DependencyObject obj, int value)
        {
            obj.SetValue(SM_PullProperty, value);
        }
        // Using a DependencyProperty as the backing store for SM_Pull.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SM_PullProperty =
            DependencyProperty.RegisterAttached("SM_Pull",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetMD_Pull(DependencyObject obj)
        {
            return (int)obj.GetValue(MD_PullProperty);
        }
        public static void SetMD_Pull(DependencyObject obj, int value)
        {
            obj.SetValue(MD_PullProperty, value);
        }
        // Using a DependencyProperty as the backing store for MD_Pull.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MD_PullProperty =
            DependencyProperty.RegisterAttached("MD_Pull",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static int GetLG_Pull(DependencyObject obj)
        {
            return (int)obj.GetValue(LG_PullProperty);
        }
        public static void SetLG_Pull(DependencyObject obj, int value)
        {
            obj.SetValue(LG_PullProperty, value);
        }
        // Using a DependencyProperty as the backing store for LG_Pull.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LG_PullProperty =
            DependencyProperty.RegisterAttached("LG_Pull",
                                                typeof(int),
                                                typeof(ResponsiveGrid),
                                                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        #endregion


        #region 読み取り専用の添付プロパティ
        public static int GetActualColumn(DependencyObject obj)
        {
            return (int)obj.GetValue(ActualColumnProperty);
        }
        protected static void SetActualColumn(DependencyObject obj, int value)
        {
            obj.SetValue(ActualColumnProperty, value);
        }
        // Using a DependencyProperty as the backing store for ActualColumn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActualColumnProperty =
            DependencyProperty.RegisterAttached("ActualColumn", typeof(int), typeof(ResponsiveGrid), new PropertyMetadata(0));

        public static int GetActualRow(DependencyObject obj)
        {
            return (int)obj.GetValue(ActualRowProperty);
        }
        protected static void SetActualRow(DependencyObject obj, int value)
        {
            obj.SetValue(ActualRowProperty, value);
        }
        // Using a DependencyProperty as the backing store for ActualRow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActualRowProperty =
            DependencyProperty.RegisterAttached("ActualRow", typeof(int), typeof(ResponsiveGrid), new PropertyMetadata(0));

        #endregion
    }
}
