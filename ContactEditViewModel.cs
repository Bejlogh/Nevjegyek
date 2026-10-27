using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
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
                    string imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    if (!Directory.Exists(imagesDir))
                    {
                        Directory.CreateDirectory(imagesDir);
                    }

                    string extension = Path.GetExtension(openFileDialog.FileName);
                    string newFileName = Guid.NewGuid().ToString() + extension;
                    string destinationPath = Path.Combine(imagesDir, newFileName);

                    File.Copy(openFileDialog.FileName, destinationPath, true);
                    ContactImagePath = destinationPath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hiba a kép feltöltése közben: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteImage()
        {
            ContactImagePath = string.Empty;
        }

        private bool NameMatch(string name) => !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[\p{L}\d\.\-\s]*$");
        private bool PhoneNumberMatch(string phone) => !string.IsNullOrEmpty(phone) && Regex.IsMatch(phone, @"^[0-9+]+$");
        private bool EmailMatch(string email) => string.IsNullOrEmpty(email) || Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private bool BirthDateMatch(string date) => string.IsNullOrEmpty(date) || Regex.IsMatch(date, @"^\d{4}[-\.]\d{2}[-\.]\d{2}\.?$");
        private bool WebsiteMatch(string site) => string.IsNullOrEmpty(site) || Regex.IsMatch(site, @"^(https?:\/\/)?([\w\-]+\.)+[\w\-]+(\/[^\s]*)?$");

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(ContactName) || string.IsNullOrEmpty(ContactPhone))
            {
                MessageBox.Show("Név és telefonszám megadása kötelező", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!NameMatch(ContactName)) { MessageBox.Show("Nem megfelelő név formátum", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (!PhoneNumberMatch(ContactPhone)) { MessageBox.Show("Nem megfelelő telefonszám formátum", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (!EmailMatch(ContactEmail)) { MessageBox.Show("Nem megfelelő email cím formátum", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (!BirthDateMatch(ContactBirthDate)) { MessageBox.Show("Nem megfelelő születési dátum formátum (ÉÉÉÉ-HH-NN)", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
            if (!WebsiteMatch(ContactWebsite)) { MessageBox.Show("Nem megfelelő webhely formátum", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
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
            contact.Name = ContactName;
            contact.Email = ContactEmail;
            contact.Phone = ContactPhone;
            contact.Position = ContactPosition;
            contact.Address = ContactAddress;
            contact.BirthDate = ContactBirthDate;
            contact.Website = ContactWebsite;
            contact.ImagePath = ContactImagePath;
            contact.IsBlocked = IsBlocked;
        }
    }
}