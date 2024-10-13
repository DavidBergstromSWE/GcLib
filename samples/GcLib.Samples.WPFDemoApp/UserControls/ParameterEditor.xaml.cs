using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using GcLib;
using GcLib.Utilities.Collections;

namespace FusionViewer.UserControls;

/// <summary>
/// Control for displaying and editing parameters, with parameter category filter and parameter search string functionality.
/// </summary>
public partial class ParameterEditor : UserControl, INotifyPropertyChanged, IDisposable
{
    #region Private fields

    /// <summary>
    /// Parameter collection bound to the control.
    /// </summary>
    private IReadOnlyParameterCollection _parameterCollection;

    // backing-fields
    private ObservableCollection<GcParameter> _parameters;
    private ObservableCollection<string> _categories;
    private string _selectedCategory;
    private string _searchString;

    /// <summary>
    /// Represents the task of updating a parameter.
    /// </summary>
    private Task _updateParameterTask;


    /// <summary>
    /// Provides the ability to cancel an ongoing task.
    /// </summary>
    private CancellationTokenSource _cts = new();

    #endregion

    #region Public fields

    // Dependency property identifier fields.      
    public static readonly DependencyProperty ToolbarVisibilityProperty = DependencyProperty.Register(nameof(ToolbarVisibility), typeof(System.Windows.Visibility), typeof(ParameterEditor), new PropertyMetadata(System.Windows.Visibility.Visible));
    public static readonly DependencyProperty ParameterVisibilityProperty = DependencyProperty.Register(nameof(ParameterVisibility), typeof(GcVisibility), typeof(ParameterEditor), new PropertyMetadata(GcVisibility.Guru, new PropertyChangedCallback(OnParameterVisibilityChanged)));
    public static readonly DependencyProperty ParameterCollectionProperty = DependencyProperty.Register(nameof(ParameterCollection), typeof(IReadOnlyParameterCollection), typeof(ParameterEditor), new PropertyMetadata(null, new PropertyChangedCallback(OnParameterCollectionChanged)));
    public static readonly DependencyProperty HeadersVisibilityProperty = DependencyProperty.Register(nameof(HeadersVisibility), typeof(DataGridHeadersVisibility), typeof(ParameterEditor), new PropertyMetadata(DataGridHeadersVisibility.Column));
    public static readonly DependencyProperty ParameterUpdateDelayProperty = DependencyProperty.Register(nameof(ParameterUpdateDelay), typeof(uint), typeof(ParameterEditor), new PropertyMetadata(400u));

    #endregion

    #region DependencyProperties

    /// <summary>
    /// Visibility of toolbar with category filter and parameter search textbox.
    /// </summary>
    public System.Windows.Visibility ToolbarVisibility
    {
        get => (System.Windows.Visibility)GetValue(ToolbarVisibilityProperty);
        set => SetValue(ToolbarVisibilityProperty, value);
    }

    /// <summary>
    /// Visibility of datagrid headers.
    /// </summary>
    public DataGridHeadersVisibility HeadersVisibility
    {
        get { return (DataGridHeadersVisibility)GetValue(HeadersVisibilityProperty); }
        set { SetValue(HeadersVisibilityProperty, value); }
    }

    /// <summary>
    /// Parameter visibility level. Parameters will only be visible up to (and including) this specified level.
    /// </summary>
    public GcVisibility ParameterVisibility
    {
        get => (GcVisibility)GetValue(ParameterVisibilityProperty);
        set => SetValue(ParameterVisibilityProperty, value);
    }

    /// <summary>
    /// Collection of parameters.
    /// </summary>
    public IReadOnlyParameterCollection ParameterCollection
    {
        get => (IReadOnlyParameterCollection)GetValue(ParameterCollectionProperty);
        set => SetValue(ParameterCollectionProperty, value);
    }

    /// <summary>
    /// Represents the time waited before updating a parameter after changing its value (in milliseconds).
    /// </summary>
    public uint ParameterUpdateDelay
    {
        get => (uint)GetValue(ParameterUpdateDelayProperty);
        set => SetValue(ParameterUpdateDelayProperty, value);
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Collection of parameters to be displayed and edited.
    /// </summary>
    public ObservableCollection<GcParameter> Parameters
    {
        get => _parameters;
        set
        {
            _parameters = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// List of categories for parameters.
    /// </summary>
    public ObservableCollection<string> Categories
    {
        get => _categories;
        private set
        {
            _categories = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Category selected in the filter.
    /// </summary>
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            OnPropertyChanged();

            // Refresh view with new settings.
            RefreshView();
        }
    }

    /// <summary>
    /// Search string entered.
    /// </summary>
    public string SearchString
    {
        get => _searchString;
        set
        {
            _searchString = value;
            OnPropertyChanged();

            // Refresh view with new settings.
            RefreshView();
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Relays a parameter update invoked by a command to changes in the parameter collection.
    /// </summary>
    public IAsyncRelayCommand<GcParameter> UpdateParameterCommand { get; }

    /// <summary>
    /// Relays a request for an updated parameter collection invoked by a command.
    /// </summary>
    public IAsyncRelayCommand UpdateParameterCollectionCommand { get; }

    #endregion

    #region Private methods

    /// <summary>
    /// Updates parameter in the collection.
    /// </summary>
    /// <param name="parameter">Parameter to be updated.</param>
    private void UpdateParameter(GcParameter parameter)
    {
        try
        {
            switch (parameter.Type)
            {
                case GcParameterType.Command:

                    _parameterCollection.ExecuteParameterCommand(parameter.Name);

                    break;

                case GcParameterType.Integer:
                case GcParameterType.Float:
                case GcParameterType.String:
                case GcParameterType.Enumeration:
                case GcParameterType.Boolean:
                default:

                    if (parameter.IsWritable)
                        _parameterCollection.SetParameterValue(parameter.Name, parameter.ToString());
                    
                    break;
            }
        }
        catch (Exception ex)
        {
            // handle here?
            _ = MessageBox.Show($"Unable to update parameter {parameter.Name}: {ex.GetBaseException().Message}", "Input Error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Updates parameter and parameter collection.
    /// </summary>
    /// <param name="parameter">Parameter to be updated.</param>
    /// <returns>(awaitable) <see cref="Task"/>.</returns>
    private Task UpdateParameterAsync(GcParameter parameter)
    {
        // Cancel current update task if a new update is requested.
        if (_updateParameterTask != null)
            _cts.Cancel();

        if (_updateParameterTask == null || _updateParameterTask.IsCanceled || _updateParameterTask.IsFaulted || _updateParameterTask.IsCompleted)
        {
            _cts = new CancellationTokenSource();

            // Wait some time before updating parameter in collection.
            _updateParameterTask = Task.Delay((int)ParameterUpdateDelay, _cts.Token).ContinueWith(async _ =>
            {
                try
                {
                    // Update parameter in collection.
                    await Task.Run(() => UpdateParameter(parameter));
                }
                finally
                {
                    // Always keep collection updated (even when exceptions are raised).
                    await UpdateParameterCollectionAsync().ConfigureAwait(false);  
                }
            }, _cts.Token);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the parameter collection.
    /// </summary>
    /// <returns>(awaitable) <see cref="Task"/>.</returns>
    private async Task UpdateParameterCollectionAsync()
    {
        await Task.Run(_parameterCollection.Update);   
    }

    /// <summary>
    /// Filters parameters up to selected parameter visibility level, in selected parameter category, using entered search string.
    /// </summary>
    private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
    {
        // Only accept parameter if visible at currently selected visibility level and for the currently selected category, supporting the currently typed search string.
        var parameter = e.Item as GcParameter;
        if (parameter is GcParameter gcParameter)
            e.Accepted = gcParameter.IsVisible(ParameterVisibility)
                         && (gcParameter.Category == SelectedCategory || SelectedCategory == "All")
                         && gcParameter.Name.Contains(SearchString ?? "", StringComparison.OrdinalIgnoreCase);
        else
            e.Accepted = false;
    }

    /// <summary>
    /// Refreshes view with current filter settings.
    /// </summary>
    private void RefreshView()
    {
        var cvs = (CollectionViewSource)grid.FindResource("cvsParameters");
        cvs.View?.Refresh();
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates control for displaying and editing parameters, with parameter category filter and parameter search string functionality.
    /// </summary>
    public ParameterEditor()
    {
        InitializeComponent();

        Parameters = [];

        // Instantiate commands.
        UpdateParameterCommand = new AsyncRelayCommand<GcParameter>(UpdateParameterAsync, p => p is not null);
        UpdateParameterCollectionCommand = new AsyncRelayCommand(UpdateParameterCollectionAsync);
    }

    #endregion

    #region Events

    /// <summary>
    /// Eventhandler to binding changes to ParameterCollection dependencyproperty, initializing new list of parameters to be displayed.
    /// </summary>
    private static void OnParameterCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (ParameterEditor)d;

        // Make CLR copy (to be used in thread calls).
        editor._parameterCollection = (IReadOnlyParameterCollection)e.NewValue;

        if (editor._parameterCollection == null)
            return;

        // Update parameter collection.
        editor.Parameters = new ObservableCollection<GcParameter>(editor._parameterCollection);

        // Update category list.
        editor.UpdateCategories();

        // Refresh view with new settings.
        editor.RefreshView();
    }

    /// <summary>
    /// Eventhandler to binding changes to ParameterVisibility dependencyproperty, re-filtering parameters and categories to new visibility setting.
    /// </summary>
    private static void OnParameterVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (ParameterEditor)d;

        // Update category list.
        editor.UpdateCategories();

        // Refresh view with new settings.
        editor.RefreshView();
    }

    /// <summary>
    /// Updates list of categories.
    /// </summary>
    private void UpdateCategories()
    {
        // Setup category filter.
        var categoryList = _parameterCollection.GetCategories(ParameterVisibility).ToList();
        categoryList.Insert(0, "All");
        Categories = new ObservableCollection<string>(categoryList);

        // Select "All" as initial filter setting.
        SelectedCategory = Categories[0];
    }

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Raises PropertyChanged event for a property.
    /// </summary>
    /// <param name="propertyName">Name of property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Eventhandler to Unloaded events.
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Clear bindings in datatemplates used in datagrid (prevents multiplying of commands from events raised).
        BindingOperations.ClearAllBindings(datagrid);

        Dispose();
    }

    /// <summary>
    /// Eventhandler to key events in ComboBox.
    /// </summary>
    private void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Disable default behaviour of opening combobox when F4 key is pressed and execute keybinding command instead.
        if (e.Key == Key.F4)
        {         
            var binding = Application.Current.MainWindow.InputBindings.Cast<KeyBinding>().Where(k => k.Key == Key.F4);
            if (binding.Any())
                binding.First()?.Command.Execute((DisplayChannel)binding.First().CommandParameter);

            e.Handled = true;
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        // Let last parameter update complete.
        _updateParameterTask?.Wait();

        // Cancel all other ongoing tasks.
        _cts.Cancel();

        // Dispose resources (needed?).
        _cts?.Dispose();
        _updateParameterTask?.Dispose();

        GC.SuppressFinalize(this);
    }

    #endregion
}