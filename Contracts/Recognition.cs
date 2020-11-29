
using System.Collections.ObjectModel;

namespace Contracts
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
}
