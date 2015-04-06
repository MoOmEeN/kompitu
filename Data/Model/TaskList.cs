using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Kompitu.Data.Model
{

    public class TaskList
    {
        public string Id;
        public string Title { get; set; }
        public string SelfLink;
        public DateTime Updated;
        public ObservableCollection<Task> Tasks = new ObservableCollection<Task>();
        public string TasksCount { get { return Tasks.Count(t => t.Status != "completed").ToString(); } }
        public ObservableCollection<Task> OrderedTasks
        {
            get
            {
                return new ObservableCollection<Kompitu.Data.Model.Task>(Tasks.OrderByDescending(t => t.Status));
            }
        }

        // helper field
        public bool Deleted = false;

        public static TaskList Clone(TaskList taskList)
        {
            TaskList newTaskList = new TaskList();
            newTaskList.Id = taskList.Id;
            newTaskList.SelfLink = taskList.SelfLink;
            newTaskList.Tasks = taskList.Tasks;
            newTaskList.Title = taskList.Title;
            newTaskList.Updated = taskList.Updated;
            newTaskList.Deleted = taskList.Deleted;
            return newTaskList;
        }

        public static TaskList GetByTitle(string title, Collection<TaskList> list)
        {
            foreach (TaskList taskList in list)
            {
                if (taskList.Title == title)
                {
                    return taskList;
                }
            }
            return null;
        }
    }
}
