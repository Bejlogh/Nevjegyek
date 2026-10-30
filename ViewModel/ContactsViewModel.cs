using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Névjegyzék
{
    public class SelectableContact : ViewModelBase
    {
        public Contact Contact { get; set; }

        private bool _isSelectedForDelete;
        public bool IsSelectedForDelete
        {
            get => _isSelectedForDelete;
            set => SetProperty(ref _isSelectedForDelete, value);
        }
    }

    public class ContactsViewModel : ViewModelBase
    {
        private ObservableCollection<SelectableContact> _contacts;
        private SelectableContact _selectedContact;

        public ContactsViewModel()
        {
            LoadContacts();

            OpenAddWindowCommand = new RelayCommand(OpenAddWindow);
            OpenEditWindowCommand = new RelayCommand(OpenEditWindow);
            DeleteSelectedContactsCommand = new RelayCommand(DeleteSelectedContacts);
            BlockSelectedContactsCommand = new RelayCommand(BlockSelectedContacts);
            UnblockSelectedContactsCommand = new RelayCommand(UnblockSelectedContacts);
        }

        public ObservableCollection<SelectableContact> Contacts
        {
            get => _contacts;
            set => SetProperty(ref _contacts, value);
        }

        public SelectableContact SelectedContact
        {
            get => _selectedContact;
            set => SetProperty(ref _selectedContact, value);
        }

        public ICommand OpenAddWindowCommand { get; }
        public ICommand OpenEditWindowCommand { get; }
        public ICommand DeleteSelectedContactsCommand { get; }
        public ICommand BlockSelectedContactsCommand { get; }
        public ICommand UnblockSelectedContactsCommand { get; }

        private void LoadContacts()
        {
            var loaded = XmlFileManager.ContactLoading();
            var list = new List<SelectableContact>();
            foreach (var c in loaded)
            {
                c.LoadImageSource();
                list.Add(new SelectableContact { Contact = c });
            }
            Contacts = new ObservableCollection<SelectableContact>(SortContacts(list));
        }

        private static IEnumerable<SelectableContact> SortContacts(IEnumerable<SelectableContact> contacts)
        {
            return contacts.OrderBy(
                contact => contact.Contact?.Name ?? string.Empty,
                StringComparer.CurrentCultureIgnoreCase);
        }

        private void SortContacts()
        {
            var sortedContacts = SortContacts(Contacts).ToList();
            Contacts.Clear();
            foreach (var contact in sortedContacts)
            {
                Contacts.Add(contact);
            }
        }

        private void UnblockSelectedContacts()
        {
            var toUnblock = Contacts
                .Where(x => x.IsSelectedForDelete && x.Contact.IsBlocked)
                .ToList();
            if (toUnblock.Count == 0)
            {
                MessageBox.Show("Jelöljön ki legalább egy letiltott névjegyet a feloldáshoz!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Biztosan fel szeretné oldani a kiválasztott {toUnblock.Count} névjegy letiltását?", "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var item in toUnblock)
                {
                    item.Contact.IsBlocked = false;
                    item.IsSelectedForDelete = false;
                }

                SelectedContact = null;
                SaveContacts();
            }
        }

        private void BlockSelectedContacts()
        {
            var toBlock = Contacts.Where(x => x.IsSelectedForDelete).ToList();
            if (toBlock.Count == 0)
            {
                MessageBox.Show("Jelöljön ki legalább egy letiltandó névjegyet a jelölőnégyzettel!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Biztosan le szeretné tiltani a kiválasztott {toBlock.Count} névjegyet?", "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (var item in toBlock)
                {
                    item.Contact.IsBlocked = true;
                    item.IsSelectedForDelete = false;
                }

                SelectedContact = null;
                SaveContacts();
            }
        }

        private void SaveContacts()
        {
            var rawList = Contacts.Select(sc => sc.Contact).ToList();
            XmlFileManager.ContactManaging(rawList);
        }

        private bool HasDuplicateName(string name, Contact contactToIgnore = null)
        {
            return Contacts.Any(selectableContact =>
                selectableContact.Contact != contactToIgnore &&
                string.Equals(selectableContact.Contact.Name?.Trim(), name?.Trim(), StringComparison.CurrentCultureIgnoreCase));
        }

        private bool HasDuplicateEmail(string email, Contact contactToIgnore = null)
        {
            return Contacts.Any(selectableContact =>
                selectableContact.Contact != contactToIgnore &&
                string.Equals(selectableContact.Contact.Email?.Trim(), email?.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private bool HasDuplicatePhone(string phone, Contact contactToIgnore = null)
        {
            return Contacts.Any(selectableContact =>
                selectableContact.Contact != contactToIgnore &&
                string.Equals(selectableContact.Contact.Phone?.Trim(), phone?.Trim(), StringComparison.Ordinal));
        }

        private void OpenAddWindow()
        {
            var vm = new ContactEditViewModel(
                isDuplicateName: name => HasDuplicateName(name),
                isDuplicateEmail: email => HasDuplicateEmail(email),
                isDuplicatePhone: phone => HasDuplicatePhone(phone));

            var win = Application.Current.Windows.OfType<ContactEditWindow>().FirstOrDefault() ?? new ContactEditWindow();
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
                var newContact = new Contact();
                vm.UpdateContactModel(newContact);

                Contacts.Add(new SelectableContact { Contact = newContact });
                SortContacts();
                SaveContacts();
            }
        }

        private void OpenEditWindow()
        {
            if (SelectedContact?.Contact != null)
            {
                var vm = new ContactEditViewModel(
                    SelectedContact.Contact,
                    name => HasDuplicateName(name, SelectedContact.Contact),
                    email => HasDuplicateEmail(email, SelectedContact.Contact),
                    phone => HasDuplicatePhone(phone, SelectedContact.Contact));

                var win = Application.Current.Windows.OfType<ContactEditWindow>().FirstOrDefault() ?? new ContactEditWindow();
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
                    vm.UpdateContactModel(SelectedContact.Contact);
                    SortContacts();
                    SaveContacts();
                }
            }
            else
            {
                MessageBox.Show("Válasszon ki egy névjegyet a módosításhoz!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
































































































































































































































        private void DeleteSelectedContacts()
        {
            var toDelete = Contacts.Where(x => x.IsSelectedForDelete).ToList();
            if (toDelete.Count == 0)
            {
                MessageBox.Show("Jelöljön ki legalább egy törlendő névjegyet a jelölőnégyzettel!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Biztosan törölni szeretné a kiválasztott {toDelete.Count} névjegyet?", "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                SelectedContact = null;

                foreach (var item in toDelete)
                {
                    var imageToDelete = item.Contact.ImagePath;
                    item.Contact.ImagePath = string.Empty;
                    item.Contact.ImageSource = null;
                    Contacts.Remove(item);
                    XmlFileManager.DeleteContactImage(new Contact { ImagePath = imageToDelete });
                }
                SaveContacts();

                var groups = XmlFileManager.LoadGroups();
                bool groupsChanged = false;
                foreach (var group in groups)
                {
                    foreach (var deletedItem in toDelete)
                    {
                        var contactToRemove = group.Contacts.FirstOrDefault(c => c.Name == deletedItem.Contact.Name && c.Phone == deletedItem.Contact.Phone);
                        if (contactToRemove != null)
                        {
                            group.Contacts.Remove(contactToRemove);
                            groupsChanged = true;
                        }
                    }
                }
                if (groupsChanged)
                {
                    XmlFileManager.SaveGroups(groups);
                }

                SelectedContact = null;
            }
        }
    }
}