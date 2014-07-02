using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ELearningCrawlerGUI
{
    class Course : PropertyChangedBase
    {
        private bool _isActivated;
        private string _name;
        private string _link;
        private CourseMaterialList _materials;

        public bool IsActivated
        { 
            get { return _isActivated; }
            set { _isActivated = value; NotifyPropertyChanged(); }
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyPropertyChanged(); }
        }
        public string Link
        {
            get { return _link; }
            set { _link = value; NotifyPropertyChanged(); }
        }
        public CourseMaterialList Materials
        {
            get { return _materials; }
            set { _materials = value; NotifyPropertyChanged(); }
        }

        public Course(string name, string link)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (string.IsNullOrEmpty(link))
                throw new ArgumentNullException("link");

            _name = name;
            _link = link;
            _isActivated = true;
        }

        public override string ToString()
        {
            return string.Format("Course: {0}", _name);
        }
    }

    class CourseMaterial : PropertyChangedBase
    {
        private Course _course;
        private string _name;
        private string _downloadLink;
        private long _fileSize;
        private float _downloadProgress;

        public Course Course
        {
            get { return _course; }
            set { _course = value; NotifyPropertyChanged(); }
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyPropertyChanged(); }
        }
        public string DownloadLink
        {
            get { return _downloadLink; }
            set { _downloadLink = value; }
        }

        [Obsolete]
        public long FileSize
        {
            get { return _fileSize; }
            set { _fileSize = value; NotifyPropertyChanged(); }
        }
        [Obsolete]
        public float DownloadProgress
        {
            get { return _downloadProgress; }
            set { _downloadProgress = value; NotifyPropertyChanged(); }
        }


        public CourseMaterial(Course course, string name, string link)
        {
            if (course == null)
                throw new ArgumentNullException("course");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (string.IsNullOrEmpty(link))
                throw new ArgumentNullException("link");

            _course = course;
            _name = name;
            _downloadLink = link;
        }
    }

    sealed class CourseList : List<Course>
    {
    }

    sealed class CourseMaterialList : List<CourseMaterial>
    {
    }

    class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
    }
}
