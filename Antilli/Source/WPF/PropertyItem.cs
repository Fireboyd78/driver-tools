using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Antilli
{
    public delegate bool PropertyUpdatedCallback(string updatedValue);

    public class PropertyItem
    {
        private string m_value;

        public string Name { get; set; }

        public string Value
        {
            get { return m_value; }
        }

        PropertyUpdatedCallback Callback { get; set; }

        public bool ReadOnly { get; set; }

        public void AddToPanel(StackPanel panel, string[] items)
        {
            var label = new Label() {
                Content = $"{Name}:"
            };

            var chooser = new ComboBox() {
                IsReadOnly = true,
            };

            foreach (var item in items)
                chooser.Items.Add(item);

            chooser.SelectedItem = Value;

            if (!ReadOnly)
            {
                chooser.SelectionChanged += (o, e) => {
                    var values = e.AddedItems;
                    var value = values[0] as string;

                    if (!String.Equals(m_value, value))
                    {
                        if (Callback(value))
                        {
                            m_value = value;
                        }
                        else
                        {
                            MessageBox.Show("What the fuck did you do?!", "Dude...", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                };
            }

            var grid = new Grid() {
                Margin = new Thickness(4)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(label);
            grid.Children.Add(chooser);

            Grid.SetColumn(chooser, 1);

            panel.Children.Add(grid);
        }

        public void AddToPanel(StackPanel panel)
        {
            var label = new Label() {
                Content = $"{Name}:"
            };

            var txtBox = new TextBox() {
                Text = Value,
                IsReadOnly = ReadOnly,
            };

            var oldBrush = txtBox.BorderBrush;

            if (!ReadOnly)
            {
                txtBox.KeyDown += (o, e) => {
                    if (e.Key == Key.Enter)
                    {
                        if (!String.Equals(m_value, txtBox.Text))
                        {
                            if (Callback(txtBox.Text))
                            {
                                m_value = txtBox.Text;
                            }
                            else
                            {
                                MessageBox.Show("Invalid value, please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                txtBox.Text = m_value;
                            }
                        }
                    }
                };
            }

            var grid = new Grid() {
                Margin = new Thickness(4)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(75) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(label);
            grid.Children.Add(txtBox);

            Grid.SetColumn(txtBox, 1);

            panel.Children.Add(grid);
        }

        public PropertyItem(string name, PropertyUpdatedCallback callback, string initialValue = "")
        {
            Name = name;
            Callback = callback;

            m_value = initialValue;
        }
    }
}
