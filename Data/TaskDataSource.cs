using Kompitu.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;

namespace Kompitu.Data
{
    class TaskDataSource
    {
        private static string FileName = "TaskLists.xml";
        private TaskDao taskDao = null;
        private static XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<Kompitu.Data.Model.TaskList>));

        /* Basic list of TaskLists - contains every list and every task */
        private ObservableCollection<Kompitu.Data.Model.TaskList> _TaskLists = new ObservableCollection<Kompitu.Data.Model.TaskList>();
        public ObservableCollection<Kompitu.Data.Model.TaskList> TaskLists
        {
            get { return this._TaskLists; }
            set { this._TaskLists = value; }
        }

        /* List being used to bind to taskListsView - does not contain hidden/deleted tasks and taskLists that contain only those */
        private ObservableCollection<Kompitu.Data.Model.TaskList> _VisibleTaskLists = new ObservableCollection<Kompitu.Data.Model.TaskList>();
        public ObservableCollection<Kompitu.Data.Model.TaskList> VisibleTaskLists
        {
            get { return this._VisibleTaskLists; }
            set { this._VisibleTaskLists = value; }
        }

        private ObservableCollection<Kompitu.Data.Model.Task> _TodaysTasks = new ObservableCollection<Kompitu.Data.Model.Task>();
        public ObservableCollection<Kompitu.Data.Model.Task> TodaysTasks
        {
            get { return this._TodaysTasks; }
            set { this._TodaysTasks = value; }
        }

        private ObservableCollection<Kompitu.Data.Model.Task> _OverdueTasks = new ObservableCollection<Kompitu.Data.Model.Task>();
        public ObservableCollection<Kompitu.Data.Model.Task> OverdueTasks
        {
            get { return this._OverdueTasks; }
            set { this._OverdueTasks = value; }
        }

        private ObservableCollection<Kompitu.Data.Model.Task> _NotCompletedTasks = new ObservableCollection<Kompitu.Data.Model.Task>();
        public ObservableCollection<Kompitu.Data.Model.Task> NotCompletedTasks
        {
            get { return this._NotCompletedTasks; }
            set { this._NotCompletedTasks = value; }
        }

        private ObservableCollection<Kompitu.Data.Model.Task> _CompletedTasks = new ObservableCollection<Kompitu.Data.Model.Task>();
        public ObservableCollection<Kompitu.Data.Model.Task> CompletedTasks
        {
            get { return this._CompletedTasks; }
            set { this._CompletedTasks = value; }
        }

        /* Gets taskLists from XML file or web */
        async public Task GetTaskListsAsync(bool force)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            if (!force && await FileExists(folder, FileName))
            {
                Stream reader = await folder.OpenStreamForReadAsync(FileName);
                this.TaskLists = (ObservableCollection<Kompitu.Data.Model.TaskList>)serializer.Deserialize(reader);
            }
            else
            {
                TaskDao taskDao = await GetTaskDao();
                this.TaskLists = await taskDao.GetTaskLists();
            }
            RefreshLists();
        }

        public void RefreshLists()
        {
            //fill lists with visible tasks (not hidden and not deleted)
            VisibleTaskLists.Clear();
            RetrieveVisibleLists();

            //fill lists with todays tasks
            TodaysTasks.Clear();
            RetrieveTodaysTasks();

            //fill list with overdue tasks
            OverdueTasks.Clear();
            RetrieveOverdueTasks();

            //fill list with not completed tasks
            NotCompletedTasks.Clear();
            RetrieveNotCompletedTasks();

            //fill list with completed tasks
            CompletedTasks.Clear();
            RetrieveCompletedTasks();
        }

        /* Stores taskList in XML file */
        async public Task StoreTaskListsAsync()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<Kompitu.Data.Model.TaskList>));
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            Stream writer = await folder.OpenStreamForWriteAsync(FileName, CreationCollisionOption.ReplaceExisting);
            serializer.Serialize(writer, TaskLists);
        }

        /* Retrieves tasklists that are not deleted or hidden*/
        private void RetrieveVisibleLists()
        {
            foreach (Model.TaskList origTaskList in TaskLists.Where(l => !l.Deleted))
            {
                Model.TaskList visibleTaskList = Model.TaskList.Clone(origTaskList);
                visibleTaskList.Tasks = new ObservableCollection<Model.Task>(origTaskList.Tasks.Where(t => IsNotHiddenOrDeleted(t)));
                VisibleTaskLists.Add(visibleTaskList);
            }

        }

        private Model.TaskList GetTaskListObjectFromTaskLists(Model.TaskList list)
        {
            foreach (Model.TaskList taskList in TaskLists.Where(l => !l.Deleted))
            {
                if ((list.Id != null && list.Id == taskList.Id) || list.Title == taskList.Title)
                {
                    return taskList;
                }
            }
            return null;
        }

        /* Returns tasks thats due date is equal to today */
        public void RetrieveTodaysTasks()
        {
            foreach (Kompitu.Data.Model.TaskList taskList in _TaskLists)
            {
                foreach (Kompitu.Data.Model.Task task in taskList.Tasks.
                    Where(t => IsNotHiddenOrDeleted(t) && t.Status != "completed" 
                        && t.Due != DateTime.MinValue && DateTime.Compare(t.Due.Date, DateTime.Today) == 0))
                {
                        TodaysTasks.Add(task);
                }
            }
        }

        /* Returns tasks thats due date is earlier than today */
        public void RetrieveOverdueTasks()
        {
            foreach (Kompitu.Data.Model.TaskList taskList in _TaskLists)
            {
                foreach (Kompitu.Data.Model.Task task in taskList.Tasks.
                    Where(t => IsNotHiddenOrDeleted(t) && t.Status != "completed"
                        && t.Due != DateTime.MinValue && DateTime.Compare(t.Due, DateTime.Today) < 0))
                {
                        OverdueTasks.Add(task);
                }
            }
        }

        /* Returns all NOT completed tasks */
        public void RetrieveNotCompletedTasks()
        {
            foreach (Kompitu.Data.Model.TaskList taskList in _TaskLists)
            {
                foreach (Kompitu.Data.Model.Task task in taskList.Tasks.
                    Where(t => IsNotHiddenOrDeleted(t) && t.Status != "completed"))
                {
                        NotCompletedTasks.Add(task);
                }
            }
        }

        /* Returns all completed tasks */
        public void RetrieveCompletedTasks()
        {
            foreach (Kompitu.Data.Model.TaskList taskList in _TaskLists)
            {
                foreach (Kompitu.Data.Model.Task task in taskList.Tasks.
                    Where(t => IsNotHiddenOrDeleted(t) && t.Status == "completed"))
                {
                        CompletedTasks.Add(task);
                }
            }
        }

        private bool IsNotHiddenOrDeleted(Model.Task task)
        {
            return !(task.Hidden || task.Deleted);
        }

        async private Task<Boolean> FileExists(StorageFolder folder, string fileName)
        {
            try 
            {
                StorageFile f = await folder.GetFileAsync(fileName);
                return true;
            }
            catch (FileNotFoundException e)
            {
                return false;
            }
        }

        public bool ListTitleExists(string title)
        {
            return TaskLists.
                Count(l => l.Title == title) > 0;
        }

        async public Task<TaskDao> GetTaskDao()
        {
            if (taskDao == null)
            {
                taskDao = await TaskDao.GetObject();
            }
            return taskDao;
        }
    }
}
