using System.Windows;
using LibraryManagement.ViewModels;

namespace LibraryManagement;

/// <summary>
/// Главное окно: таблица книг с фильтрами по жанру/автору и кнопками Добавить/Изменить/Удалить.
/// Открывает диалог добавления/редактирования и обновляет таблицу после сохранения.
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _viewModel.SetAddEditCallbacks(OnAddBook, OnEditBook, RefreshGrid);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        _viewModel?.LoadData();
    }

    private void OnAddBook()
    {
        var dialog = new AddEditBookWindow(null);
        if (dialog.ShowDialog() == true)
            RefreshGrid();
    }

    private void OnEditBook(Models.Book book)
    {
        var dialog = new AddEditBookWindow(book);
        if (dialog.ShowDialog() == true)
            RefreshGrid();
    }
}
