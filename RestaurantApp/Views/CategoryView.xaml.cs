using RestaurantApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantApp.Views
{
    public partial class CategoryView : UserControl
    {
        public CategoryView()
        {
            InitializeComponent();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is CategoryViewModel viewModel)
            {
                viewModel.IsEditing = true;
            }
        }
    }
}