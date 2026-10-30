using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Névjegyzék
{
    public class NoteEditViewModel : ViewModelBase, IDataErrorInfo
    {
        private string _noteTitle;
        private string _noteDescription;
        private readonly Func<string, bool> _isDuplicateTitle;

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(NoteTitle):
                        if (string.IsNullOrWhiteSpace(NoteTitle))
                        {
                            return "A jegyzet címét meg kell adni!";
                        }
                        if (_isDuplicateTitle?.Invoke(NoteTitle) == true)
                        {
                            return "Már létezik ilyen nevű jegyzet ennél a névjegynél!";
                        }
                        break;

                    case nameof(NoteDescription):
                        if (string.IsNullOrWhiteSpace(NoteDescription))
                        {
                            return "A jegyzet szövegét meg kell adni!";
                        }
                        if (NoteDescription.Length < 30)
                        {
                            return "Legalább 30 karakter hosszú szöveg kell!";
                        }
                        if (NoteDescription.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length < 3)
                        {
                            return "A jegyzet szövegének legalább 3 szót kell tartalmaznia!";
                        }
                        break;
                }

                return null;
            }
        }

        public Window Window { get; set; }
        public bool IsSaved { get; private set; } = false;

        public string NoteTitle
        {
            get => _noteTitle;
            set => SetProperty(ref _noteTitle, value);
        }

        public string NoteDescription
        {
            get => _noteDescription;
            set => SetProperty(ref _noteDescription, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public NoteEditViewModel(Note note = null, Func<string, bool> isDuplicateTitle = null)
        {
            _isDuplicateTitle = isDuplicateTitle;

            if (note != null)
            {
                _noteTitle = note.Title;
                _noteDescription = note.Description;
            }

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        private bool ValidateFields()
        {
            var propertyNames = new[] { nameof(NoteTitle), nameof(NoteDescription) };

            foreach (var propertyName in propertyNames)
            {
                var error = this[propertyName];
                if (!string.IsNullOrWhiteSpace(error))
                {
                    return false;
                }
            }

            return true;
        }

        private void Save()
        {
            if (!ValidateFields())
            {
                return;
            }

            IsSaved = true;
            Window?.Close();
        }

        private void Cancel()
        {
            IsSaved = false;
            Window?.Close();
        }

        public void UpdateNoteModel(Note note)
        {
            note.Title = NoteTitle;
            note.Description = NoteDescription;
        }
    }
}