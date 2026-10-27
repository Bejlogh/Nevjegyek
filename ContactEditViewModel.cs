using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Névjegyzék
{
    public class ContactEditViewModel : ViewModelBase
    {
        private string _contactName;
        private string _contactEmail;
        private string _contactPhone;
        private string _contactPosition;
        private string _contactAddress;
        private string _contactBirthDate;
        private string _contactWebsite;
        private string _contactImagePath;
        private BitmapImage _contactImageSource;
        private bool _isBlocked;

        public Window Window { get; set; }
        public bool IsSaved { get; private set; } = false;

        public string ContactName { get => _contactName; set => SetProperty(ref _contactName, value); }
        public string ContactEmail { get => _contactEmail; set => SetProperty(ref _contactEmail, value); }
        public string ContactPhone { get => _contactPhone; set => SetProperty(ref _contactPhone, value); }
        public string ContactPosition { get => _contactPosition; set => SetProperty(ref _contactPosition, value); }
        public string ContactAddress { get => _contactAddress; set => SetProperty(ref _contactAddress, value); }
        public string ContactBirthDate { get => _contactBirthDate; set => SetProperty(ref _contactBirthDate, value); }
        public string ContactWebsite { get => _contactWebsite; set => SetProperty(ref _contactWebsite, value); }
        public string ContactImagePath { get => _contactImagePath; set => SetProperty(ref _contactImagePath, value); }
        public BitmapImage ContactImageSource
        {
            get => _contactImageSource;
            set => SetProperty(ref _contactImageSource, value);
        }
        public bool IsBlocked { get => _isBlocked; set => SetProperty(ref _isBlocked, value); }

        public ICommand UploadImageCommand { get; }
        public ICommand DeleteImageCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ContactEditViewModel(Contact contact = null)
        {
            if (contact != null)
            {
                _contactName = contact.Name;
                _contactEmail = contact.Email;
                _contactPhone = contact.Phone;
                _contactPosition = contact.Position;
                _contactAddress = contact.Address;
                _contactBirthDate = contact.BirthDate;
                _contactWebsite = contact.Website;
                _contactImagePath = contact.ImagePath;
                _contactImageSource = contact.ImageSource;
                _isBlocked = contact.IsBlocked;
            }

            UploadImageCommand = new RelayCommand(UploadImage);
            DeleteImageCommand = new RelayCommand(DeleteImage);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void UploadImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Képfájlok (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string imagesDir = XmlFileManager.GetImagesDirectoryPath();
                    string extension = Path.GetExtension(openFileDialog.FileName);
                    string newFileName = Guid.NewGuid().ToString() + extension;
                    string destinationPath = Path.Combine(imagesDir, newFileName);

                    File.Copy(openFileDialog.FileName, destinationPath, true);
                    ContactImagePath = destinationPath;
                    var temp = new Contact { ImagePath = destinationPath };
                    temp.LoadImageSource();
                    ContactImageSource = temp.ImageSource;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hiba a kép feltöltése közben: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteImage()
        {
            var imageToDelete = ContactImagePath;
            ContactImagePath = string.Empty;
            ContactImageSource = null;

            if (!string.IsNullOrWhiteSpace(imageToDelete))
            {
                var temp = new Contact { ImagePath = imageToDelete };
                XmlFileManager.DeleteContactImage(temp);
            }
        }

        private bool NameMatch(string name) => !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[\p{L}\d\.\-\s]*$");
        private bool PhoneNumberMatch(string phone) => !string.IsNullOrEmpty(phone) && Regex.IsMatch(phone, @"^[0-9+]+$");
        private bool EmailMatch(string email) => string.IsNullOrEmpty(email) || Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private bool BirthDateMatch(string date) => string.IsNullOrEmpty(date) || Regex.IsMatch(date, @"^\d{4}[-\.]\d{2}[-\.]\d{2}\.?$");
        private bool WebsiteMatch(string site) => string.IsNullOrEmpty(site) || Regex.IsMatch(site, @"^(https?:\/\/)?([\w\-]+\.)+[\w\-]+(\/[^\s]*)?$");

        private bool ValidateFields()
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ContactName) || string.IsNullOrWhiteSpace(ContactPhone))
            {
                errors.Add("Név és telefonszám megadása kötelező. Példa: Név: Kovács Anna, Telefonszám: +36123456789");
            }
            if (!string.IsNullOrWhiteSpace(ContactName) && !NameMatch(ContactName))
            {
                errors.Add("Nem megfelelő név formátum. Példa: Kovács Anna");
            }
            if (!string.IsNullOrWhiteSpace(ContactPhone) && !PhoneNumberMatch(ContactPhone))
            {
                errors.Add("Nem megfelelő telefonszám formátum. Példa: +36123456789");
            }
            if (!string.IsNullOrWhiteSpace(ContactEmail) && !EmailMatch(ContactEmail))
            {
                errors.Add("Nem megfelelő email cím formátum. Példa: valaki@pelda.hu");
            }
            if (!string.IsNullOrWhiteSpace(ContactBirthDate) && !BirthDateMatch(ContactBirthDate))
            {
                errors.Add("Nem megfelelő születési dátum formátum. Példa: 1999-12-31");
            }
            if (!string.IsNullOrWhiteSpace(ContactWebsite) && !WebsiteMatch(ContactWebsite))
            {
                errors.Add("Nem megfelelő webhely formátum. Példa: https://www.pelda.hu");
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
            if (!ValidateFields()) return;
            IsSaved = true;
            Window?.Close();
        }

        private void Cancel()
        {
            IsSaved = false;
            Window?.Close();
        }

        public void UpdateContactModel(Contact contact)
        {
            if (contact == null)
            {
                return;
            }

            var oldImagePath = contact.ImagePath;

            contact.Name = ContactName;
            contact.Email = ContactEmail;
            contact.Phone = ContactPhone;
            contact.Position = ContactPosition;
            contact.Address = ContactAddress;
            contact.BirthDate = ContactBirthDate;
            contact.Website = ContactWebsite;
            contact.ImagePath = ContactImagePath;
            contact.ImageSource = ContactImageSource;
            contact.IsBlocked = IsBlocked;

            if (!string.IsNullOrWhiteSpace(oldImagePath)
                && !string.Equals(oldImagePath, ContactImagePath, StringComparison.OrdinalIgnoreCase))
            {
                XmlFileManager.DeleteContactImage(new Contact { ImagePath = oldImagePath });
            }
        }
    }
}