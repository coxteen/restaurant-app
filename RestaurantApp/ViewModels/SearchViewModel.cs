using RestaurantApp.Helpers;
using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class SearchViewModel : BaseViewModel
    {
        private readonly ProductService _productService;
        private string _searchTerm;
        private bool _searchForAllergen;
        private bool _invertSearch;
        private ObservableCollection<Product> _searchResults;

        public string SearchTerm
        {
            get { return _searchTerm; }
            set { SetProperty(ref _searchTerm, value); }
        }

        public bool SearchForAllergen
        {
            get { return _searchForAllergen; }
            set { SetProperty(ref _searchForAllergen, value); }
        }

        public bool InvertSearch
        {
            get { return _invertSearch; }
            set { SetProperty(ref _invertSearch, value); }
        }

        public ObservableCollection<Product> SearchResults
        {
            get { return _searchResults; }
            set { SetProperty(ref _searchResults, value); }
        }

        public ICommand SearchCommand { get; }

        public SearchViewModel(ProductService productService)
        {
            _productService = productService;
            SearchResults = new ObservableCollection<Product>();
            SearchCommand = new RelayCommand(ExecuteSearch, CanExecuteSearch);
        }

        private bool CanExecuteSearch(object parameter)
        {
            return !string.IsNullOrWhiteSpace(SearchTerm);
        }

        private void ExecuteSearch(object parameter)
        {
            var allProducts = _productService.GetAllProducts();
            IEnumerable<Product> results;

            if (SearchForAllergen)
            {
                // Search by allergen
                if (InvertSearch)
                {
                    // Products that DON'T contain the allergen
                    results = allProducts.Where(p => !p.Allergens.Any(a =>
                        a.Name.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0));
                }
                else
                {
                    // Products that DO contain the allergen
                    results = allProducts.Where(p => p.Allergens.Any(a =>
                        a.Name.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0));
                }
            }
            else
            {
                // Search by product name
                if (InvertSearch)
                {
                    // Products that DON'T contain the search term in name
                    results = allProducts.Where(p =>
                        p.Name.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) < 0);
                }
                else
                {
                    // Products that DO contain the search term in name
                    results = allProducts.Where(p =>
                        p.Name.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
                }
            }

            SearchResults.Clear();
            foreach (var product in results)
            {
                SearchResults.Add(product);
            }
        }
    }
}