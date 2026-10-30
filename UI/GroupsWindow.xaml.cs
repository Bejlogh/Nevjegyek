using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Névjegyzék
{
    public partial class GroupsWindow : Window
    {
        private Point _dragStartPoint;
        private bool _checkboxInteractionInProgress;

        public GroupsWindow()
        {
            InitializeComponent();
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T result)
                {
                    return result;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private void AvailableContactCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _checkboxInteractionInProgress = true;
            _dragStartPoint = default;
        }

        private void AvailableContactCheckBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _checkboxInteractionInProgress = false;
            _dragStartPoint = default;
        }

        private void AvailableContactsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var hit = e.OriginalSource as DependencyObject;
            if (FindAncestor<CheckBox>(hit) != null)
            {
                _checkboxInteractionInProgress = true;
                _dragStartPoint = default;
                return;
            }

            _dragStartPoint = e.GetPosition(this);
        }

        private void AvailableContactsList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _checkboxInteractionInProgress = false;
            _dragStartPoint = default;
        }

        private void AvailableContactsList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (_checkboxInteractionInProgress)
            {
                return;
            }

            var hit = e.OriginalSource as DependencyObject;
            if (FindAncestor<CheckBox>(hit) != null)
            {
                return;
            }

            var currentPoint = e.GetPosition(this);
            if (Math.Abs(currentPoint.X - _dragStartPoint.X) < 5 && Math.Abs(currentPoint.Y - _dragStartPoint.Y) < 5)
            {
                return;
            }

            if (FindAncestor<ListBoxItem>(hit) == null)
            {
                return;
            }

            var viewModel = listBox.DataContext as GroupsViewModel;
            var checkedContacts = viewModel?.AvailableContacts
                .Where(x => x.IsSelected)
                .Select(x => x.Contact)
                .Distinct()
                .ToList() ?? new List<Contact>();

            if (checkedContacts.Count == 0)
            {
                return;
            }

            DragDrop.DoDragDrop(listBox, new DataObject(typeof(List<Contact>), checkedContacts), DragDropEffects.Copy);
        }

        private void GroupMembersList_DragEnter(object sender, DragEventArgs e)
        {
            if (DataContext is GroupsViewModel viewModel && viewModel.SelectedGroup != null && e.Data.GetDataPresent(typeof(List<Contact>)))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
                return;
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void GroupMembersList_DragOver(object sender, DragEventArgs e)
        {
            GroupMembersList_DragEnter(sender, e);
        }

        private void GroupMembersList_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is not GroupsViewModel viewModel || viewModel.SelectedGroup == null)
            {
                return;
            }

            if (!e.Data.GetDataPresent(typeof(List<Contact>)))
            {
                return;
            }

            var droppedContacts = e.Data.GetData(typeof(List<Contact>)) as List<Contact>;
            if (droppedContacts == null || droppedContacts.Count == 0)
            {
                return;
            }

            viewModel.AddContactsToSelectedGroup(droppedContacts);

            if (DataContext is GroupsViewModel groupsViewModel)
            {
                groupsViewModel.ClearAvailableContactSelection();
            }

            e.Handled = true;
        }
    }
}
