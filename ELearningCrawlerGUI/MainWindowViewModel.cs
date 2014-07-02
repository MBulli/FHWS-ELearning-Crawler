using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ELearningCrawlerGUI
{
    internal class MainWindowViewModel : PropertyChangedBase
    {
        private Crawler crawler;
        private string _username;

        public ObservableCollection<Course> Courses { get; private set; }
        public ObservableCollection<CourseMaterial> Downloads { get; private set; }
        public string Username
        {
            get { return _username; }
            set { _username = value; NotifyPropertyChanged(); }
        }

        public MainWindowViewModel()
        {
            this.Courses = new ObservableCollection<Course>();
            this.Downloads = new ObservableCollection<CourseMaterial>();
            crawler = new Crawler();
        }

        public Task<bool> Login(string password)
        {
            return crawler.LoginToELearning(this.Username, password);
        }

        public Task LoadCourses()
        {
            this.Courses.Clear();
            return crawler.FetchCourses(this.Courses);
        }

        public Task DownloadMaterials()
        {
            var courses = this.Courses.Where(c => c.IsActivated);

            this.Downloads.Clear();
            return crawler.DownloadMaterials(courses);
        }
    }
}
