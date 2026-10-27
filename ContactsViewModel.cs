using System.Collections.ObjectModel;
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

        private void LoadContacts()
        {
            var loaded = XmlFileManager.ContactLoading();
            var list = new ObservableCollection<SelectableContact>();
            foreach (var c in loaded)
            {
                c.LoadImageSource();
                list.Add(new SelectableContact { Contact = c });
            }
            Contacts = list;
        }

        private void SaveContacts()
        {
            var rawList = Contacts.Select(sc => sc.Contact).ToList();
            XmlFileManager.ContactManaging(rawList);
        }

        private void OpenAddWindow()
        {
            var vm = new ContactEditViewModel();
            var win = new ContactEditWindow(vm);
            win.ShowDialog();

            if (vm.IsSaved)
            {
                var newContact = new Contact();
                vm.UpdateContactModel(newContact);

                Contacts.Add(new SelectableContact { Contact = newContact });
                SaveContacts();
            }
        }

        private void OpenEditWindow()
        {
            if (SelectedContact?.Contact != null)
            {
                var vm = new ContactEditViewModel(SelectedContact.Contact);
                var win = new ContactEditWindow(vm);
                win.ShowDialog();

                if (vm.IsSaved)
                {
                    vm.UpdateContactModel(SelectedContact.Contact);
                    SaveContacts();

                    var temp = SelectedContact;
                    Contacts.Remove(temp);
                    Contacts.Insert(0, temp);
                    SelectedContact = temp;

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