using Kompitu.Data;
using Kompitu.Data.Synchronization;
using Syncfusion.UI.Xaml.Controls.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Kompitu
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MainPage : Kompitu.Common.LayoutAwarePage
    {
        private Boolean editMode = false;
        private Kompitu.Data.Model.Task selectedTask;
        private Kompitu.Data.Model.TaskList selectedTaskList;
        
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
            if (taskDataSource != null)
            {
                this.taskListsView.ItemsSource = taskDataSource.VisibleTaskLists;
               // Binding myBinding = new Binding();
                //myBinding.Source = taskDataSource.VisibleTaskLists;
                //myBinding.Path = new PropertyPath("SomeString")
                //myBinding.Mode = BindingMode.TwoWay;
                //myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
               // BindingOperations.SetBinding(taskListsView, ListView.ItemsSourceProperty, myBinding);
                RefreshCounters(taskDataSource);
                showEmptyTextBlock(Messages.GetMsgValue(MessageKey.SELECT_LIST));


                showEmptyTaskTextBlock(Messages.GetMsgValue(MessageKey.SELECT_TASK));

            }
            
        }

        private void RefreshCounters(TaskDataSource taskDataSource)
        {
            this.todaysCount.Text = taskDataSource.TodaysTasks.Count.ToString();
            this.overdueCount.Text = taskDataSource.OverdueTasks.Count.ToString();
            this.notCompletedCount.Text = taskDataSource.NotCompletedTasks.Count.ToString();
            this.completedCount.Text = taskDataSource.CompletedTasks.Count.ToString();
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        async protected override void SaveState(Dictionary<String, Object> pageState)
        {
            TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
            await taskDataSource.StoreTaskListsAsync();
        }

        /* Enables task edit mdoe, where user can modify selected tasks properties.
         */ 
        private void EnableTaskEditMode()
        {
            this.markCompleted.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            this.add.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            this.delete.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            this.save.Visibility = Windows.UI.Xaml.Visibility.Visible;
            this.cancel.Visibility = Windows.UI.Xaml.Visibility.Visible;
            editMode = true;
        }

        /* Disables task edit mode, where user can modify selected tasks properties.
        */ 
        private void DisableTaskEditMode(Kompitu.Data.Model.Task task)
        {
            this.add.Visibility = Windows.UI.Xaml.Visibility.Visible;
            if (task == null || task.Status == "completed")
            {
                //completed task - blocking possiblity to mark completed and delete
                this.markCompleted.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                this.delete.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                this.markCompleted.Visibility = Windows.UI.Xaml.Visibility.Visible;
                this.delete.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            this.save.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            this.cancel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            editMode = false;
        }

        private void SaveTask()
        {
            if (selectedTask == null)
            {
                SaveNewTask();
                
            }
            else
            {
                SaveEditedTask();
                DisableTaskEditMode(selectedTask);
            }
        }


        private void SaveNewTask()
        {
            string selectedList = null;
            // show select list popup
            Popup myPopup = new Popup();
            myPopup.HorizontalAlignment = HorizontalAlignment.Center;
            myPopup.VerticalAlignment = VerticalAlignment.Center;
            myPopup.IsLightDismissEnabled = false;

            Border b = new Border();
            b.BorderBrush = new SolidColorBrush(Colors.Gray);
            b.BorderThickness = new Thickness(2);
            b.Width = 400;

            StackPanel s = new StackPanel();
            b.Child = s;
            s.Orientation = Orientation.Vertical;
            s.Width = 400;
            s.Background = new SolidColorBrush(Colors.White);

            TextBlock text = new TextBlock();
            text.Text = "Select a list for the task:";
            text.Margin = new Thickness(10, 5, 20, 0);
            text.Foreground = new SolidColorBrush(Colors.SteelBlue);
            text.FontSize = 16;
            s.Children.Add(text);

            ComboBox combo = new ComboBox();
            TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
            combo.ItemsSource = taskDataSource.VisibleTaskLists;
            combo.DisplayMemberPath = "Title";

            s.Children.Add(combo);
            combo.Margin = new Thickness(10, 5, 10, 5);
            combo.SelectedIndex = combo.Items.Count - 1;

            Button ok = new Button();
            ok.HorizontalAlignment = HorizontalAlignment.Right;
            ok.Margin = new Thickness(10, 0, 10, 5);
            ok.Content = "OK";
            ok.Click += new RoutedEventHandler(async delegate(object sender1, RoutedEventArgs ev)
            {
                selectedList
                    = ((Data.Model.TaskList)combo.SelectedValue).Title;
                Data.Model.TaskList selectedListObject = Data.Model.TaskList.GetByTitle(selectedList, taskDataSource.VisibleTaskLists);
                foreach (Data.Model.Task selectedListTask in selectedListObject.Tasks)
                {
                    if (selectedListTask.Title == taskTitleTextBox.Text)
                    {
                        var messageDialog =
                            new Windows.UI.Popups.MessageDialog(
                                Messages.GetMsgValue(MessageKey.TASK_TITLE_EXISTS_CNTNT), Messages.GetMsgValue(MessageKey.TASK_TITLE_EXISTS_HDR));

                        messageDialog.Commands.Add(new UICommand("OK"));

                        messageDialog.DefaultCommandIndex = 0;
                        await messageDialog.ShowAsync();
                        myPopup.IsOpen = false;
                        return;
                    }
                }
                myPopup.IsOpen = false;
                Data.Model.Task task = new Data.Model.Task();
                task.Updated = DateTime.Now;
                this.taskUpdatedDateLabel.Text = "Updated";
                this.taskUpdatedDateTextBox.Text = task.Updated.ToString();
                task.Title = taskTitleTextBox.Text;
                task.ListTitle = selectedList;
                task.Status = "needsAction";
                if (taskDueDateTextBox.Text == "None")
                {
                    task.Due = DateTime.MinValue;
                }
                else
                {
                    task.Due = DateTime.Parse(taskDueDateTextBox.Text);
                }
                if (task.Due == DateTime.MinValue || DateTime.Compare(task.Due, DateTime.Now) >= 0)
                {
                    // not overdue
                    taskDueDateLabel.Foreground = new SolidColorBrush(Colors.Gray);
                    taskDueDateTextBox.Foreground = new SolidColorBrush(Colors.Gray);
                }
                else if (task.Due != DateTime.MinValue && DateTime.Compare(task.Due, DateTime.Now) < 0)
                {
                    taskDueDateLabel.Foreground = new SolidColorBrush(Colors.Red);
                    taskDueDateTextBox.Foreground = new SolidColorBrush(Colors.Red);
                }
                task.Notes = taskNotesTextBox.Text;
                task.ListTitle = selectedList;
                bool added = false;
                foreach (Data.Model.TaskList list in taskDataSource.TaskLists.Where(l => l.Title == selectedList))
                {
                    list.Tasks.Add(task);
                    added = true;
                }
                if (added)
                {
                    taskListTextBox.Text = selectedList;
                    selectedTask = task;
                    taskDataSource.RefreshLists();
                    RefreshCounters(taskDataSource);
                    DisableTaskEditMode(selectedTask);
                    if (selectedTaskList != null)
                    {
                        if (selectedList == selectedTaskList.Title)
                        {
                            ((ObservableCollection<Data.Model.Task>)tasksView.ItemsSource).Add(task);
                        }
                    }
                }
                
            });
            s.Children.Add(ok);

            myPopup.Child = b;

            myPopup.IsOpen = true;
            myCanvas1.Children.Add(myPopup);
        }

        private void SaveEditedTask()
        {
            bool shouldReload = false;
               
            if (selectedTask.Title != taskTitleTextBox.Text)
            {
                shouldReload = true;
            }
            selectedTask.Updated = DateTime.Now;
            this.taskUpdatedDateTextBox.Text = selectedTask.Updated.ToString();
            selectedTask.Title = taskTitleTextBox.Text;
            selectedTask.Notes = taskNotesTextBox.Text;
            DateTime newDue;
            if (taskDueDateTextBox.Text == "None")
            {
                newDue = DateTime.MinValue;
            }
            else
            {
                newDue = DateTime.Parse(taskDueDateTextBox.Text);
            }
            if (selectedTask.Due != newDue)
            {
                shouldReload = true;
                if (newDue == DateTime.MinValue || (DateTime.Compare(selectedTask.Due, DateTime.Now) < 0 && DateTime.Compare(newDue, DateTime.Now) >= 0))
                {
                    // overdue deleted, changing color
                    taskDueDateLabel.Foreground = new SolidColorBrush(Colors.Gray);
                    taskDueDateTextBox.Foreground = new SolidColorBrush(Colors.Gray);
                }
                else if ((DateTime.Compare(selectedTask.Due, DateTime.Now) >= 0 || selectedTask.Due == DateTime.MinValue) && DateTime.Compare(newDue, DateTime.Now) < 0)
                {
                    // overdue set, changing to red
                    taskDueDateLabel.Foreground = new SolidColorBrush(Colors.Red);
                    taskDueDateTextBox.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
            selectedTask.Due = newDue;   
            
            if (shouldReload)
            {
                TaskDataSource dataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
                dataSource.RefreshLists();
                RefreshCounters(dataSource);
            }
        }

        /* Shows textblock above tasksListView.
         * The textblock is used to show messages when itemSource for tasksListView is empty.
         */
        private void showEmptyTextBlock(String msg)
        {
           emptyTextBlock.Opacity = 0;
            emptyTextBlock.Height = 50;
            emptyTextBlock.Margin = new Thickness(10);
            emptyTextBlock.Text = msg;
            selectedTaskList = null;
           ShowEmptyTaskList.Begin();
        }

        /* Hides textBlock above taskListView.
        */
        private void HideEmptyTextBlock()
        {
            emptyTextBlock.Height = 0;
            emptyTextBlock.Margin = new Thickness(0);
        }

        /* Shows textblock instead of task details fields.
         * The textblock is used to show messages when task is not selected.
         */
        private void showEmptyTaskTextBlock(String msg)
        {
            emptyTaskTextBlock.Height = 50;
            emptyTaskTextBlock.Margin = new Thickness(10);
            emptyTaskTextBlock.Text = msg;

            // hide task details view fields
            taskListTextBox.Visibility = Visibility.Collapsed;
            taskListTextBoxBorder.Visibility = Visibility.Collapsed;
            taskTitleTextBox.Visibility = Visibility.Collapsed;
            taskUpdatedDateLabel.Visibility = Visibility.Collapsed;
            taskUpdatedDateTextBox.Visibility = Visibility.Collapsed;
            taskDueDateLabel.Visibility = Visibility.Collapsed;
            taskDueDateTextBox.Visibility = Visibility.Collapsed;
            editDueDate.Visibility = Visibility.Collapsed;
            taskNotesTextBox.Visibility = Visibility.Collapsed;
          
        }

        /* Hides textBlock above taskListView.
        */
        private void HideEmptyTaskTextBlock(bool completed)
        {
            emptyTaskTextBlock.Height = 0;
            emptyTaskTextBlock.Margin = new Thickness(0);

            // show task details view fields 
            taskListTextBox.Visibility = Visibility.Visible;
            taskListTextBoxBorder.Visibility = Visibility.Visible;
            taskTitleTextBox.Visibility = Visibility.Visible;
            taskUpdatedDateLabel.Visibility = Visibility.Visible;
            taskUpdatedDateTextBox.Visibility = Visibility.Visible;
            taskNotesTextBox.Visibility = Visibility.Visible;
            if (!completed)
            {
                taskDueDateLabel.Visibility = Visibility.Visible;
                taskDueDateTextBox.Visibility = Visibility.Visible;
                editDueDate.Visibility = Visibility.Visible;
            }

        }

        /* Hides textBlock above taskListView only if not hidden.
        */
        private void HideEmptyTextBlockIfNeeded()
        {
            if (emptyTextBlock.Height > 0)
            {
                HideEmptyTextBlock();
            }
        }

        /* Hides textBlock if not hidden.
        */
        private void HideEmptyTaskTextBlockIfNeeded(bool completed)
        {
            if (emptyTaskTextBlock.Height > 0)
            {
                HideEmptyTaskTextBlock(completed);
            }
        }

        /* Events handling */

        /* Handler for click on 'For today' button.
         */ 
        private void todaysButton_Click(object sender, RoutedEventArgs e)
        {
            tasksView.Opacity = 0;
            HideEmptyTextBlockIfNeeded();
            TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
            // call datasource to produce list of tasks for today
            tasksView.ItemsSource = taskDataSource.TodaysTasks;
            if (((ObservableCollection<Kompitu.Data.Model.Task>) tasksView.ItemsSource).Count == 0)
            {
                showEmptyTextBlock(Messages.GetMsgValue(MessageKey.TODAYS_EMPTY));
            }
            selectedTaskList = null;
            ShowTaskList.Begin();
        }

        /* Handler for click on 'Overdue' button.
        */ 
        private void overdueButton_Click(object sender, RoutedEventArgs e)
        {
            tasksView.Opacity = 0;
            HideEmptyTextBlockIfNeeded();
            TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
            tasksView.ItemsSource = taskDataSource.OverdueTasks;
            if (((ObservableCollection<Kompitu.Data.Model.Task>)tasksView.ItemsSource).Count == 0)
            {
                showEmptyTextBlock(Messages.GetMsgValue(MessageKey.OVERDUE_EMPTY));
            }
            selectedTaskList = null;
            ShowTaskList.Begin();
        }

        /* Handler for click on 'Not completed' button.
        */ 
        private void notCompletedButton_Click(object sender, RoutedEventArgs e)
        {
            tasksView.Opacity = 0;
            HideEmptyTextBlockIfNeeded();
            TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
            tasksView.ItemsSource = taskDataSource.NotCompletedTasks;
            if (((ObservableCollection<Kompitu.Data.Model.Task>)tasksView.ItemsSource).Count == 0)
            {
                showEmptyTextBlock(Messages.GetMsgValue(MessageKey.NOT_COMPLETED_EMPTY));
            }
            selectedTaskList = null;
            ShowTaskList.Begin();
        }

        /* Handler for click on 'Completed' button.
        */ 
        private void completedButton_Click(object sender, RoutedEventArgs e)
        {
            tasksView.Opacity = 0;
            HideEmptyTextBlockIfNeeded();
            TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
            tasksView.ItemsSource = taskDataSource.CompletedTasks;
            if (((ObservableCollection<Kompitu.Data.Model.Task>)tasksView.ItemsSource).Count == 0)
            {
                showEmptyTextBlock(Messages.GetMsgValue(MessageKey.COMPLETED_EMPTY));
            }

            selectedTaskList = null;
            ShowTaskList.Begin();
        }

        /* Handler for click event on item of taskListsView.
         */ 
        private void taskListsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Kompitu.Data.Model.TaskList taskList = (Kompitu.Data.Model.TaskList)e.ClickedItem;
            /*if (editMode)
            {
                await PopupEditWarningDialog();
                return;
                
                 * komentuje bo nie pamietam po co to bylo
                if (taskList.Tasks.Count != 0)
                {
                    DisableTaskEditMode(taskList.Tasks[0]);
                }
                else
                {
                    DisableTaskEditMode(null);
                }
            } */
            //HideTaskList.Begin();
            tasksView.Opacity = 0;
            HideEmptyTextBlockIfNeeded();
            TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];

            //this.tasksView.ItemsSource = taskList.Tasks;
           
            this.tasksView.ItemsSource = taskList.OrderedTasks;
            if (((ObservableCollection<Kompitu.Data.Model.Task>)tasksView.ItemsSource).Count == 0)
            {
                showEmptyTextBlock(Messages.GetMsgValue(MessageKey.LIST_EMPTY));
            }

           // this.selectedTask = null;
            ShowTaskList.Begin();
            this.selectedTaskList = taskList;
        }


        /* Handler for click event on item of tasksView.
         * Shows up dialog if TaskEdit mode is ON.
         */ 
        async private void tasksView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Kompitu.Data.Model.Task t = (Kompitu.Data.Model.Task)e.ClickedItem;
            TaskDetails.Opacity = 0;

            await showTask(t);
            ShowTask.Begin();
        }

        async private Task showTask(Data.Model.Task t)
        {
            if (editMode)
            {
                await PopupEditWarningDialog();
                return;
            }

            this.taskListTextBox.Text = t.ListTitle;
            this.taskTitleTextBox.Text = t.Title;
            this.taskNotesTextBox.Text = t.Notes;

            if (t.Status == "completed")
            {
                hideTaskEditButtons();
                this.delete.Visibility = Windows.UI.Xaml.Visibility.Visible;
                this.taskUpdatedDateLabel.Text = "Completed:";
                this.taskUpdatedDateTextBox.Text = t.Completed.ToString();

                taskDueDateLabel.Visibility = Visibility.Collapsed;
                taskDueDateTextBox.Visibility = Visibility.Collapsed;
                editDueDate.Visibility = Visibility.Collapsed;
            }
            else
            {
                showTaskEditButtons();
                this.taskUpdatedDateLabel.Text = "Updated:";
                this.taskUpdatedDateTextBox.Text = t.Updated.ToString();

                taskDueDateLabel.Visibility = Visibility.Visible;
                taskDueDateTextBox.Visibility = Visibility.Visible;
                editDueDate.Visibility = Visibility.Visible;
                this.taskDueDateLabel.Text = "Due:";
                taskDueDateLabel.Foreground = new SolidColorBrush(Colors.Gray);
                taskDueDateTextBox.Foreground = new SolidColorBrush(Colors.Gray);
                if (t.Due != DateTime.MinValue)
                {
                    this.taskDueDateTextBox.Text = t.Due.Date.ToString();
                    if (t.IsOverdue)
                    {
                        taskDueDateLabel.Foreground = new SolidColorBrush(Colors.Red);
                        taskDueDateTextBox.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }
                else
                {
                    this.taskDueDateTextBox.Text = "None";
                }
            }

            HideEmptyTaskTextBlockIfNeeded(t.Status == "completed");
            this.selectedTask = t;
        }

        private void showTaskEditButtons()
        {
            this.markCompleted.Visibility = Windows.UI.Xaml.Visibility.Visible;
            this.delete.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void hideTaskEditButtons()
        {
            this.markCompleted.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            this.delete.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        /* Shows popup dialog with question what should be done with not saved task details changes
         */ 
        async private Task PopupEditWarningDialog()
        {
            var messageDialog =
                new Windows.UI.Popups.MessageDialog(
                    Messages.GetMsgValue(MessageKey.EDIT_WARN_CONTENT), Messages.GetMsgValue(MessageKey.EDIT_WARN_TITLE));

            messageDialog.Commands.Add(new UICommand("OK"));

            messageDialog.DefaultCommandIndex = 0;
            await messageDialog.ShowAsync();
        }

        /* Handler for key down on task notes textarea.
         * Enables edit mode.
         */ 
        private void taskNotesTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            EnableTaskEditMode();
        }

        /* Hanlder for key down on task title textblock.
         * Enables edit mode.
         */
        private void takTitleTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            EnableTaskEditMode();
        }

        /* Handler for click on save button while in task edit mode
        */
        private void save_Click(object sender, RoutedEventArgs e)
        {
            SaveTask();
        }

        private void editDueDate_Click(object sender, RoutedEventArgs e)
        {
            Popup myPopup = new Popup();
            myPopup.HorizontalAlignment = HorizontalAlignment.Center;
            myPopup.VerticalAlignment = VerticalAlignment.Center;
            myPopup.IsLightDismissEnabled = true;

            Grid g = new Grid();

            ColumnDefinition cd1 = new ColumnDefinition();
            g.ColumnDefinitions.Add(cd1);
            //cd1.Width = new GridLength(320);
            //ColumnDefinition cd2 = new ColumnDefinition();
            //g.ColumnDefinitions.Add(cd2);
            //cd2.Width = new GridLength(320);

            RowDefinition rd1 = new RowDefinition();
            rd1.Height = new GridLength(205);
            g.RowDefinitions.Add(rd1);
            RowDefinition rd2 = new RowDefinition();
            g.RowDefinitions.Add(rd2);
            
            DateSelector dateSelector = new DateSelector();
            dateSelector.ShowCancelButton = false;
            dateSelector.ShowDoneButton = false;
            dateSelector.Height = 200;
            dateSelector.Margin = new Thickness(2);
            dateSelector.AccentBrush = new SolidColorBrush(Colors.SteelBlue);
            if (taskDueDateTextBox.Text != "None")
            {
                dateSelector.SelectedDateTime = DateTime.Parse(taskDueDateTextBox.Text);
            }
            Grid.SetColumn(dateSelector, 0);
            Grid.SetRow(dateSelector, 0);
            g.Children.Add(dateSelector);

            TimeSelector timeSelector = new TimeSelector();
            timeSelector.ShowDoneButton = false;
            timeSelector.ShowCancelButton = false;
            timeSelector.Height = 200;
            timeSelector.Margin = new Thickness(2);
            timeSelector.AccentBrush = new SolidColorBrush(Colors.SteelBlue);
            if (taskDueDateTextBox.Text != "None")
            {
                timeSelector.SelectedTime = DateTime.Parse(taskDueDateTextBox.Text);
            }
            Grid.SetColumn(timeSelector, 1);
            Grid.SetRow(timeSelector, 0);
            //g.Children.Add(timeSelector);

            Border b = new Border();
            b.BorderBrush = new SolidColorBrush(Colors.LightGray);
            b.BorderThickness = new Thickness(2);
            b.Width = 314;
            //b.Width = 635;

            StackPanel s = new StackPanel();
            b.Child = s;
            s.Orientation = Orientation.Horizontal;
            //s.Width = 630;
            //s.Width = 630;
            s.Background = new SolidColorBrush(Colors.White);

            StackPanel s1 = new StackPanel();
            s1.Orientation = Orientation.Horizontal;
            //s1.Width = 315;
            s1.Width = 157;
            s1.Margin = new Thickness(0, 10, 0, 10);
            s1.FlowDirection = FlowDirection.RightToLeft;
            s.Children.Add(s1);

            StackPanel s2 = new StackPanel();
            s2.Orientation = Orientation.Horizontal;
            s2.Width = 157;
            //s2.Width = 315;
            s2.Margin = new Thickness(0, 10, 0, 10);
            s2.FlowDirection = FlowDirection.LeftToRight;
            s.Children.Add(s2); ;

            Button apply = new Button();
            apply.Content = "Apply";
            apply.HorizontalContentAlignment = HorizontalAlignment.Center;
            s1.Children.Add(apply);
            apply.Click += new RoutedEventHandler(delegate(object sender1, RoutedEventArgs ev)
            {
                DateTime d = (DateTime) dateSelector.SelectedDateTime;
                //DateTime d = DateTime.Parse(((DateTime)dateSelector.SelectedDateTime).ToString("yyyy-MM-dd") + "T" + ((DateTime) timeSelector.SelectedTime).ToString("HH:mm:ss"));
                System.Diagnostics.Debug.WriteLine(d);
                taskDueDateTextBox.Text = d.ToString();
                EnableTaskEditMode();
                myPopup.IsOpen = false;
            });

            Button clear = new Button();
            clear.Content = "Clear";
            clear.HorizontalContentAlignment = HorizontalAlignment.Center;
            s2.Children.Add(clear);
            clear.Click += new RoutedEventHandler(delegate(object sender1, RoutedEventArgs ev)
            {
                taskDueDateTextBox.Text = "None";
                EnableTaskEditMode();
                myPopup.IsOpen = false;
            });


            Grid.SetColumnSpan(b, 2);
            Grid.SetColumn(b, 0);
            Grid.SetRow(b, 1);
            g.Children.Add(b);

            myPopup.Child = g;

            myPopup.IsOpen = true;
            myCanvas.Children.Add(myPopup);
        }

        private void taskListsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // show edit/remove buttons
            if (taskListsView.SelectedItems.Count > 0)
            {
                renameList.Visibility = Visibility.Visible;
                removeList.Visibility = Visibility.Visible;
            }
            // hide edit/remove buttons
            else 
            {
                renameList.Visibility = Visibility.Collapsed;
                removeList.Visibility = Visibility.Collapsed;
            }
        }

        async private void removeList_Click(object sender, RoutedEventArgs e)
        {
            Data.Model.TaskList selectedList = (Data.Model.TaskList) taskListsView.SelectedItem;
            if (selectedList.Tasks.Count > 0)
            {
                var messageDialog =
                new Windows.UI.Popups.MessageDialog(
                    Messages.GetMsgValue(MessageKey.DELETE_LIST_WARN_CNTNT), Messages.GetMsgValue(MessageKey.DELETE_LIST_WARN_HDR));

                bool confirmed = false;
                messageDialog.Commands.Add(new UICommand("Yes", (command) =>
                {
                    confirmed = true;
                }));

                messageDialog.Commands.Add(new UICommand("No"));

                messageDialog.DefaultCommandIndex = 1;
                await messageDialog.ShowAsync();
                if (!confirmed)
                {
                    return;
                }
            }

            TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
            foreach (Data.Model.TaskList list in taskDataSource.TaskLists)
            {
                if (list.Id == selectedList.Id)
                {
                    list.Deleted = true;
                }
            }
            taskDataSource.RefreshLists();
            RefreshCounters(taskDataSource);
        }

        private void renameList_Click(object sender, RoutedEventArgs e)
        {
            Data.Model.TaskList selectedList = (Data.Model.TaskList)taskListsView.SelectedItem;
            Popup myPopup = new Popup();
            myPopup.HorizontalAlignment = HorizontalAlignment.Center;
            myPopup.VerticalAlignment = VerticalAlignment.Center;
            myPopup.IsLightDismissEnabled = true;

            Border b = new Border();
            b.BorderBrush = new SolidColorBrush(Colors.Gray);
            b.BorderThickness = new Thickness(2);
            b.Width = 400;

            StackPanel s = new StackPanel();
            b.Child = s;
            s.Orientation = Orientation.Vertical;
            s.Width = 400;
            s.Background = new SolidColorBrush(Colors.White);

            TextBlock text = new TextBlock();
            text.Text = "New name for the list:";
            text.Margin = new Thickness(10, 5, 20, 0);
            text.Foreground = new SolidColorBrush(Colors.SteelBlue);
            text.FontSize = 16;
            s.Children.Add(text);

            TextBox input = new TextBox();
            input.Text = selectedList.Title;
            s.Children.Add(input);
            input.Margin = new Thickness(10, 5, 10, 5);

            Button save = new Button();
            save.HorizontalAlignment = HorizontalAlignment.Right;
            save.Margin = new Thickness(10, 0, 10, 5);
            save.Content = "Save";
            save.Click += new RoutedEventHandler(async delegate(object sender1, RoutedEventArgs ev)
            {
                TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
                if (!taskDataSource.ListTitleExists(input.Text))
                {
                    foreach (Data.Model.TaskList list in taskDataSource.TaskLists)
                    {
                        if (list.Id == selectedList.Id)
                        {
                            list.Title = input.Text;
                            list.Updated = DateTime.Now;
                            foreach (Data.Model.Task task in list.Tasks)
                            {
                                task.ListTitle = input.Text;

                            }
                        }
                    }

                    taskDataSource.RefreshLists();
                    myPopup.IsOpen = false;
                }
                else
                {
                    var messageDialog =
                        new Windows.UI.Popups.MessageDialog(
                            Messages.GetMsgValue(MessageKey.LIST_TITLE_EXISTS_CNTNT), Messages.GetMsgValue(MessageKey.LIST_TITLE_EXISTS_HDR));

                    messageDialog.Commands.Add(new UICommand("OK"));

                    messageDialog.DefaultCommandIndex = 1;
                    await messageDialog.ShowAsync();
                }
               
            });
            s.Children.Add(save);

            myPopup.Child = b;

            myPopup.IsOpen = true;
            myCanvas1.Children.Add(myPopup);
        }

        async private void markCompleted_Click(object sender, RoutedEventArgs e)
        {
            markTaskCompleted(selectedTask);
            TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
        
            taskDataSource.RefreshLists();
            RefreshCounters(taskDataSource);
            await showTask(selectedTask);
        }

        private void markTaskCompleted(Data.Model.Task task)
        {
            task.Status = "completed";
            task.Completed = DateTime.Now;
            task.Updated = DateTime.Now;
        }  

        private void delete_Click(object sender, RoutedEventArgs e)
        {
            TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];

            Data.Model.Task toDelete = null;
            foreach (Data.Model.TaskList list in taskDataSource.TaskLists)
            {
                if (list.Title == selectedTask.ListTitle)
                {
                    foreach (Data.Model.Task task in list.Tasks)
                    {
                        if (task.Id != null && task.Id == selectedTask.Id)
                        {
                            task.Deleted = true;
                            toDelete = task;
                            break;
                        }
                    }
                    if (toDelete == null)
                    {
                        // not found, probably task not synchronized so does not have id, trying to find by title
                        foreach (Data.Model.Task task in list.Tasks)
                        {
                            if (task.Title != null && task.Title == selectedTask.Title)
                            {
                                task.Deleted = true;
                                toDelete = task;
                                break;
                            }
                        }
                    }                 
                }
            }
            if (toDelete != null)
            {
                showEmptyTaskTextBlock(Messages.GetMsgValue(MessageKey.SELECT_TASK));

                // trying to delete from tasksView
                ObservableCollection<Data.Model.Task> tasksFromView = (ObservableCollection<Data.Model.Task>) tasksView.ItemsSource;
                Data.Model.Task toDeleteFromVisible = null;
                foreach (Data.Model.Task taskFromView in tasksFromView)
                {
                    if ((taskFromView.Id != null && taskFromView.Id == toDelete.Id) || taskFromView.Title == toDelete.Title)
                    {
                        toDeleteFromVisible = taskFromView;
                        break;
                    }
                }
                if (toDeleteFromVisible != null)
                {
                    tasksFromView.Remove(toDeleteFromVisible);
                }
                taskDataSource.RefreshLists();
                RefreshCounters(taskDataSource);
                hideTaskEditButtons();
            }
        }

        private void tasksView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool markingAllVisible = false;
            if (tasksView.SelectedItems.Count > 0)
            {
                foreach (Data.Model.Task task in tasksView.SelectedItems)
                {
                    if (task.Status != "completed")
                    {
                        markingAllVisible = true;
                    }
                }
                
            }
            if (markingAllVisible)
            {
                markListCompleted.Visibility = Visibility.Visible;
            }
            else
            {
                markListCompleted.Visibility = Visibility.Collapsed;
            }
        }

        private void markListCompleted_Click(object sender, RoutedEventArgs e)
        {
            foreach (Data.Model.Task task in tasksView.SelectedItems)
            {
                if (task.Status != "completed")
                {
                    markTaskCompleted(task);
                }
               
            }
            TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
            taskDataSource.RefreshLists();
            RefreshCounters(taskDataSource);
            showEmptyTaskTextBlock(Messages.GetMsgValue(MessageKey.SELECT_TASK));
            hideTaskEditButtons();
        }

        private void addList_Click(object sender, RoutedEventArgs e)
        {
            Popup myPopup = new Popup();
            myPopup.HorizontalAlignment = HorizontalAlignment.Center;
            myPopup.VerticalAlignment = VerticalAlignment.Center;
            myPopup.IsLightDismissEnabled = true;

            Border b = new Border();
            b.BorderBrush = new SolidColorBrush(Colors.Gray);
            b.BorderThickness = new Thickness(2);
            b.Width = 400;

            StackPanel s = new StackPanel();
            b.Child = s;
            s.Orientation = Orientation.Vertical;
            s.Width = 400;
            s.Background = new SolidColorBrush(Colors.White);

            TextBlock text = new TextBlock();
            text.Text = "Name for the list:";
            text.Margin = new Thickness(10, 5, 20, 0);
            text.Foreground = new SolidColorBrush(Colors.SteelBlue);
            text.FontSize = 16;
            s.Children.Add(text);

            TextBox input = new TextBox();
            s.Children.Add(input);
            input.Margin = new Thickness(10, 5, 10, 5);

            Button save = new Button();
            save.HorizontalAlignment = HorizontalAlignment.Right;
            save.Margin = new Thickness(10, 0, 10, 5);
            save.Content = "Save";
            save.Click += new RoutedEventHandler(delegate(object sender1, RoutedEventArgs ev)
            {
                TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
                Data.Model.TaskList t = new Data.Model.TaskList();
                t.Title = input.Text;
                t.Updated = DateTime.Now;
                taskDataSource.TaskLists.Add(t);
                taskDataSource.RefreshLists();
                myPopup.IsOpen = false;
            });
            s.Children.Add(save);

            myPopup.Child = b;

            myPopup.IsOpen = true;
            myCanvas1.Children.Add(myPopup);
        }

        private void add_Click(object sender, RoutedEventArgs e)
        {
            HideEmptyTaskTextBlockIfNeeded(false);
            EnableTaskEditMode();
            ClearTaskDetails();
            selectedTask = null;
        }

        private void ClearTaskDetails()
        {
            taskTitleTextBox.Text = "title";
            taskListTextBox.Text = String.Empty;
            taskUpdatedDateLabel.Text = String.Empty;
            taskUpdatedDateTextBox.Text = String.Empty;
            this.taskDueDateLabel.Text = "Due:";
            taskDueDateLabel.Foreground = new SolidColorBrush(Colors.Gray);
            taskDueDateTextBox.Text = "None";
            taskDueDateTextBox.Foreground = new SolidColorBrush(Colors.Gray);
            taskNotesTextBox.Text = "notes";
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTask != null)
            {
                taskTitleTextBox.Text = selectedTask.Title;
                taskNotesTextBox.Text = selectedTask.Notes;
                if (selectedTask.Due != DateTime.MinValue)
                {
                    this.taskDueDateTextBox.Text = selectedTask.Due.Date.ToString();
                    if (selectedTask.IsOverdue)
                    {
                        taskDueDateLabel.Foreground = new SolidColorBrush(Colors.Red);
                        taskDueDateTextBox.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }
                else
                {
                    this.taskDueDateTextBox.Text = "None";
                }
                
            }
            else 
            {
                showEmptyTaskTextBlock(Messages.GetMsgValue(MessageKey.SELECT_TASK));
                hideTaskEditButtons();
            }

            DisableTaskEditMode(selectedTask);
        }

        async private void sync_Click(object sender, RoutedEventArgs e)
        {
            Synchronizer synchronizer = await Synchronizer.GetInstance();
            if (synchronizer.IsInternetAvailable())
            {
                progressBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
                TaskDataSource taskDataSource = (TaskDataSource)App.Current.Resources["taskDataSource"];
                bool success = await synchronizer.Synchronize();
                if (success)
                {
                    // pobranie jeszcze raz i zastapienie
                    await taskDataSource.GetTaskListsAsync(true);
                    showEmptyTextBlock(Messages.GetMsgValue(MessageKey.SELECT_LIST));
                    tasksView.ItemsSource = null;
                    taskDataSource.RefreshLists();
                    RefreshCounters(taskDataSource);
                }
                else
                {
                    var messageDialog =
                        new Windows.UI.Popups.MessageDialog(
                            Messages.GetMsgValue(MessageKey.SYNC_ERROR_CNTNT), Messages.GetMsgValue(MessageKey.SYNC_ERROR));
                }

                progressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                var messageDialog =
                new Windows.UI.Popups.MessageDialog(
                    Messages.GetMsgValue(MessageKey.NO_INTERNET_CNTNT), Messages.GetMsgValue(MessageKey.NO_INTERNET));

                messageDialog.Commands.Add(new UICommand("OK"));
            }
        }

    }
}
