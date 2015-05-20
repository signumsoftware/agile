using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace Agile.Windows.Controls
{
    public class ImmediateGrid : DataGrid
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            var desiredSize = base.MeasureOverride(availableSize);
            ClearBindingGroup();
            return desiredSize;
        }

        private void ClearBindingGroup()
        {
            // Clear ItemBindingGroup so it isn't applied to new rows
            ItemBindingGroup = null;
            // Clear BindingGroup on already created rows
            foreach (var item in Items)
            {
                var row = ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (row != null)
                    row.BindingGroup = null;
            }
        }
    }
}
