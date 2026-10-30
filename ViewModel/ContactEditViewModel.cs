using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Névjegyzék
{
    public class ContactEditViewModel : ViewModelBase, IDataErrorInfo
    {
        private string _contactName;
        private string _contactEmail;
        private string _contactPhone;
        private string _contactPosition;
        private string _contactAddress;
        private DateTime? _contactBirthDate;
        private string _contactWebsite;
        private string _contactImagePath;
        private BitmapImage _contactImageSource;
        private bool _isBlocked;
        private readonly Func<string, bool> _isDuplicateName;
        private readonly Func<string, bool> _isDuplicateEmail;
        private readonly Func<string, bool> _isDuplicatePhone;

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(ContactName):
                        if (string.IsNullOrWhiteSpace(ContactName))
                        {
                            return "Név megadása kötelező.";
                        }
                        if (!NameMatch(ContactName))
                        {
                            return "Nem megfelelő név formátum. Példa: Kovács Anna";
                        }
                        if (_isDuplicateName?.Invoke(ContactName) == true)
                        {
                            return "Már létezik ilyen nevű névjegy!";
                        }
                        break;

                    case nameof(ContactPhone):
                        if (string.IsNullOrWhiteSpace(ContactPhone))
                        {
                            return "Telefonszám megadása kötelező.";
                        }
                        if (!PhoneNumberMatch(ContactPhone))
                        {
                            return "Nem megfelelő telefonszám formátum. Példa: +36123456789";
                        }
                        if (_isDuplicatePhone?.Invoke(ContactPhone) == true)
                        {
                            return "Ez a telefonszám már egy másik névjegynél szerepel!";
                        }
                        break;

                    case nameof(ContactEmail):
                        if (!string.IsNullOrWhiteSpace(ContactEmail) && !EmailMatch(ContactEmail))
                        {
                            return "Nem megfelelő email cím formátum. Példa: valaki@pelda.hu";
                        }
                        if (!string.IsNullOrWhiteSpace(ContactEmail) && _isDuplicateEmail?.Invoke(ContactEmail) == true)
                        {
                            return "Ez az email cím már egy másik névjegynél szerepel!";
                        }
                        break;

                    case nameof(ContactBirthDate):
                        if (!BirthDateMatch(ContactBirthDate))
                        {
                            return "A születési dátumnak legalább 18 éves kort kell jelentenie.";
                        }
                        break;

                    case nameof(ContactWebsite):
                        if (!string.IsNullOrWhiteSpace(ContactWebsite) && !WebsiteMatch(ContactWebsite))
                        {
                            return "Nem megfelelő webhely formátum. Példa: https://www.pelda.hu";
                        }
                        break;
                }

                return null;
            }
        }

        public Window Window { get; set; }
        public bool IsSaved { get; private set; } = false;

        public string ContactName { get => _contactName; set => SetProperty(ref _contactName, value); }
        public string ContactEmail { get => _contactEmail; set => SetProperty(ref _contactEmail, value); }
        public string ContactPhone { get => _contactPhone; set => SetProperty(ref _contactPhone, value); }
        public string ContactPosition { get => _contactPosition; set => SetProperty(ref _contactPosition, value); }
        public string ContactAddress { get => _contactAddress; set => SetProperty(ref _contactAddress, value); }
        public DateTime? ContactBirthDate
        {
            get => _contactBirthDate;
            set => SetProperty(ref _contactBirthDate, value);
        }
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

        public ContactEditViewModel(
            Contact contact = null,
            Func<string, bool> isDuplicateName = null,
            Func<string, bool> isDuplicateEmail = null,
            Func<string, bool> isDuplicatePhone = null)
        {
            _isDuplicateName = isDuplicateName;
            _isDuplicateEmail = isDuplicateEmail;
            _isDuplicatePhone = isDuplicatePhone;

            if (contact != null)
            {
                _contactName = contact.Name;
                _contactEmail = contact.Email;
                _contactPhone = contact.Phone;
                _contactPosition = contact.Position;
                _contactAddress = contact.Address;
                _contactBirthDate = ParseBirthDate(contact.BirthDate);
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

        public void UploadImageFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            string extension = Path.GetExtension(filePath);
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };

            if (Array.IndexOf(allowedExtensions, extension?.ToLowerInvariant()) < 0)
            {
                MessageBox.Show("Kérjük, képfájlt töltsön fel (.jpg, .jpeg, .png, .bmp).", "Érvénytelen fájl", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string imagesDir = XmlFileManager.GetImagesDirectoryPath();
                string newFileName = Guid.NewGuid().ToString() + extension;
                string destinationPath = Path.Combine(imagesDir, newFileName);

                File.Copy(filePath, destinationPath, true);
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

        private void UploadImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Képfájlok (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                UploadImageFromFile(openFileDialog.FileName);
            }
        }

        public bool IsSupportedImageFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            string extension = Path.GetExtension(filePath);
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
            return Array.IndexOf(allowedExtensions, extension?.ToLowerInvariant()) >= 0;
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

        private static DateTime? ParseBirthDate(string date)
        {
            if (string.IsNullOrWhiteSpace(date))
            {
                return null;
            }

            string[] formats =
            {
                "yyyy-MM-dd",
                "yyyy.MM.dd",
                "yyyy/MM/dd",
                "yyyy-M-d",
                "yyyy.M.d",
                "yyyy/M/d"
            };

            if (DateTime.TryParseExact(date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate;
            }

            return DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate)
                ? parsedDate
                : null;
        }

        private static string FormatBirthDate(DateTime? date) => date.HasValue ? date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;

        private bool NameMatch(string name) => !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[\p{L}\d\.\-\s]*$");
        private bool PhoneNumberMatch(string phone) => !string.IsNullOrEmpty(phone) && Regex.IsMatch(phone, @"^[0-9+]+$");
        private bool EmailMatch(string email) => string.IsNullOrEmpty(email) || Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private bool BirthDateMatch(DateTime? date)
        {
            if (!date.HasValue)
            {
                return true;
            }

            var minimumBirthDate = DateTime.Today.AddYears(-18);
            return date.Value <= DateTime.Today && date.Value <= minimumBirthDate;
        }
        private bool WebsiteMatch(string site) => string.IsNullOrEmpty(site) || Regex.IsMatch(site, @"^(https?:\/\/)?([\w\-]+\.)+[\w\-]+(\/[^\s]*)?$");

        private bool ValidateFields()
        {
            var propertyNames = new[]
            {
                nameof(ContactName),
                nameof(ContactPhone),
                nameof(ContactEmail),
                nameof(ContactBirthDate),
                nameof(ContactWebsite)
            };

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
            contact.BirthDate = FormatBirthDate(ContactBirthDate);
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
