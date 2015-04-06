using Kompitu.Data.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Kompitu.Data
{
    class TaskDao
    {
        private const string baseUrl = "https://www.googleapis.com/tasks/v1";
        private static HttpClient httpClient;
        private string accessToken;
        private Authenticator authenticator;

        public static async Task<TaskDao> GetObject()
        {
            TaskDao taskDao = new TaskDao();
            Authenticator authenticator = new Authenticator();
            taskDao.accessToken = await authenticator.GetValidAccessToken();
            taskDao.authenticator = authenticator;
            return taskDao;
        }

        public async Task<ObservableCollection<TaskList>> GetTaskLists()
        {
            string address = "/users/@me/lists";
            ObservableCollection<TaskList> taskLists = new ObservableCollection<TaskList>();
            JsonObject json = await MakeGetRequest(address);
            JsonArray lists = json.GetNamedArray("items");
            for (uint i = 0; i<lists.Count; i++)
            {   
                TaskList taskList = new TaskList();
                JsonObject listObject = lists.GetObjectAt(i);
                taskList.Id = GetAsString(listObject, "id");
                taskList.Title = GetAsString(listObject, "title");
                taskList.SelfLink = GetAsString(listObject, "selfLink");
                taskList.Updated = GetAsDateTime(listObject, "updated");
                taskList.Deleted = false;
                ObservableCollection<Kompitu.Data.Model.Task> tasks = await GetTasks(taskList);
                taskList.Tasks = tasks;
                taskLists.Add(taskList);
            }
            return taskLists;
        }

        public async System.Threading.Tasks.Task InsertTaskList(TaskList list)
        {
            string address = "/users/@me/lists";
            JsonObject json = new JsonObject();
            json.Add("kind", JsonValue.CreateStringValue("tasks#taskList"));
            json.Add("title", JsonValue.CreateStringValue(list.Title));
            json.Add("updated", JsonValue.CreateStringValue(list.Updated.ToString("yyyy-MM-dd'T'HH:mm:ss.000Z")));
            JsonObject response = await MakePostRequest(address, json);
            list.Id = GetAsString(response, "id");
            list.Title = GetAsString(response, "title");
            list.SelfLink = GetAsString(response, "selfLink");
            list.Updated = GetAsDateTime(response, "updated");
        }

        public async System.Threading.Tasks.Task UpdateTaskList(TaskList list)
        {
            string address = "/users/@me/lists/" + list.Id;
            JsonObject json = new JsonObject();
            json.Add("id", JsonValue.CreateStringValue(list.Id));
            json.Add("title", JsonValue.CreateStringValue(list.Title));
            json.Add("updated", JsonValue.CreateStringValue(list.Updated.ToString("yyyy-MM-dd'T'HH:mm:ss.000Z")));
            JsonObject response = await MakePutRequest(address, json);
            list.Id = GetAsString(response, "id");
            list.Title = GetAsString(response, "title");
            list.SelfLink = GetAsString(response, "selfLink");
            list.Updated = GetAsDateTime(response, "updated");
        }

        public async System.Threading.Tasks.Task DeleteTaskList(TaskList list)
        {
            string address = "/users/@me/lists/" + list.Id;
            await MakeDeleteRequest(address);
        }

        public async Task<ObservableCollection<Kompitu.Data.Model.Task>> GetTasks(TaskList taskList)
        {
            string address = "/lists/" + taskList.Id + "/tasks";
            JsonObject json = await MakeGetRequest(address);
            ObservableCollection<Kompitu.Data.Model.Task> tasks = new ObservableCollection<Kompitu.Data.Model.Task>();
            if (json.ContainsKey("items"))
            {
                JsonArray lists = json.GetNamedArray("items");
                for (uint i = 0; i < lists.Count; i++)
                {
                    Kompitu.Data.Model.Task task = new Kompitu.Data.Model.Task();
                    JsonObject taskObject = lists.GetObjectAt(i);
                    JsonObjectToTask(taskObject, task);
                    task.ListTitle = taskList.Title;
                    tasks.Add(task);
                }

            }
            return tasks;
        }

        /* Inserts Task to the specified TaskList and updates its details */
        public async System.Threading.Tasks.Task InsertTask(Model.Task task, TaskList list)
        {
            string address = "/lists/" + list.Id + "/tasks";
            JsonObject json = TaskToJsonObject(task);
            JsonObject response = await MakePostRequest(address, json);
            JsonObjectToTask(response, task);
            task.ListTitle = list.Title;
        }

        public async System.Threading.Tasks.Task DeleteTask(Model.Task task, TaskList list)
        {
            string address = "/lists/" + list.Id + "/tasks/" + task.Id;
            await MakeDeleteRequest(address);
        }

        public async System.Threading.Tasks.Task UpdateTask(Model.Task task, TaskList list)
        {
            string address = "/lists/" + list.Id + "/tasks/" + task.Id;
            JsonObject json = TaskToJsonObject(task);
            JsonObject response = await MakePutRequest(address, json);
            JsonObjectToTask(response, task);
        }

        private void JsonObjectToTask(JsonObject json, Model.Task task)
        {
            task.Id = GetAsString(json, "id");
            task.Title = GetAsString(json, "title");
            task.Updated = GetAsDateTime(json, "updated");
            task.SelfLink = GetAsString(json, "selfLink");
            task.Parent = GetAsString(json, "parent");
            task.Position = GetAsString(json, "position");
            task.Notes = GetAsString(json, "notes");
            task.Status = GetAsString(json, "status");
            task.Due = GetAsDateTime(json, "due");
            task.Completed = GetAsDateTime(json, "completed");
            task.Deleted = GetAsBool(json, "deleted");
            task.Hidden = GetAsBool(json, "hidden");
            if (json.ContainsKey("links"))
            {
                JsonArray linksArray = json.GetNamedArray("links");
                Link[] links = new Link[linksArray.Count];
                for (uint j = 0; j < linksArray.Count; j++)
                {
                    Link link = new Link();
                    JsonObject linkObject = linksArray.GetObjectAt(j);
                    link.Type = GetAsString(linkObject, "type");
                    link.Description = GetAsString(linkObject, "description");
                    link.Url = GetAsString(linkObject, "link");
                    links[j] = link;
                }
                task.Links = links;
            }
        }

        private JsonObject TaskToJsonObject(Model.Task task)
        {
            JsonObject json = new JsonObject();
            json.Add("kind", JsonValue.CreateStringValue("tasks#task"));
            if (task.Id != null)
            {
                json.Add("id", JsonValue.CreateStringValue(task.Id));
            }
            json.Add("title", JsonValue.CreateStringValue(task.Title));
            json.Add("updated", JsonValue.CreateStringValue(task.Updated.ToString("yyyy-MM-dd'T'HH:mm:ss.000Z")));
            if (task.Due != DateTime.MinValue)
            {
                json.Add("due", JsonValue.CreateStringValue(task.Due.ToString("yyyy-MM-dd'T'HH:mm:ss.000Z")));
            }
            json.Add("notes", JsonValue.CreateStringValue(task.Notes));
            json.Add("status", JsonValue.CreateStringValue(task.Status));
            if (task.Status == "completed")
            {
                json.Add("completed", JsonValue.CreateStringValue(task.Completed.ToString("yyyy-MM-dd'T'HH:mm:ss.000Z")));
            }
            return json;
        }

        private async Task<JsonObject> MakeGetRequest(string address)
        {
            Uri addressUri = new Uri(baseUrl + address + "?access_token=" + accessToken);
            HttpResponseMessage response = await GetHttpClient().GetAsync(addressUri);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();    
                //Debug.WriteLine(content);
                return JsonObject.Parse(content);
            }
            else
            {
                throw new Exception("Exception during making get request: " + response.StatusCode);
            }
        }

        private async Task<JsonObject> MakePostRequest(string address, JsonObject content)
        {
            Uri addressUri = new Uri(baseUrl + address + "?access_token=" + accessToken);
            
            StringContent s = new StringContent(content.Stringify(), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await GetHttpClient().PostAsync(addressUri, s);
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                //Debug.WriteLine(responseContent);
                //Debug.WriteLine(response.RequestMessage.Content);
                return JsonObject.Parse(responseContent);
            }
            else
            {
                throw new Exception("Exception during making post request: " + response.StatusCode);
            }
        }

        private async Task<JsonObject> MakePutRequest(string address, JsonObject content)
        {
            Uri addressUri = new Uri(baseUrl + address + "?access_token=" + accessToken);

            StringContent s = new StringContent(content.Stringify(), Encoding.UTF8, "application/json");
            Debug.WriteLine(content.Stringify());
            HttpResponseMessage response = await GetHttpClient().PutAsync(addressUri, s);
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine(responseContent);
                Debug.WriteLine(response.RequestMessage.Content);
                return JsonObject.Parse(responseContent);
            }
            else
            {
                //string responseContent = await response.Content.ReadAsStringAsync();
               // Debug.WriteLine(responseContent);
               // Debug.WriteLine(response.RequestMessage.Content);
                throw new Exception("Exception during making put request: " + response.StatusCode);
            }
        }

        private async System.Threading.Tasks.Task MakeDeleteRequest(string address)
        {
            Uri addressUri = new Uri(baseUrl + address + "?access_token=" + accessToken);

            HttpResponseMessage response = await GetHttpClient().DeleteAsync(addressUri);
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
               // Debug.WriteLine(responseContent);
            }
            else
            {
                throw new Exception("Exception during making delete request: " + response.StatusCode);
            }
        }

        public string GetAsString(JsonObject json, string name)
        {
            if (json.ContainsKey(name))
            {
                return json.GetNamedString(name);
            }
            return String.Empty;
        }

        public DateTime GetAsDateTime(JsonObject json, string name)
        {
            if (json.ContainsKey(name))
            {
                return DateTime.Parse(json.GetNamedString(name));
            }
            return DateTime.MinValue;
        }

        public bool GetAsBool(JsonObject json, string name)
        {
            if (json.ContainsKey(name))
            {
                return json.GetNamedBoolean(name);
            }
            return false;
        }

        private static HttpClient GetHttpClient()
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient();
            }
            return httpClient;
        }

    }
}
