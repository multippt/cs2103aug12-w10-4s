﻿// This file is to be excluded from code review
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Calendo.Data;
using Calendo.Logic;
using Calendo.GoogleCalendar;
using Calendo.Diagnostics;

namespace Calendo
{
    /// <summary>
    /// Interaction logic for DebugMode.xaml
    /// </summary>
    public partial class DebugMode : Window
    {
        // This is a testing class with a GUI interface
        // Used for exploratory testing of experimental features or proof-of-concept implementation
        TaskManager tm = TaskManager.Instance;
        CommandProcessor cp = new CommandProcessor();
        public DebugMode()
        {
            InitializeComponent();

            // TM delegate
            TaskManager.UpdateHandler delegateMethod = new TaskManager.UpdateHandler(this.SubscriberMethod);
            tm.AddSubscriber(delegateMethod);

            // Debug delegate
            DebugTool.AddSubscriber(new DebugTool.NotifyHandler(this.Alert));

        }

        private void Alert(string message)
        {
            this.StatusLabel.Content = message;
        }

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private Dictionary<int, Entry> entryDictionary;
        private void UpdateList()
        {
            //this.listBox1.Items.Clear();
            entryDictionary = new Dictionary<int, Entry>();
            Dictionary<int, int> indexMap = new Dictionary<int, int>();

            tm.Load(); // prevent concurrency issues
            for (int i = 0; i < tm.Entries.Count; i++)
            {
                //this.listBox1.Items.Add("[" + tm.Entries[i].ID.ToString() + "] " + tm.Entries[i].Description);
                entryDictionary.Add(i, tm.Entries[i]);
                indexMap.Add(i + 1, i + 1);
            }
            this.listBox1.ItemsSource = entryDictionary;
            cp.IndexMap = indexMap;
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            // Used for testing CP
            if (e.Key == Key.Enter)
            {
                cp.ExecuteCommand(textBox1.Text);
                tm.Load(); // Update TM with changes done by CP
                UpdateList();
                textBox1.Text = "";
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            tm.Add(this.textBox1.Text, "", "", "", "");
            this.textBox1.Text = "";
            UpdateList();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            if (this.listBox1.Items.Count > 0 && this.listBox1.SelectedIndex >= 0)
            {
                //tm.Remove(tm.Entries[this.listBox1.SelectedIndex].ID);
                tm.Remove(this.listBox1.SelectedIndex + 1);
            }
            UpdateList();
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            tm.Undo();
            UpdateList();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This button is not in use");
            //MessageBox.Show(GoogleCalendar.GoogleCalendar.Import());
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager sm = new SettingsManager();
            sm.GetSetting("null");
            sm.SetSetting("test", "a");
            MessageBox.Show(sm.GetSetting("test"));
        }

        private void buttonclose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            //((ListBoxItem)this.listBox1.Items[0]).Focus();
        }

        private void buttonnew_Click(object sender, RoutedEventArgs e)
        {
        }

        private void button6_Click(object sender, RoutedEventArgs e)
        {
            JSON<string> test = new JSON<string>();
            string testDate = test.DateToJSON(DateTime.Now);
            MessageBox.Show(testDate);
            DateTime testDateTime = test.JSONToDate(testDate).ToLocalTime();
            MessageBox.Show(testDateTime.ToString());
        }

        private void listBox1_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void buttonInput_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            KeyValuePair<int, Entry> dContext = (KeyValuePair<int, Entry>)btn.DataContext;

            Entry currentEntry = dContext.Value;
            //JSON<Entry> jsonParse = new JSON<Entry>();
            //MessageBox.Show(jsonParse.Serialize(currentEntry));
            int taskId = tm.Entries.FindIndex(x => x == currentEntry);
            tm.Remove(taskId + 1);
        }

        // Used to prevent list update conflicts when items are dynamically changed
        private bool isUpdating = false;

        private void TextBox_KeyUp_1(object sender, KeyEventArgs e)
        {
            TextBox currentTextbox = sender as TextBox;
            KeyValuePair<int, Entry> dContext = (KeyValuePair<int, Entry>)currentTextbox.DataContext;
            if (e.Key == Key.Return)
            {
                // Request change command if needed
                if (currentTextbox.Text != dContext.Value.Description)
                {
                    string command = "/change " + (dContext.Key + 1).ToString() + " " + currentTextbox.Text;
                    isUpdating = true;
                    cp.ExecuteCommand(command);
                    UpdateList();
                    this.listBox1.SelectedIndex = dContext.Key;
                    currentTextbox.Text = dContext.Value.Description;
                }
                currentTextbox.IsReadOnly = true;
            }
        }

        private void TextBox_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {
            TextBox currentTextbox = sender as TextBox;
            currentTextbox.IsReadOnly = false;
            currentTextbox.Focusable = true;
            currentTextbox.Focus();
            e.Handled = true; // Required, so that focus do not go back to list item
        }

        private void TextBox_LostKeyboardFocus_1(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox currentTextbox = sender as TextBox;
            if (!currentTextbox.IsReadOnly && !isUpdating)
            {
                // Request change command if needed
                KeyValuePair<int, Entry> dContext = (KeyValuePair<int, Entry>)currentTextbox.DataContext;
                if (currentTextbox.Text != dContext.Value.Description)
                {
                    // Show command in textbox
                    //this.textBox1.Text = "/change " + (dContext.Key + 1).ToString() + " " + currentTextbox.Text;
                    //currentTextbox.Text = dContext.Value.Description;
                    string command = "/change " + (dContext.Key + 1).ToString() + " " + currentTextbox.Text;
                    cp.ExecuteCommand(command);
                    UpdateList();
                    this.listBox1.SelectedIndex = dContext.Key;
                    currentTextbox.Text = dContext.Value.Description;
                }
            }
            isUpdating = false;
            currentTextbox.IsReadOnly = true;
            currentTextbox.Focusable = false;
        }

        private void TextBox_PreviewMouseDown_1(object sender, MouseButtonEventArgs e)
        {
            TextBox currentTextbox = sender as TextBox;
            KeyValuePair<int, Entry> dContext = (KeyValuePair<int, Entry>)currentTextbox.DataContext;
            if (currentTextbox.IsReadOnly)
            {
                currentTextbox.Focusable = false; // So that can select list item
            }
        }

        private void listBox1_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        private void SubscriberMethod()
        {
            this.StatusLabel.Content = "The list has been updated";
            this.UpdateList();
        }
    }
}
