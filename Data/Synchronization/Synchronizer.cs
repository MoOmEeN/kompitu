using Kompitu.Data.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompitu.Data.Synchronization
{
    class Synchronizer
    {
        private static Synchronizer synchronizer;
        private static TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
        private TaskDao taskDao;

        async public static Task<Synchronizer> GetInstance()
        {
            if (synchronizer == null)
            {
                synchronizer = await GetObject();
            }
            return synchronizer;
        }
        
        async private static Task<Synchronizer> GetObject()
        {
            if (synchronizer == null)
            {
                synchronizer = new Synchronizer();
                synchronizer.taskDao = await taskDataSource.GetTaskDao();
            }
            return synchronizer;
        }

        async public Task<Boolean> Synchronize()
        {
            Task<ObservableCollection<TaskList>> remoteTaskListsTask = taskDao.GetTaskLists();
            List<TaskList> tempTaskLists = taskDataSource.TaskLists.ToList();
            List<TaskList> remoteTaskLists = (await remoteTaskListsTask).ToList();
            List<TaskList> toRemove = new List<TaskList>();
            List<string> processedIds = new List<string>();
            try
            {
                foreach (TaskList tempTaskList in tempTaskLists)
                {
                    if (tempTaskList.Id == null)
                    {
                        if (!tempTaskList.Deleted)
                        {
                            //ADD REMOTELY
                            await taskDao.InsertTaskList(tempTaskList);
                            if (tempTaskList.Tasks.Count > 0)
                            {
                                foreach (Model.Task task in tempTaskList.Tasks)
                                {
                                    await taskDao.InsertTask(task, tempTaskList);
                                }
                            }
                        }
                    }
                    else if (ListExists(remoteTaskLists, tempTaskList))
                    {
                        if (tempTaskList.Deleted)
                        {
                            //DELETE REMOTE
                            await taskDao.DeleteTaskList(tempTaskList);
                        }
                        else
                        {
                            // SYNC
                            await SyncTaskLists(tempTaskList, GetList(remoteTaskLists, tempTaskList));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        async private System.Threading.Tasks.Task SyncTaskLists(TaskList local, TaskList remote)
        {
            TaskList localTemp = TaskList.Clone(local);
            if (local.Title != remote.Title)
            {
                if (DateTime.Compare(local.Updated, remote.Updated) > 0)
                {
                    // UPDATE REMOTELY
                    await taskDao.UpdateTaskList(local);
                }
                
            }

            List<string> processedIds = new List<string>();
            foreach (Model.Task task in local.Tasks)
            {
                if (task.Id == null)
                {
                    if (!task.Deleted)
                    {
                        //INSERT TASK
                        await taskDao.InsertTask(task, localTemp);
                    }
                }
                else if (TaskExists(remote.Tasks.ToList(), task))
                {
                    if (task.Deleted)
                    {
                        // DELETE REMOTELY
                        await taskDao.DeleteTask(task, localTemp);
                    }
                    else
                    {
                        // SYNC
                        if (DateTime.Compare(task.Updated, GetTask(remote.Tasks.ToList(), task).Updated) > 0)
                        {
                            // UPDATE REMOTELY
                            await taskDao.UpdateTask(task, localTemp);
                        }
                    }
                }
            }
        }

        private bool ListExists(List<TaskList> list, TaskList toFind)
        {
            foreach (TaskList listItem in list)
            {
                if (listItem.Id == toFind.Id)
                {
                    return true;
                }
            }
            return false;
        }

        private TaskList GetList(List<TaskList> list, TaskList toFind)
        {
            foreach (TaskList listItem in list)
            {
                if (listItem.Id == toFind.Id)
                {
                    return listItem;
                }
            }
            return null;
        }

        private bool TaskExists(List<Model.Task> list, Model.Task toFind)
        {
            foreach (Model.Task taskItem in list)
            {
                if (taskItem.Id == toFind.Id)
                {
                    return true;
                }
            }
            return false;
        }

        private Model.Task GetTask(List<Model.Task> list, Model.Task toFind)
        {
            foreach (Model.Task taskItem in list)
            {
                if (taskItem.Id == toFind.Id)
                {
                    return taskItem;
                }
            }
            return null;
        }

        public bool IsInternetAvailable()
        {
            var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            if (profile != null)
            {
                return (profile.GetNetworkConnectivityLevel() != Windows.Networking.Connectivity.NetworkConnectivityLevel.None);
            }
            else
            {
                return false;
            }
        }

    }
}
