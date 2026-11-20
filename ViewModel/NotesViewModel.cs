using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Névjegyzék
{
    public class SelectableNote : ViewModelBase
    {
        public Note Note { get; set; }

        private bool _isSelectedForDelete;
        public bool IsSelectedForDelete
        {
            get => _isSelectedForDelete;
            set => SetProperty(ref _isSelectedForDelete, value);
        }

        private bool _isSelectedForAssignment;
        public bool IsSelectedForAssignment
        {
            get => _isSelectedForAssignment;
            set => SetProperty(ref _isSelectedForAssignment, value);
        }
    }

    public class NotesViewModel : ViewModelBase
    {
        private ObservableCollection<Contact> _contacts;
        private Contact _selectedContact;
        private ObservableCollection<SelectableNote> _selectableNotes;
        private SelectableNote _selectedSelectableNote;
        private ObservableCollection<SelectableNote> _allSelectableNotes;
        private SelectableNote _selectedAllSelectableNote;
        private Note _displayedNote;
        private bool _isSynchronizingSelection;

        public NotesViewModel()
        {
            LoadContacts();
            LoadAllNotes();

            OpenAddWindowCommand = new RelayCommand(OpenAddWindow);
            OpenEditWindowCommand = new RelayCommand(OpenEditWindow);
            DeleteSelectedNotesCommand = new RelayCommand(DeleteSelectedNotes);
            DeleteAllNotesCommand = new RelayCommand(DeleteAllNotes);
            AssignSelectedNotesCommand = new RelayCommand(AssignSelectedNotes);
        }

        public ObservableCollection<Contact> Contacts
        {
            get => _contacts;
            set => SetProperty(ref _contacts, value);
        }

        public Contact SelectedContact
        {
            get => _selectedContact;
            set
            {
                if (SetProperty(ref _selectedContact, value))
                {
                    LoadNotesForSelectedContact();
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
            set
            {
                if (SetProperty(ref _selectedSelectableNote, value))
                {
                    if (!_isSynchronizingSelection)
                    {
                        if (value != null)
                        {
                            DisplayedNote = value.Note;
                            _isSynchronizingSelection = true;
                            SelectedAllSelectableNote = null;
                            _isSynchronizingSelection = false;
                        }
                        else
                        {
                            DisplayedNote = null;
                        }
                    }
                    else if (value != null)
                    {
                        DisplayedNote = value.Note;
                    }
                }
            }
        }

        public Note DisplayedNote
        {
            get => _displayedNote;
            set => SetProperty(ref _displayedNote, value);
        }

        public ObservableCollection<SelectableNote> AllSelectableNotes
        {
            get => _allSelectableNotes;
            set => SetProperty(ref _allSelectableNotes, value);
        }

        public SelectableNote SelectedAllSelectableNote
        {
            get => _selectedAllSelectableNote;
            set
            {
                if (SetProperty(ref _selectedAllSelectableNote, value))
                {
                    if (!_isSynchronizingSelection)
                    {
                        if (value != null)
                        {
                            DisplayedNote = value.Note;
                            _isSynchronizingSelection = true;
                            SelectedSelectableNote = null;
                            _isSynchronizingSelection = false;
                        }
                        else
                        {
                            DisplayedNote = null;
                        }
                    }
                    else if (value != null)
                    {
                        DisplayedNote = value.Note;
                    }
                }
            }
        }

        public ICommand OpenAddWindowCommand { get; }
        public ICommand OpenEditWindowCommand { get; }
        public ICommand DeleteSelectedNotesCommand { get; }
        public ICommand DeleteAllNotesCommand { get; }
        public ICommand AssignSelectedNotesCommand { get; }

        private void LoadContacts()
        {
            var loaded = XmlFileManager.ContactLoading();
            Contacts = new ObservableCollection<Contact>(loaded.Where(x => !x.IsBlocked));
            foreach (var contact in Contacts)
            {
                contact.LoadImageSource();
            }
        }

        private void LoadAllNotes()
        {
            AllSelectableNotes = new ObservableCollection<SelectableNote>(
                XmlFileManager.LoadNotes()
                    .OrderByDescending(note => note.Id)
                    .Select(note => new SelectableNote { Note = note }));
        }

        private void LoadNotesForSelectedContact()
        {
            SelectableNotes = new ObservableCollection<SelectableNote>();
            if (_selectedContact?.Notes != null)
            {
                foreach (var note in _selectedContact.Notes)
                {
                    SelectableNotes.Add(new SelectableNote { Note = note });
                }
            }
            SelectedSelectableNote = null;
        }

        private void SaveChanges()
        {
            var allContacts = XmlFileManager.ContactLoading();
            var latestNotes = AllSelectableNotes?.Select(sn => sn.Note).ToList()
                ?? new System.Collections.Generic.List<Note>();

            foreach (var contact in allContacts)
            {
                foreach (var note in contact.Notes.ToList())
                {
                    var updated = latestNotes.FirstOrDefault(n => n.Id == note.Id);
                    if (updated != null)
                    {
                        var index = contact.Notes.IndexOf(note);
                        if (index >= 0)
                        {
                            contact.Notes[index] = updated;
                        }
                    }
                }
            }

            if (_selectedContact != null)
            {
                var target = allContacts.FirstOrDefault(c => c.Id == _selectedContact.Id)
                    ?? allContacts.FirstOrDefault(c => c.Name == _selectedContact.Name && c.Phone == _selectedContact.Phone);

                if (target != null)
                {
                    var assignedNotes = (SelectableNotes ?? new ObservableCollection<SelectableNote>())
                        .Select(sn => latestNotes.FirstOrDefault(n => n.Id == sn.Note.Id) ?? sn.Note)
                        .ToList();
                    target.Notes = new ObservableCollection<Note>(assignedNotes);
                    _selectedContact.Notes = target.Notes;
                }
            }

            XmlFileManager.SaveContactsAndNotes(
                allContacts,
                AllSelectableNotes?.Select(sn => sn.Note) ?? Enumerable.Empty<Note>());

            LoadNotesForSelectedContact();
            LoadAllNotes();
        }

        private bool HasDuplicateTitle(string title, Note noteToIgnore = null)
        {
            var allNotes = AllSelectableNotes?.Select(sn => sn.Note) ?? Enumerable.Empty<Note>();
            return allNotes.Any(note =>
                note != noteToIgnore &&
                string.Equals(note.Title?.Trim(), title?.Trim(), StringComparison.CurrentCultureIgnoreCase));
        }

        private void OpenAddWindow()
        {
            var vm = new NoteEditViewModel(isDuplicateTitle: title => HasDuplicateTitle(title));
            var win = Application.Current.Windows.OfType<NoteEditWindow>().FirstOrDefault() ?? new NoteEditWindow();
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
                var newNote = new Note();
                vm.UpdateNoteModel(newNote);
                AllSelectableNotes.Insert(0, new SelectableNote { Note = newNote });
                SaveChanges();
            }
        }

        private void OpenEditWindow()
        {
            var noteToEdit = SelectedSelectableNote?.Note ?? SelectedAllSelectableNote?.Note;

            if (noteToEdit != null)
            {
                var vm = new NoteEditViewModel(
                    noteToEdit,
                    title => HasDuplicateTitle(title, noteToEdit));

                var win = Application.Current.Windows.OfType<NoteEditWindow>().FirstOrDefault() ?? new NoteEditWindow();
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
                    vm.UpdateNoteModel(noteToEdit);

                    if (SelectedSelectableNote?.Note == noteToEdit)
                    {
                        var temp = SelectedSelectableNote;
                        SelectableNotes.Remove(temp);
                        SelectableNotes.Insert(0, temp);
                        SelectedSelectableNote = temp;
                    }

                    SaveChanges();
                }
            }
            else
            {
                MessageBox.Show("Válasszon ki egy jegyzetet a módosításhoz!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AssignSelectedNotes()
        {
            if (_selectedContact == null)
            {
                MessageBox.Show("Először válasszon ki egy névjegyet!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedNotes = AllSelectableNotes
                .Where(item => item.IsSelectedForAssignment)
                .Select(item => item.Note)
                .ToList();

            if (selectedNotes.Count == 0)
            {
                MessageBox.Show("Jelöljön ki legalább egy hozzárendelendő jegyzetet!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var item in AllSelectableNotes)
            {
                item.IsSelectedForAssignment = false;
            }

            var allContacts = XmlFileManager.ContactLoading();
            var target = allContacts.FirstOrDefault(c => c.Id == _selectedContact.Id)
                ?? allContacts.FirstOrDefault(c => c.Name == _selectedContact.Name && c.Phone == _selectedContact.Phone);

            if (target == null)
            {
                return;
            }

            var selectedIds = selectedNotes
                .Select(note => note.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var contact in allContacts.Where(contact => contact.Id != target.Id))
            {
                var remainingNotes = contact.Notes
                    .Where(note => !selectedIds.Contains(note.Id))
                    .ToList();
                contact.Notes = new ObservableCollection<Note>(remainingNotes);
            }

            foreach (var note in selectedNotes)
            {
                note.ContactId = target.Id;
                note.ContactName = target.Name;
                note.GroupId = string.Empty;

                if (!target.Notes.Any(existing => existing.Id == note.Id))
                {
                    target.Notes.Add(note);
                }
            }

            var groups = XmlFileManager.LoadGroups();
            foreach (var group in groups)
            {
                group.Notes = new ObservableCollection<Note>(
                    group.Notes.Where(note => !selectedIds.Contains(note.Id)));
            }
            XmlFileManager.SaveGroups(groups);

            XmlFileManager.SaveContactsAndNotes(allContacts, AllSelectableNotes.Select(item => item.Note));
            foreach (var item in AllSelectableNotes)
            {
                item.IsSelectedForAssignment = false;
            }

            _selectedContact.Notes = target.Notes;
            LoadNotesForSelectedContact();
            LoadAllNotes();
            SelectedAllSelectableNote = null;
        }

        private void DeleteSelectedNotes()
        {
            if (_selectedContact == null)
            {
                MessageBox.Show("Először válasszon ki egy névjegyet!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var toUnassign = (SelectableNotes ?? new ObservableCollection<SelectableNote>())
                .Where(item => item.IsSelectedForDelete)
                .ToList();

            if (toUnassign.Count == 0)
            {
                MessageBox.Show("Jelöljön ki legalább egy eltávolítandó jegyzetet!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Biztosan eltávolítja a kiválasztott {toUnassign.Count} jegyzetet erről a névjegyről?", "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            var unassignedIds = toUnassign.Select(item => item.Note.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var allContacts = XmlFileManager.ContactLoading();
            var target = allContacts.FirstOrDefault(c => c.Id == _selectedContact.Id)
                ?? allContacts.FirstOrDefault(c => c.Name == _selectedContact.Name && c.Phone == _selectedContact.Phone);

            if (target != null)
            {
                var remainingNotes = target.Notes
                    .Where(note => !unassignedIds.Contains(note.Id))
                    .ToList();
                target.Notes = new ObservableCollection<Note>(remainingNotes);
            }

            foreach (var item in toUnassign)
            {
                item.IsSelectedForDelete = false;

                var globalItem = AllSelectableNotes?.FirstOrDefault(
                    globalNote => globalNote.Note.Id == item.Note.Id);
                var noteToUnassign = globalItem?.Note ?? item.Note;
                noteToUnassign.ContactId = string.Empty;
                noteToUnassign.ContactName = string.Empty;
                noteToUnassign.GroupId = string.Empty;
            }

            XmlFileManager.SaveContactsAndNotes(
                allContacts,
                AllSelectableNotes?.Select(item => item.Note) ?? Enumerable.Empty<Note>());

            _selectedContact.Notes = target?.Notes ?? new ObservableCollection<Note>();
            LoadNotesForSelectedContact();
            SelectedSelectableNote = null;
            DisplayedNote = null;
        }

        private void DeleteAllNotes()
        {
            var toDelete = AllSelectableNotes == null
                ? new ObservableCollection<SelectableNote>()
                : new ObservableCollection<SelectableNote>(
                    AllSelectableNotes.Where(item => item.IsSelectedForDelete));

            if (toDelete.Count == 0)
            {
                MessageBox.Show("Jelöljön ki vagy válasszon ki törlendő jegyzetet!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Biztosan törli a kiválasztott {toDelete.Count} jegyzetet az összes jegyzet közül?", "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            var deletedIds = toDelete.Select(item => item.Note.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var allContacts = XmlFileManager.ContactLoading();
            foreach (var contact in allContacts)
            {
                contact.Notes = new ObservableCollection<Note>(
                    contact.Notes.Where(note => !deletedIds.Contains(note.Id)));
            }

            var remainingGlobalNotes = AllSelectableNotes
                .Where(item => !deletedIds.Contains(item.Note.Id))
                .Select(item => item.Note)
                .ToList();

            var groups = XmlFileManager.LoadGroups();
            foreach (var group in groups)
            {
                group.Notes = new ObservableCollection<Note>(
                    group.Notes.Where(note => !deletedIds.Contains(note.Id)));
            }
            XmlFileManager.SaveGroups(groups);

            XmlFileManager.SaveContactsAndNotes(allContacts, remainingGlobalNotes);

            foreach (var item in toDelete)
            {
                AllSelectableNotes.Remove(item);
                item.IsSelectedForDelete = false;
            }

            LoadContacts();

            if (_selectedContact != null)
            {
                var refreshedSelected = Contacts.FirstOrDefault(contact => contact.Id == _selectedContact.Id)
                    ?? Contacts.FirstOrDefault(contact =>
                        contact.Name == _selectedContact.Name && contact.Phone == _selectedContact.Phone);

                SelectedContact = refreshedSelected;
            }

            LoadNotesForSelectedContact();
            LoadAllNotes();
            SelectedSelectableNote = null;
            SelectedAllSelectableNote = null;
            DisplayedNote = null;
        }
    }
}
