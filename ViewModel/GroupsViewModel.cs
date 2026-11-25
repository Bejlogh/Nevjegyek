using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Névjegyzék
{
    public class GroupSelectableContact : ViewModelBase
    {
        public Contact Contact { get; set; }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public class GroupsViewModel : ViewModelBase
    {
        private ObservableCollection<ContactGroup> _groups;
        private ContactGroup _selectedGroup;
        private ObservableCollection<SelectableNote> _selectableNotes;
        private SelectableNote _selectedSelectableNote;

        public ObservableCollection<GroupSelectableContact> AvailableContacts { get; set; } = new ObservableCollection<GroupSelectableContact>();
        public ObservableCollection<GroupSelectableContact> GroupSelectableContacts { get; set; } = new ObservableCollection<GroupSelectableContact>();

        public GroupsViewModel()
        {
            LoadData();

            OpenAddGroupWindowCommand = new RelayCommand(OpenAddGroupWindow);
            OpenEditGroupWindowCommand = new RelayCommand(OpenEditGroupWindow);
            DeleteGroupCommand = new RelayCommand(DeleteGroup);

            AddSelectedContactsCommand = new RelayCommand(AddSelectedContacts);
            RemoveSelectedContactsFromGroupCommand = new RelayCommand(RemoveSelectedContactsFromGroup);

            OpenAddNoteWindowCommand = new RelayCommand(OpenAddNoteWindow);
            OpenEditNoteWindowCommand = new RelayCommand(OpenEditNoteWindow);
            DeleteSelectedNotesCommand = new RelayCommand(DeleteSelectedNotes);
        }

        public ObservableCollection<ContactGroup> Groups
        {
            get => _groups;
            set => SetProperty(ref _groups, value);
        }

        public ContactGroup SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                if (SetProperty(ref _selectedGroup, value))
                {
                    SelectedSelectableNote = null;
                    UpdateGroupSelectableContacts();
                    LoadNotesForSelectedGroup();
                }
            }
        }

        public ObservableCollection<SelectableNote> SelectableNotes
        {
            get => _selectableNotes;
            set => SetProperty(ref _selectableNotes, value);
        }

        public SelectableNote SelectedSelectableNote
        {
            get => _selectedSelectableNote;
            set => SetProperty(ref _selectedSelectableNote, value);
        }

        public ICommand OpenAddGroupWindowCommand { get; }
        public ICommand OpenEditGroupWindowCommand { get; }
        public ICommand DeleteGroupCommand { get; }
        public ICommand AddSelectedContactsCommand { get; }
        public ICommand RemoveSelectedContactsFromGroupCommand { get; }
        public ICommand OpenAddNoteWindowCommand { get; }
        public ICommand OpenEditNoteWindowCommand { get; }
        public ICommand DeleteSelectedNotesCommand { get; }

        private void LoadData()
        {
            RefreshData();
        }

        private void RefreshData()
        {
            List<ContactGroup> loadedGroups = XmlFileManager.LoadGroups();
            string? currentGroupName = SelectedGroup?.Name;

            Groups = new ObservableCollection<ContactGroup>(loadedGroups);

            if (currentGroupName != null)
            {
                _selectedGroup = Groups.FirstOrDefault(g => g.Name == currentGroupName);
                OnPropertyChanged(nameof(SelectedGroup));
            }

            List<Contact> globalContacts = XmlFileManager.ContactLoading();
            AvailableContacts.Clear();

            foreach (Contact c in globalContacts.Where(x => !x.IsBlocked))
            {
                c.LoadImageSource();
                AvailableContacts.Add(new GroupSelectableContact { Contact = c, IsSelected = false });
            }

            UpdateGroupSelectableContacts();
            LoadNotesForSelectedGroup();
        }

        private void SaveData()
        {
            XmlFileManager.SaveGroups(new List<ContactGroup>(Groups));
        }

        private void UpdateGroupSelectableContacts()
        {
            GroupSelectableContacts.Clear();
            if (SelectedGroup?.Contacts != null)
            {
                foreach (Contact c in SelectedGroup.Contacts.Where(x => !x.IsBlocked))
                {
                    GroupSelectableContacts.Add(new GroupSelectableContact { Contact = c, IsSelected = false });
                }
            }
        }

        private void LoadNotesForSelectedGroup()
        {
            SelectableNotes = new ObservableCollection<SelectableNote>();
            if (_selectedGroup?.Notes != null)
            {
                foreach (Note note in _selectedGroup.Notes)
                {
                    SelectableNotes.Add(new SelectableNote { Note = note });
                }
            }
            SelectedSelectableNote = null;
        }

        private void SyncNotesToGroup()
        {
            if (_selectedGroup == null) return;
            _selectedGroup.Notes = new ObservableCollection<Note>(_selectableNotes.Select(sn => sn.Note));
            SaveData();
        }

        private void OpenAddGroupWindow()
        {
            GroupEditViewModel vm = new GroupEditViewModel();
            GroupEditWindow win = Application.Current.Windows.OfType<GroupEditWindow>().FirstOrDefault() ?? new GroupEditWindow();
            win.SetViewModel(vm);

            if (win.WindowState == WindowState.Minimized)
            {
                win.WindowState = WindowState.Normal;
            }

            if (!win.IsVisible)
            {
                win.ShowDialog();
            }
            else
            {
                win.Activate();
            }

            if (vm.IsSaved)
            {
                ContactGroup newGroup = new ContactGroup { Name = vm.GroupName };
                Groups.Add(newGroup);
                SaveData();
            }
        }

        private void OpenEditGroupWindow()
        {
            if (SelectedGroup != null)
            {
                GroupEditViewModel vm = new GroupEditViewModel(SelectedGroup.Name);
                GroupEditWindow win = Application.Current.Windows.OfType<GroupEditWindow>().FirstOrDefault() ?? new GroupEditWindow();
                win.SetViewModel(vm);

                if (win.WindowState == WindowState.Minimized)
                {
                    win.WindowState = WindowState.Normal;
                }

                if (!win.IsVisible)
                {
                    win.ShowDialog();
                }
                else
                {
                    win.Activate();
                }

                if (vm.IsSaved)
                {
                    SelectedGroup.Name = vm.GroupName;
                    SaveData();

                    ContactGroup temp = SelectedGroup;
                    Groups.Remove(temp);
                    Groups.Insert(0, temp);
                    SelectedGroup = temp;

                }
            }
            else
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectGroupToEdit"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteGroup()
        {
            if (SelectedGroup != null)
            {
                if (MessageBox.Show(LanguageManager.GetString("Dialogs_ConfirmDeleteGroup"), LanguageManager.GetString("Dialogs_ConfirmationTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Groups.Remove(SelectedGroup);
                    SaveData();
                    SelectedGroup = null;
                    UpdateGroupSelectableContacts();
                    LoadNotesForSelectedGroup();
                }
            }
            else
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectGroupToDelete"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void AddContactsToSelectedGroup(IEnumerable<Contact> contacts)
        {
            if (SelectedGroup == null)
            {
                return;
            }

            List<Contact> contactsToAdd = contacts?.Where(c => c != null && !c.IsBlocked).ToList() ?? new List<Contact>();
            if (contactsToAdd.Count == 0)
            {
                return;
            }

            foreach (Contact contact in contactsToAdd)
            {
                if (!SelectedGroup.Contacts.Any(c => c.Name == contact.Name && c.Phone == contact.Phone))
                {
                    SelectedGroup.Contacts.Add(contact);
                }
            }

            foreach (GroupSelectableContact item in AvailableContacts.ToList())
            {
                item.IsSelected = false;
            }

            SaveData();
            UpdateGroupSelectableContacts();
        }

        public void ClearAvailableContactSelection()
        {
            foreach (GroupSelectableContact item in AvailableContacts.ToList())
            {
                item.IsSelected = false;
            }
        }

        private void AddSelectedContacts()
        {
            if (SelectedGroup == null)
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectGroupFirst"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<GroupSelectableContact> selectedItems = AvailableContacts.Where(x => x.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectContactFromList"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AddContactsToSelectedGroup(selectedItems.Select(x => x.Contact));

            foreach (GroupSelectableContact item in selectedItems)
            {
                item.IsSelected = false;
            }
        }

        private void RemoveSelectedContactsFromGroup()
        {
            if (SelectedGroup == null)
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectGroupFirst"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<GroupSelectableContact> itemsToRemove = GroupSelectableContacts.Where(x => x.IsSelected).ToList();
            if (itemsToRemove.Count == 0)
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectContactToRemove"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(LanguageManager.GetString("Dialogs_ConfirmRemoveContactsFromGroup"), LanguageManager.GetString("Dialogs_ConfirmationTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (GroupSelectableContact item in itemsToRemove)
                {
                    SelectedGroup.Contacts.Remove(item.Contact);
                }
                SaveData();
                UpdateGroupSelectableContacts();
            }
        }

        private void OpenAddNoteWindow()
        {
            if (SelectedGroup == null)
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectGroupFirst"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NoteEditViewModel vm = new NoteEditViewModel();
            NoteEditWindow win = Application.Current.Windows.OfType<NoteEditWindow>().FirstOrDefault() ?? new NoteEditWindow();
            win.SetViewModel(vm);

            if (win.WindowState == WindowState.Minimized)
            {
                win.WindowState = WindowState.Normal;
            }

            if (!win.IsVisible)
            {
                win.ShowDialog();
            }
            else
            {
                win.Activate();
            }

            if (vm.IsSaved)
            {
                Note newNote = new Note();
                vm.UpdateNoteModel(newNote);
                newNote.GroupId = SelectedGroup.Id;
                newNote.ContactName = SelectedGroup.Name;

                SelectableNotes.Add(new SelectableNote { Note = newNote });
                SyncNotesToGroup();
            }
        }

        private void OpenEditNoteWindow()
        {
            if (SelectedSelectableNote?.Note != null)
            {
                NoteEditViewModel vm = new NoteEditViewModel(SelectedSelectableNote.Note);
                NoteEditWindow win = Application.Current.Windows.OfType<NoteEditWindow>().FirstOrDefault() ?? new NoteEditWindow();
                win.SetViewModel(vm);

                if (win.WindowState == WindowState.Minimized)
                {
                    win.WindowState = WindowState.Normal;
                }

                if (!win.IsVisible)
                {
                    win.ShowDialog();
                }
                else
                {
                    win.Activate();
                }

                if (vm.IsSaved)
                {
                    vm.UpdateNoteModel(SelectedSelectableNote.Note);
                    SyncNotesToGroup();

                    SelectableNote temp = SelectedSelectableNote;
                    SelectableNotes.Remove(temp);
                    SelectableNotes.Insert(0, temp);
                    SelectedSelectableNote = temp;

                }
            }
            else
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectNoteToEdit"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteSelectedNotes()
        {
            if (SelectedGroup == null)
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectGroupFirst"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<SelectableNote> toDelete = SelectableNotes.Where(x => x.IsSelectedForDelete).ToList();
            if (toDelete.Count == 0)
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectGroupNotesToDelete"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(string.Format(LanguageManager.GetString("Dialogs_ConfirmDeleteGroupNotes"), toDelete.Count), LanguageManager.GetString("Dialogs_ConfirmationTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (SelectableNote item in toDelete)
                {
                    SelectableNotes.Remove(item);
                }
                SyncNotesToGroup();
                SelectedSelectableNote = null;
            }
        }
    }
}