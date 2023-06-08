using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeApp.Controls
{
    public class PageSwitchModesViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<string> filteredProcesses;
        public ObservableCollection<string> FilteredProcesses
        {
            get { return filteredProcesses; }
            set
            {
                filteredProcesses = value;
                OnPropertyChanged(nameof(FilteredProcesses));
            }
        }

        // Implement the INotifyPropertyChanged interface
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PageSwitchModesViewModel()
        {
            FilteredProcesses = new();
        }
    }
}
