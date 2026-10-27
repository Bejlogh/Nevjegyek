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
            var loadedGroups = XmlFileManager.LoadGroups();
            var currentGroupName = SelectedGroup?.Name;

            Groups = new ObservableCollection<ContactGroup>(loadedGroups);

            if (currentGroupName != null)
            {
                _selectedGroup = Groups.FirstOrDefault(g => g.Name == currentGroupName);
                OnPropertyChanged(nameof(SelectedGroup));
            }

            var globalContacts = XmlFileManager.ContactLoading();
            AvailableContacts.Clear();

            foreach (var c in globalContacts.Where(x => !x.IsBlocked))
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
                foreach (var c in SelectedGroup.Contacts.Where(x => !x.IsBlocked))
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
                foreach (var note in _selectedGroup.Notes)
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
            var vm = new GroupEditViewModel();
            var win = new GroupEditWindow(vm);
            win.ShowDialog();

            if (vm.IsSaved)
            {
                var newGroup = new ContactGroup { Name = vm.GroupName };
                Groups.Add(newGroup);
                SaveData();
            }
        }

        private void OpenEditGroupWindow()
        {
            if (SelectedGroup != null)
            {
                var vm = new GroupEditViewModel(SelectedGroup.Name);
                var win = new GroupEditWindow(vm);
                win.ShowDialog();

                if (vm.IsSaved)
                {
                    SelectedGroup.Name = vm.GroupName;
                    SaveData();

                    var temp = SelectedGroup;
                    Groups.Remove(temp);
                    Groups.Insert(0, temp);
                    SelectedGroup = temp;

                    MessageBox.Show("Sikeres módosítás!", "Információ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Válasszon ki egy csoportot a módosításhoz!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteGroup()
        {
            if (SelectedGroup != null)
            {
                if (MessageBox.Show("Biztosan törölni szeretné a csoportot?", "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                MessageBox.Show("Válasszon ki egy csoportot a törléshez!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddSelectedContacts()
        {
            if (SelectedGroup == null)
            {
                MessageBox.Show("Először válasszon ki egy csoportot!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItems = AvailableContacts.Where(x => x.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Jelöljön ki legalább egy névjegyet a listából!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var item in selectedItems)
            {
                if (!item.Contact.IsBlocked && !SelectedGroup.Contacts.Any(c => c.Name == item.Contact.Name && c.Phone == item.Contact.Phone))
                {
                    SelectedGroup.Contacts.Add(item.Contact);
                }
                item.IsSelected = false;
            }

            SaveData();
            UpdateGroupSelectableContacts();
        }

        private void RemoveSelectedContactsFromGroup()
        {
            if (SelectedGroup == null)
            {
                MessageBox.Show("Először válasszon ki egy csoportot!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var itemsToRemove = GroupSelectableContacts.Where(x => x.IsSelected).ToList();
            if (itemsToRemove.Count == 0)
            {
                MessageBox.Show("Jelöljön ki legalább egy eltávolítandó névjegyet a csoport tagjai közül!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Biztosan eltávolítja a kijelölt névjegyeket a csoportból?", "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var item in itemsToRemove)
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
                MessageBox.Show("Először válasszon ki egy csoportot!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var vm = new NoteEditViewModel();
            var win = new NoteEditWindow(vm);
            win.ShowDialog();

            if (vm.IsSaved)
            {
                var newNote = new Note();
                vm.UpdateNoteModel(newNote);

                SelectableNotes.Add(new SelectableNote { Note = newNote });
                SyncNotesToGroup();
            }
        }

        private void OpenEditNoteWindow()
        {
            if (SelectedSelectableNote?.Note != null)
            {
                var vm = new NoteEditViewModel(SelectedSelectableNote.Note);
                var win = new NoteEditWindow(vm);
                win.ShowDialog();

                if (vm.IsSaved)
                {
                    vm.UpdateNoteModel(SelectedSelectableNote.Note);
                    SyncNotesToGroup();

                    var temp = SelectedSelectableNote;
                    SelectableNotes.Remove(temp);
                    SelectableNotes.Insert(0, temp);
                    SelectedSelectableNote = temp;

                    MessageBox.Show("Sikeres módosítás!", "Információ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Válasszon ki egy jegyzetet a módosításhoz!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteSelectedNotes()
        {
            if (SelectedGroup == null)
            {
                MessageBox.Show("Először válasszon ki egy csoportot!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var toDelete = SelectableNotes.Where(x => x.IsSelectedForDelete).ToList();
            if (toDelete.Count == 0)
            {
                MessageBox.Show("Jelöljön ki legalább egy törlendő jegyzetet a jelölőnégyzettel!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Biztosan törölni szeretné a kiválasztott {toDelete.Count} jegyzetet?", "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var item in toDelete)
                {
                    SelectableNotes.Remove(item);
                }
                SyncNotesToGroup();
                SelectedSelectableNote = null;
            }
        }
    }
}