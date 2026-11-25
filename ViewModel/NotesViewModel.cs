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

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
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
            List<Contact> loaded = XmlFileManager.ContactLoading();
            Contacts = new ObservableCollection<Contact>(loaded.Where(x => !x.IsBlocked));
            foreach (Contact contact in Contacts)
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
                foreach (Note note in _selectedContact.Notes)
                {
                    SelectableNotes.Add(new SelectableNote { Note = note });
                }
            }
            SelectedSelectableNote = null;
        }

        private void SaveChanges()
        {
            List<Contact> allContacts = XmlFileManager.ContactLoading();
            List<Note> latestNotes = AllSelectableNotes?.Select(sn => sn.Note).ToList()
                ?? new System.Collections.Generic.List<Note>();

            foreach (Contact contact in allContacts)
            {
                foreach (Note note in contact.Notes.ToList())
                {
                    Note updated = latestNotes.FirstOrDefault(n => n.Id == note.Id);
                    if (updated != null)
                    {
                        int index = contact.Notes.IndexOf(note);
                        if (index >= 0)
                        {
                            contact.Notes[index] = updated;
                        }
                    }
                }
            }

            if (_selectedContact != null)
            {
                Contact? target = allContacts.FirstOrDefault(c => c.Id == _selectedContact.Id)
                    ?? allContacts.FirstOrDefault(c => c.Name == _selectedContact.Name && c.Phone == _selectedContact.Phone);

                if (target != null)
                {
                    List<Note> assignedNotes = (SelectableNotes ?? new ObservableCollection<SelectableNote>())
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
            IEnumerable<Note> allNotes = AllSelectableNotes?.Select(sn => sn.Note) ?? Enumerable.Empty<Note>();
            return allNotes.Any(note =>
                note != noteToIgnore &&
                string.Equals(note.Title?.Trim(), title?.Trim(), StringComparison.CurrentCultureIgnoreCase));
        }

        private void OpenAddWindow()
        {
            NoteEditViewModel vm = new NoteEditViewModel(isDuplicateTitle: title => HasDuplicateTitle(title));
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
                AllSelectableNotes.Insert(0, new SelectableNote { Note = newNote });
                SaveChanges();
            }
        }

        private void OpenEditWindow()
        {
            Note? noteToEdit = SelectedSelectableNote?.Note ?? SelectedAllSelectableNote?.Note;

            if (noteToEdit != null)
            {
                NoteEditViewModel vm = new NoteEditViewModel(
                    noteToEdit,
                    title => HasDuplicateTitle(title, noteToEdit));

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
                    vm.UpdateNoteModel(noteToEdit);

                    if (SelectedSelectableNote?.Note == noteToEdit)
                    {
                        SelectableNote temp = SelectedSelectableNote;
                        SelectableNotes.Remove(temp);
                        SelectableNotes.Insert(0, temp);
                        SelectedSelectableNote = temp;
                    }

                    SaveChanges();
                }
            }
            else
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectNoteToEdit"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AssignSelectedNotes()
        {
            if (_selectedContact == null)
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectContactFirst"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<Note> selectedNotes = AllSelectableNotes
                .Where(item => item.IsSelected)
                .Select(item => item.Note)
                .ToList();

            if (selectedNotes.Count == 0)
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectNotesToAssign"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (SelectableNote item in AllSelectableNotes)
            {
                item.IsSelected = false;
            }

            List<Contact> allContacts = XmlFileManager.ContactLoading();
            Contact? target = allContacts.FirstOrDefault(c => c.Id == _selectedContact.Id)
                ?? allContacts.FirstOrDefault(c => c.Name == _selectedContact.Name && c.Phone == _selectedContact.Phone);

            if (target == null)
            {
                return;
            }

            HashSet<string> selectedIds = selectedNotes
                .Select(note => note.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (Contact contact in allContacts.Where(contact => contact.Id != target.Id))
            {
                List<Note> remainingNotes = contact.Notes
                    .Where(note => !selectedIds.Contains(note.Id))
                    .ToList();
                contact.Notes = new ObservableCollection<Note>(remainingNotes);
            }

            foreach (Note note in selectedNotes)
            {
                note.ContactId = target.Id;
                note.ContactName = target.Name;
                note.GroupId = string.Empty;

                if (!target.Notes.Any(existing => existing.Id == note.Id))
                {
                    target.Notes.Add(note);
                }
            }

            List<ContactGroup> groups = XmlFileManager.LoadGroups();
            foreach (ContactGroup group in groups)
            {
                group.Notes = new ObservableCollection<Note>(
                    group.Notes.Where(note => !selectedIds.Contains(note.Id)));
            }
            XmlFileManager.SaveGroups(groups);

            XmlFileManager.SaveContactsAndNotes(allContacts, AllSelectableNotes.Select(item => item.Note));

            _selectedContact.Notes = target.Notes;
            LoadNotesForSelectedContact();
            LoadAllNotes();
            SelectedAllSelectableNote = null;
        }

        private void DeleteSelectedNotes()
        {
            if (_selectedContact == null)
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectContactFirst"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<SelectableNote> toUnassign = (SelectableNotes ?? new ObservableCollection<SelectableNote>())
                .Where(item => item.IsSelectedForDelete)
                .ToList();

            if (toUnassign.Count == 0)
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectNotesToRemove"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(string.Format(LanguageManager.GetString("Dialogs_ConfirmRemoveNotesFromContact"), toUnassign.Count), LanguageManager.GetString("Dialogs_ConfirmationTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            HashSet<string> unassignedIds = toUnassign.Select(item => item.Note.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<Contact> allContacts = XmlFileManager.ContactLoading();
            Contact? target = allContacts.FirstOrDefault(c => c.Id == _selectedContact.Id)
                ?? allContacts.FirstOrDefault(c => c.Name == _selectedContact.Name && c.Phone == _selectedContact.Phone);

            if (target != null)
            {
                List<Note> remainingNotes = target.Notes
                    .Where(note => !unassignedIds.Contains(note.Id))
                    .ToList();
                target.Notes = new ObservableCollection<Note>(remainingNotes);
            }

            foreach (SelectableNote item in toUnassign)
            {
                item.IsSelectedForDelete = false;

                SelectableNote? globalItem = AllSelectableNotes?.FirstOrDefault(
                    globalNote => globalNote.Note.Id == item.Note.Id);
                Note noteToUnassign = globalItem?.Note ?? item.Note;
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
            ObservableCollection<SelectableNote> toDelete = AllSelectableNotes == null
                ? new ObservableCollection<SelectableNote>()
                : new ObservableCollection<SelectableNote>(
                    AllSelectableNotes.Where(item => item.IsSelected));

            if (toDelete.Count == 0)
            {
                MessageBox.Show(LanguageManager.GetString("Dialogs_SelectNotesToDelete"), LanguageManager.GetString("Dialogs_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(string.Format(LanguageManager.GetString("Dialogs_ConfirmDeleteAllNotes"), toDelete.Count), LanguageManager.GetString("Dialogs_ConfirmationTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            HashSet<string> deletedIds = toDelete.Select(item => item.Note.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<Contact> allContacts = XmlFileManager.ContactLoading();
            foreach (Contact contact in allContacts)
            {
                contact.Notes = new ObservableCollection<Note>(
                    contact.Notes.Where(note => !deletedIds.Contains(note.Id)));
            }

            List<Note> remainingGlobalNotes = AllSelectableNotes
                .Where(item => !deletedIds.Contains(item.Note.Id))
                .Select(item => item.Note)
                .ToList();

            List<ContactGroup> groups = XmlFileManager.LoadGroups();
            foreach (ContactGroup group in groups)
            {
                group.Notes = new ObservableCollection<Note>(
                    group.Notes.Where(note => !deletedIds.Contains(note.Id)));
            }
            XmlFileManager.SaveGroups(groups);

            XmlFileManager.SaveContactsAndNotes(allContacts, remainingGlobalNotes);

            foreach (SelectableNote item in toDelete)
            {
                AllSelectableNotes.Remove(item);
                item.IsSelected = false;
            }

            LoadContacts();

            if (_selectedContact != null)
            {
                Contact? refreshedSelected = Contacts.FirstOrDefault(contact => contact.Id == _selectedContact.Id)
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
