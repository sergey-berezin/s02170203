using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Gasket
{
    public static class DataBase
    {
        //public static LoadImagesFromDataBase()
        //{

        //}
    }
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

        public Recognition(string s, string path, Object i)
        {
            Title = s;
            Photos = new ObservableCollection<Photo>();

            Photos.Add(new Photo
            {
                Path = path,
                Image = i
            });
            count = 1;
        }

        public Recognition() { }

        public override string ToString()
        {
            string s = "";
            s += Title;
            s += "  ";
            s += Count.ToString() + "  ";
            foreach (var a in Photos)
            {
                s += a.Path + "\n";
            }
            return s;
        }

    }    
    public class Photo
    {
        public bool IsSavedInDataBase { get; set; } = false;
        public string Path { get; set; }
        public byte[] Pixels { get; set; } = null;
        public object Image { get; set; }
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
