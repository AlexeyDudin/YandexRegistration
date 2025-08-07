using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace YandexRegistration.CustomControls.MultiSelectTreeView;
public class MultiSelectTreeView : TreeView
{
    private object _lastSelectedItem;

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseDown(e);

        if (e.OriginalSource is DependencyObject source)
        {
            var container = ItemsControl.ContainerFromElement(this, source) as TreeViewItem;
            if (container != null)
            {
                var item = container.Header;
                var selectedItems = TreeViewExtensions.GetSelectedItems(this) as IList;

                if (selectedItems == null) return;

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    // Ctrl+Click - toggle selection
                    if (selectedItems.Contains(item))
                    {
                        selectedItems.Remove(item);
                        _lastSelectedItem = null;
                    }
                    else
                    {
                        selectedItems.Add(item);
                        _lastSelectedItem = item;
                    }
                }
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && _lastSelectedItem != null)
                {
                    // Shift+Click - select range
                    var items = this.ItemsSource as IList;
                    if (items == null) return;

                    int lastIndex = items.IndexOf(_lastSelectedItem);
                    int currentIndex = items.IndexOf(item);

                    selectedItems.Clear();

                    int start = Math.Min(lastIndex, currentIndex);
                    int end = Math.Max(lastIndex, currentIndex);

                    for (int i = start; i <= end; i++)
                    {
                        selectedItems.Add(items[i]);
                    }
                }
                else
                {
                    // Simple click - single selection
                    selectedItems.Clear();
                    selectedItems.Add(item);
                    _lastSelectedItem = item;
                }
            }
        }
    }
}