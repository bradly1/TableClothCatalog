﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using TableCloth.Models;
using TableCloth.Models.Catalog;
using TableCloth.ViewModels;

namespace TableCloth.Pages;

public partial class CatalogPage : Page
{
    public CatalogPage(
        CatalogPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public CatalogPageViewModel ViewModel
        => (CatalogPageViewModel)DataContext;

    // https://stackoverflow.com/questions/1077397/scroll-listviewitem-to-be-at-the-top-of-a-listview
    private DependencyObject? GetScrollViewer(DependencyObject o)
    {
        if (o is ScrollViewer)
            return o;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
        {
            var child = VisualTreeHelper.GetChild(o, i);
            var result = GetScrollViewer(child);

            if (result == null)
                continue;
            else
                return result;
        }

        return null;
    }

    // https://stackoverflow.com/questions/660554/how-to-automatically-select-all-text-on-focus-in-wpf-textbox
    private void SiteCatalogFilter_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox siteCatalogFilter)
            return;

        // Fixes issue when clicking cut/copy/paste in context menu
        if (siteCatalogFilter.SelectionLength < 1)
            siteCatalogFilter.SelectAll();
    }

    private void SiteCatalogFilter_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (sender is not TextBox siteCatalogFilter)
            return;

        // If user highlights some text, don't override it
        if (siteCatalogFilter.SelectionLength < 1)
            siteCatalogFilter.SelectAll();

        // further clicks will not select all
        siteCatalogFilter.LostMouseCapture -= SiteCatalogFilter_LostMouseCapture;
    }

    private void SiteCatalogFilter_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox siteCatalogFilter)
            return;

        // once we've left the TextBox, return the select all behavior
        siteCatalogFilter.LostMouseCapture += SiteCatalogFilter_LostMouseCapture;
    }

    private void SiteCatalog_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var r = VisualTreeHelper.HitTest(this, e.GetPosition(this));

        if (r.VisualHit is Image || r.VisualHit is TextBlock)
        {
            var elem = (FrameworkElement)r.VisualHit;
            var context = elem.DataContext as CatalogInternetService;
            var hitTestServiceId = context?.Id;

            if (string.IsNullOrWhiteSpace(hitTestServiceId) ||
                !string.Equals(hitTestServiceId, ViewModel.SelectedService?.Id, StringComparison.Ordinal))
            {
                SiteCatalog.UnselectAll();
            }
        }
        else
            SiteCatalog.UnselectAll();
    }

    private void CategoryRadioButton_Click(object sender, RoutedEventArgs e)
    {
        var tag = (sender as FrameworkElement)?.Tag as CatalogInternetServiceCategory?;

        if (!tag.HasValue)
            return;

        foreach (var eachItem in SiteCatalog.Items)
        {
            var catalogItem = eachItem as CatalogInternetService;

            if (catalogItem == null)
                continue;

            if (catalogItem.Category != tag.Value)
                continue;

            SiteCatalog.SelectedItem = eachItem;

            if (GetScrollViewer(SiteCatalog) is ScrollViewer viewer)
                viewer.ScrollToBottom();

            SiteCatalog.ScrollIntoView(eachItem);
            break;
        }
    }
}
