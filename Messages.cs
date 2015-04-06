using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompitu
{
    class Messages
    {
        private static readonly Dictionary<MessageKey, string> dict = new Dictionary<MessageKey, string>()
        {
            {MessageKey.TODAYS_EMPTY,"No tasks for today"},
            {MessageKey.OVERDUE_EMPTY,"No overdue tasks"},
            {MessageKey.NOT_COMPLETED_EMPTY,"No uncompleted tasks"},
            {MessageKey.COMPLETED_EMPTY,"No completed tasks"},
            {MessageKey.LIST_EMPTY,"Empty list"},
            {MessageKey.SELECT_LIST,"Task list not selected"},
            {MessageKey.SELECT_TASK,"Select a task to show its details here"},
            {MessageKey.EDIT_WARN_TITLE,"You have modified task details."},
            {MessageKey.EDIT_WARN_CONTENT,"Please save or cancel your work"},
            {MessageKey.DELETE_LIST_WARN_HDR, "Selected list not empty"},
            {MessageKey.DELETE_LIST_WARN_CNTNT, "Are you sure you want to delete not empty task list?"},
            {MessageKey.LIST_TITLE_EXISTS_HDR, "Task list with given title already exists"},
            {MessageKey.LIST_TITLE_EXISTS_CNTNT, "Provide different title"},
            {MessageKey.NO_INTERNET, "No internet connection"},
            {MessageKey.NO_INTERNET_CNTNT, "Could not connect to the internet"},
            {MessageKey.SYNC_ERROR, "Synchronization error"},
            {MessageKey.SYNC_ERROR_CNTNT, "An error occured while synchornizing tasks with Google Tasks server"},
            {MessageKey.TASK_TITLE_EXISTS_HDR, "Task with given title already exists on selected list"},
            {MessageKey.TASK_TITLE_EXISTS_CNTNT, "Provide different title"},
        };

        public static string GetMsgValue(MessageKey key)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            else
            {
                return String.Empty;
            }
        }

    }

    enum MessageKey
    {
        TODAYS_EMPTY, OVERDUE_EMPTY, NOT_COMPLETED_EMPTY, COMPLETED_EMPTY, LIST_EMPTY, SELECT_LIST, SELECT_TASK,
        EDIT_WARN_TITLE, EDIT_WARN_CONTENT, DELETE_LIST_WARN_HDR, DELETE_LIST_WARN_CNTNT, LIST_TITLE_EXISTS_HDR, LIST_TITLE_EXISTS_CNTNT,
        NO_INTERNET, NO_INTERNET_CNTNT, SYNC_ERROR, SYNC_ERROR_CNTNT, TASK_TITLE_EXISTS_HDR, TASK_TITLE_EXISTS_CNTNT,
    }
}
