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
    }

    public class NotesViewModel : ViewModelBase
    {
        private ObservableCollection<Contact> _contacts;
        private Contact _selectedContact;
        private ObservableCollection<SelectableNote> _selectableNotes;
        private SelectableNote _selectedSelectableNote;

        public NotesViewModel()
        {
            LoadContacts();

            OpenAddWindowCommand = new RelayCommand(OpenAddWindow);
            OpenEditWindowCommand = new RelayCommand(OpenEditWindow);
            DeleteSelectedNotesCommand = new RelayCommand(DeleteSelectedNotes);
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
            set => SetProperty(ref _selectedSelectableNote, value);
        }

        public ICommand OpenAddWindowCommand { get; }
        public ICommand OpenEditWindowCommand { get; }
        public ICommand DeleteSelectedNotesCommand { get; }

        private void LoadContacts()
        {
            var loaded = XmlFileManager.ContactLoading();
            Contacts = new ObservableCollection<Contact>(loaded.Where(x => !x.IsBlocked));
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
            if (_selectedContact == null) return;
            var allContacts = XmlFileManager.ContactLoading();
            var target = allContacts.FirstOrDefault(c => c.Name == _selectedContact.Name && c.Phone == _selectedContact.Phone);
            if (target != null)
            {
                target.Notes = new ObservableCollection<Note>(_selectableNotes.Select(sn => sn.Note));
                _selectedContact.Notes = target.Notes;
            }
            XmlFileManager.ContactManaging(allContacts);
        }

        private void OpenAddWindow()
        {
            if (_selectedContact == null)
            {
                MessageBox.Show("Először válasszon ki egy névjegyet!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                SaveChanges();
            }
        }

        private void OpenEditWindow()
        {
            if (SelectedSelectableNote?.Note != null)
            {
                var vm = new NoteEditViewModel(SelectedSelectableNote.Note);
                var win = new NoteEditWindow(vm);
                win.ShowDialog();

                if (vm.IsSaved)
                {
                    vm.UpdateNoteModel(SelectedSelectableNote.Note);
                    SaveChanges();

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
            if (_selectedContact == null)
            {
                MessageBox.Show("Először válasszon ki egy névjegyet!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                SaveChanges();
                SelectedSelectableNote = null;
            }
        }
    }
}