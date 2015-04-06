using Kompitu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompitu.Data.Model
{
    public class Task : BindableBase
    {
        public string Id { get; set; }
        private string _Title;
        public string Title { 
            get 
            {
                return this._Title;
            } 
            set 
            {
                SetProperty(ref this._Title, value);
                OnPropertyChanged("TitleNotEmpty");
            } 
        }
        private DateTime _Updated;
        public DateTime Updated {
            get
            {
                return _Updated;
            }
            set
            {
                SetProperty(ref this._Updated, value);
            }
        }
        public string SelfLink { get; set; }
        public string Parent { get; set; }
        public string Position { get; set; }
        public string Notes { get; set; }
        private string _Status;
        public string Status {
            get
            {
                return _Status;
            }
            set 
            {
                this._Status = value;
                OnPropertyChanged("Opacity");
            }
        }
        private DateTime _Due;
        public DateTime Due
        {
            get
            {
                return this._Due;
            }
            set
            {
                this._Due = value;
                OnPropertyChanged("DueDateString");
                OnPropertyChanged("DueDateColor");
            }
        }
        public DateTime Completed { get; set; }
        public Boolean Deleted { get; set; }
        public Boolean Hidden { get; set; }
        public Link[] Links { get; set; }
        public string ListTitle { get; set; }

        public string TitleNotEmpty {
            get {
                return Title.Length != 0 ? Title : "untitled"; 
            } 
        }

        public string DueDateColor { 
            get { 
                return 
                    (IsOverdue) ? "Red" : "Black"; 
            } 
        }

        public string Opacity
        {
            get
            {
                return Status == "completed" ? 0.3.ToString() : 1.ToString();
            }
        }

        public string DueDateString
        {
            get
            {
                DateTime now = DateTime.Now;
                if (IsOverdue)
                {
                    if ((now - Due).Days > 0)
                    {
                        return (now - Due).Days + " days ago";
                    }
                    else if ((now - Due).Hours > 0)
                    {
                        return (now - Due).Hours + " hours ago";
                    }
                    else if ((now - Due).Minutes > 0)
                    {
                        return (now - Due).Minutes + " minutes ago";
                    }
                    else
                    {
                        return (now - Due).Seconds + " seconds ago";
                    }
                }
                else
                {
                    if (Due != DateTime.MinValue)
                    {
                        if (now.Year == Due.Year)
                        {
                            if (now.DayOfYear == Due.DayOfYear)
                            {
                                return Due.ToString("t");
                            }
                            else
                            {
                                return Due.ToString("m");
                            }
                        }
                        else
                        {
                            return Due.ToString("d");
                        }
                    }
                    else
                    {
                        return String.Empty;
                    }
                    
                }                
            }
        }

        public bool IsOverdue {
            get {
                return 
                    (Due != DateTime.MinValue && DateTime.Compare(Due, DateTime.Now) < 0);
            } 
        }
    }

}
