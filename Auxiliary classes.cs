
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace WPF
{
    public class Recognition : BaseViewModel
    {
        private string title;
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        private int count;
        public int Count
        {
            get
            {
                return count;
            }
            set
            {
                count = value;
                OnPropertyChanged(nameof(Count));
            }
        }

        public ObservableCollection<Photo> Photos { get; set; }


    }
    public class Photo
    {
        public bool IsSavedInDataBase { get; set; } = false;
        public string Path { get; set; }
        public byte[] Pixels { get; set; } = null;
        public BitmapImage Image { get; set; }
    }
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
