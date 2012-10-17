﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using Calendo.CommandProcessing;
using Calendo.Data;

namespace Calendo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AutoSuggest AutoSuggestViewModel;
        private CommandProcessor CommandProcessor;

        public static RoutedCommand UndoCommand = new RoutedCommand();
        public static RoutedCommand RedoCommand = new RoutedCommand();
        public static RoutedCommand DelCommand = new RoutedCommand();

        public MainWindow()
        {
            InitializeComponent();

            UndoCommand.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control));
            RedoCommand.InputGestures.Add(new KeyGesture(Key.Y, ModifierKeys.Control));
            DelCommand.InputGestures.Add(new KeyGesture(Key.Delete));

            AutoSuggestViewModel = new AutoSuggest();
            DataContext = AutoSuggestViewModel;
            CommandProcessor = new CommandProcessor();
            UpdateItemsList();
        }

        private void CommandBarLostFocus(object sender, RoutedEventArgs e)
        {
            if (CommandBar.Text.Length == 0)
            {
                EnterCommandWatermark.Visibility = Visibility.Visible;
            }
        }

        private void CommandBarGotFocus(object sender, RoutedEventArgs e)
        {
            EnterCommandWatermark.Visibility = Visibility.Collapsed;
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximiseWindow(sender, e);
            }

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimiseWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximiseWindow(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void WindowStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Toggling of WindowStyle is needed to get around the window
                // overlapping the TaskBar if it is maximized when WindowStyle is None.
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Maximized;
                WindowStyle = WindowStyle.None;

                RestoreButton.Visibility = Visibility.Visible;
                MaximiseButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                RestoreButton.Visibility = Visibility.Collapsed;
                MaximiseButton.Visibility = Visibility.Visible;
            }
        }

        private void CommandBarKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                // Select the first item in the auto-suggest list, and give it focus.
                AutoSuggestList.SelectedIndex = 0;
                ListBoxItem selectedItem = AutoSuggestList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                selectedItem.Focus();
            }
            else if (e.Key == Key.Return)
            {
                string inputString = CommandBar.Text;
                if (inputString.Length > 0)
                {
                    CommandProcessor.ExecuteCommand(inputString);
                    CommandBar.Clear();
                    UpdateItemsList();
                }
            }
            else if (e.Key == Key.Escape)
            {
                DefocusCommandBar();
            }
            else if (!CommandBar.Text.StartsWith("/"))
            {
                FilterListContents();
            }
        }

        private void FilterListContents()
        {
            string searchString = CommandBar.Text.ToLowerInvariant().Trim();
            if (CommandBar.Text != "")
            {
                TaskList.Items.Filter = delegate(object o)
                                                {
                                                    KeyValuePair<int, Entry> currentPair = (KeyValuePair<int, Entry>)o;
                                                    Entry currentEntry = currentPair.Value;
                                                    if (currentEntry != null)
                                                    {
                                                        string lowercaseDescription =
                                                            currentEntry.Description.ToLowerInvariant();
                                                        return lowercaseDescription.Contains(searchString);
                                                    }

                                                    return false;
                                                };
            }
            else
            {
                TaskList.Items.Filter = null;
            }
        }

        private void DefocusCommandBar()
        {
            TaskList.Focus();
            AutoSuggestBorder.Visibility = Visibility.Collapsed;
        }

        private void UpdateItemsList()
        {
            Dictionary<int, Entry> itemDictionary = new Dictionary<int, Entry>();

            int count = 1;
            foreach (Entry currentEntry in CommandProcessor.TaskList)
            {
                itemDictionary.Add(count, currentEntry);
                count++;
            }

            TaskList.ItemsSource = itemDictionary;
        }

        private void AutoSuggestListKeyDown(object sender, KeyEventArgs e)
        {
            // This is on KeyDown, as KeyUp triggers after the SelectedIndex has already changed.
            // (Results in the first item being impossible to select via keyboard.)
            if (e.Key == Key.Up && AutoSuggestList.SelectedIndex == 0)
            {
                CommandBar.Focus();
                AutoSuggestList.SelectedIndex = -1;
            }
        }

        private void SetCommandFromSuggestion()
        {
            string suggestion = (string)AutoSuggestList.SelectedItem;
            bool isInputCommand = suggestion != null && suggestion.First() == AutoSuggest.COMMAND_INDICATOR;
            if (isInputCommand)
            {
                string command = suggestion.Split()[0];
                CommandBar.Text = command;
                CommandBar.Focus();
                CommandBar.SelectionStart = command.Length;

                AutoSuggestBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void CommandBarTextChanged(object sender, TextChangedEventArgs e)
        {
            AutoSuggestViewModel.SetSuggestions(CommandBar.Text);

            AutoSuggestBorder.Visibility = CommandBar.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            DebugMode dm = new DebugMode();
            dm.Show();
        }

        private void AutoSuggestListMouseUp(object sender, MouseButtonEventArgs e)
        {
            SetCommandFromSuggestion();
        }

        private void TaskListDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            string command = "/change";

            ExecuteCommandOnSelectedTask(command);
        }

        private void UndoHandler(object sender, ExecutedRoutedEventArgs e)
        {
            CommandProcessor.ExecuteCommand("/undo");
            UpdateItemsList();
        }

        private void RedoHandler(object sender, ExecutedRoutedEventArgs e)
        {
            CommandProcessor.ExecuteCommand("/redo");
            UpdateItemsList();
        }

        private void GridMouseDown(object sender, MouseButtonEventArgs e)
        {
            DefocusCommandBar();
        }

        private void DeleteHandler(object sender, ExecutedRoutedEventArgs e)
        {
            string command = "/remove";

            ExecuteCommandOnSelectedTask(command);
        }

        private void ExecuteCommandOnSelectedTask(string command)
        {
            var selectedItem = TaskList.SelectedItem;
            if (selectedItem != null)
            {
                KeyValuePair<int, Entry> selectedPair = (KeyValuePair<int, Entry>)selectedItem;
                Entry selectedEntry = selectedPair.Value;

                int selectedIndex = selectedPair.Key;
                CommandBar.Text = command + " " + selectedIndex;
                CommandBar.Focus();
                CommandBar.SelectionStart = CommandBar.Text.Length;
            }
        }

        private void AutoSuggestListKeyUp(object sender, KeyEventArgs e)
        {
            // This is on KeyUp (and not KeyDown) to prevent the event
            // from bubbling through to the Command Bar - would cause
            // the command to be filled, then instantly executed.
            if (e.Key == Key.Return)
            {
                SetCommandFromSuggestion();
            }
        }
    }
}
