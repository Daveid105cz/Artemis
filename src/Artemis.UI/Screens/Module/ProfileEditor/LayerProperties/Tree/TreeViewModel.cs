﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stylet;

namespace Artemis.UI.Screens.Module.ProfileEditor.LayerProperties.Tree
{
    public class TreeViewModel : PropertyChangedBase
    {

        public TreeViewModel(LayerPropertiesViewModel layerPropertiesViewModel, BindableCollection<LayerPropertyGroupViewModel> layerPropertyGroups)
        {
            LayerPropertiesViewModel = layerPropertiesViewModel;
            LayerPropertyGroups = layerPropertyGroups;
        }

        public LayerPropertiesViewModel LayerPropertiesViewModel { get; }
        public BindableCollection<LayerPropertyGroupViewModel> LayerPropertyGroups { get; }

        public void PropertyTreePreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Handled || !(sender is System.Windows.Controls.TreeView))
                return;

            e.Handled = true;
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            };
            var parent = ((Control) sender).Parent as UIElement;
            parent?.RaiseEvent(eventArg);
        }
    }
}