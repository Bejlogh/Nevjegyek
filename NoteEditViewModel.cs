using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace Névjegyzék
{
    public class NoteEditViewModel : ViewModelBase
    {
        private string _noteTitle;
        private string _noteDescription;

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

        public NoteEditViewModel(Note note = null)
        {
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
            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(NoteTitle))
            {
                errors.Add("A jegyzet címét meg kell adni!");
            }

            if (string.IsNullOrWhiteSpace(NoteDescription))
            {
                errors.Add("A jegyzet szövegét meg kell adni!");
            }

            if (!string.IsNullOrWhiteSpace(NoteDescription) && NoteDescription.Length < 30)
            {
                errors.Add("Legalább 30 karakter hosszú szöveg kell!");
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join(Environment.NewLine, errors), "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
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