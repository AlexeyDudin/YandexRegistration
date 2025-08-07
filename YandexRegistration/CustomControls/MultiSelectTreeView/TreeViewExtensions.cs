using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace YandexRegistration.CustomControls.MultiSelectTreeView;

public static class TreeViewExtensions
{
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.RegisterAttached("SelectedItems", typeof(IList),
            typeof(TreeViewExtensions),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemsChanged));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.RegisterAttached("SelectedItem", typeof(object),
            typeof(TreeViewExtensions),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    public static IList GetSelectedItems(DependencyObject obj)
    {
        return (IList)obj.GetValue(SelectedItemsProperty);
    }

    public static void SetSelectedItems(DependencyObject obj, IList value)
    {
        obj.SetValue(SelectedItemsProperty, value);
    }

    public static object GetSelectedItem(DependencyObject obj)
    {
        return obj.GetValue(SelectedItemProperty);
    }

    public static void SetSelectedItem(DependencyObject obj, object value)
    {
        obj.SetValue(SelectedItemProperty, value);
    }

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TreeView treeView) return;

        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= SelectedItems_CollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += SelectedItems_CollectionChanged;
        }

        void SelectedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var item in treeView.Items)
                {
                    if (treeView.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
                    {
                        treeViewItem.IsSelected = false;
                    }
                }
            }
            else
            {
                if (args.OldItems != null)
                {
                    foreach (var item in args.OldItems)
                    {
                        if (treeView.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
                        {
                            treeViewItem.IsSelected = false;
                        }
                    }
                }

                if (args.NewItems != null)
                {
                    foreach (var item in args.NewItems)
                    {
                        if (treeView.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
                        {
                            treeViewItem.IsSelected = true;
                        }
                    }
                }
            }
        }
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TreeView treeView) return;

        treeView.SelectedItemChanged -= TreeView_SelectedItemChanged;
        treeView.SelectedItemChanged += TreeView_SelectedItemChanged;

        static void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var treeView = (TreeView)sender;
            SetSelectedItem(treeView, e.NewValue);
        }
    }
}