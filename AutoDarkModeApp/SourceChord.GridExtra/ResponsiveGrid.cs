using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SourceChord.GridExtra
{
    public partial class ResponsiveGrid : Panel
    {
        public ResponsiveGrid()
        {
            this.MaxDivision = 12;
            this.BreakPoints = new BreakPoints();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var count = 0;
            var currentRow = 0;

            var availableWidth = double.IsPositiveInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width / this.MaxDivision;
            var children = this.Children.OfType<UIElement>();


            foreach (UIElement child in this.Children)
            {
                if (child != null)
                {
                    // Collapsedの時はレイアウトしない
                    if (child.Visibility == Visibility.Collapsed) { continue; }

                    var span = this.GetSpan(child, availableSize.Width);
                    var offset = this.GetOffset(child, availableSize.Width);
                    var push = this.GetPush(child, availableSize.Width);
                    var pull = this.GetPull(child, availableSize.Width);

                    if (count + span + offset > this.MaxDivision)
                    {
                        // リセット
                        currentRow++;
                        count = 0;
                    }

                    SetActualColumn(child, count + offset + push - pull);
                    SetActualRow(child, currentRow);

                    count += (span + offset);

                    var size = new Size(availableWidth * span, double.PositiveInfinity);
                    child.Measure(size);
                }
            }

            // 行ごとにグルーピングする
            var group = this.Children.OfType<UIElement>()
                                     .GroupBy(x => GetActualRow(x));

            var totalSize = new Size();
            if (group.Count() != 0)
            {
                totalSize.Width = group.Max(rows => rows.Sum(o => o.DesiredSize.Width));
                totalSize.Height = group.Sum(rows => rows.Max(o => o.DesiredSize.Height));
            }

            return totalSize;
        }

        protected int GetSpan(UIElement element, double width)
        {
            var span = 0;

            var getXS = new Func<UIElement, int>((o) => { var x = GetXS(o); return x != 0 ? x : this.MaxDivision; });
            var getSM = new Func<UIElement, int>((o) => { var x = GetSM(o); return x != 0 ? x : getXS(o); });
            var getMD = new Func<UIElement, int>((o) => { var x = GetMD(o); return x != 0 ? x : getSM(o); });
            var getLG = new Func<UIElement, int>((o) => { var x = GetLG(o); return x != 0 ? x : getMD(o); });

            if (width < this.BreakPoints.XS_SM)
            {
                span = getXS(element);
            }
            else if (width < this.BreakPoints.SM_MD)
            {
                span = getSM(element);
            }
            else if (width < this.BreakPoints.MD_LG)
            {
                span = getMD(element);
            }
            else
            {
                span = getLG(element);
            }

            return Math.Min(Math.Max(0, span), this.MaxDivision); ;
        }

        protected int GetOffset(UIElement element, double width)
        {
            var span = 0;

            var getXS = new Func<UIElement, int>((o) => { var x = GetXS_Offset(o); return x != 0 ? x : 0; });
            var getSM = new Func<UIElement, int>((o) => { var x = GetSM_Offset(o); return x != 0 ? x : getXS(o); });
            var getMD = new Func<UIElement, int>((o) => { var x = GetMD_Offset(o); return x != 0 ? x : getSM(o); });
            var getLG = new Func<UIElement, int>((o) => { var x = GetLG_Offset(o); return x != 0 ? x : getMD(o); });

            if (width < this.BreakPoints.XS_SM)
            {
                span = getXS(element);
            }
            else if (width < this.BreakPoints.SM_MD)
            {
                span = getSM(element);
            }
            else if (width < this.BreakPoints.MD_LG)
            {
                span = getMD(element);
            }
            else
            {
                span = getLG(element);
            }

            return Math.Min(Math.Max(0, span), this.MaxDivision); ;
        }

        protected int GetPush(UIElement element, double width)
        {
            var span = 0;

            var getXS = new Func<UIElement, int>((o) => { var x = GetXS_Push(o); return x != 0 ? x : 0; });
            var getSM = new Func<UIElement, int>((o) => { var x = GetSM_Push(o); return x != 0 ? x : getXS(o); });
            var getMD = new Func<UIElement, int>((o) => { var x = GetMD_Push(o); return x != 0 ? x : getSM(o); });
            var getLG = new Func<UIElement, int>((o) => { var x = GetLG_Push(o); return x != 0 ? x : getMD(o); });

            if (width < this.BreakPoints.XS_SM)
            {
                span = getXS(element);
            }
            else if (width < this.BreakPoints.SM_MD)
            {
                span = getSM(element);
            }
            else if (width < this.BreakPoints.MD_LG)
            {
                span = getMD(element);
            }
            else
            {
                span = getLG(element);
            }

            return Math.Min(Math.Max(0, span), this.MaxDivision); ;
        }

        protected int GetPull(UIElement element, double width)
        {
            var span = 0;

            var getXS = new Func<UIElement, int>((o) => { var x = GetXS_Pull(o); return x != 0 ? x : 0; });
            var getSM = new Func<UIElement, int>((o) => { var x = GetSM_Pull(o); return x != 0 ? x : getXS(o); });
            var getMD = new Func<UIElement, int>((o) => { var x = GetMD_Pull(o); return x != 0 ? x : getSM(o); });
            var getLG = new Func<UIElement, int>((o) => { var x = GetLG_Pull(o); return x != 0 ? x : getMD(o); });

            if (width < this.BreakPoints.XS_SM)
            {
                span = getXS(element);
            }
            else if (width < this.BreakPoints.SM_MD)
            {
                span = getSM(element);
            }
            else if (width < this.BreakPoints.MD_LG)
            {
                span = getMD(element);
            }
            else
            {
                span = getLG(element);
            }

            return Math.Min(Math.Max(0, span), this.MaxDivision); ;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var columnWidth = finalSize.Width / this.MaxDivision;

            // 行ごとにグルーピングする
            var group = this.Children.OfType<UIElement>()
                                     .GroupBy(x => GetActualRow(x));

            double temp = 0;
            foreach (var rows in group)
            {
                double max = 0;

                var columnHeight = rows.Max(o => o.DesiredSize.Height);
                foreach (var element in rows)
                {
                    var column = GetActualColumn(element);
                    var row = GetActualRow(element);
                    var columnSpan = this.GetSpan(element, finalSize.Width);

                    var rect = new Rect(columnWidth * column, temp, columnWidth * columnSpan, columnHeight);

                    element.Arrange(rect);

                    max = Math.Max(element.DesiredSize.Height, max);
                }

                temp += max;
            }
            return base.ArrangeOverride(finalSize);
        }

#if WINDOWS_WPF
        // ShowGridLinesで表示する際に利用するペンの定義
        private static readonly Pen _guidePen1
            = new Pen(Brushes.Yellow, 1);
        private static readonly Pen _guidePen2
            = new Pen(Brushes.Blue, 1) { DashStyle = new DashStyle(new double[] { 4, 4 }, 0) };

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            // ShowGridLinesが有効な場合、各種エレメントを描画する前に、ガイド用のグリッドを描画する。
            if (this.ShowGridLines)
            {
                var gridNum = this.MaxDivision;
                var unit = this.ActualWidth / gridNum;
                for (var i = 0; i <= gridNum; i++)
                {
                    var x = (int)(unit * i);
                    dc.DrawLine(_guidePen1, new Point(x, 0), new Point(x, this.ActualHeight));
                    dc.DrawLine(_guidePen2, new Point(x, 0), new Point(x, this.ActualHeight));
                }
            }
        }
#endif

    }
}
