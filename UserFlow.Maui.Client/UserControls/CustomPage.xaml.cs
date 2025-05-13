/// *****************************************************************************************
/// @file CustomPage.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Custom base class for content pages with bindable navigation bar and back button visibility.
/// *****************************************************************************************

namespace UserFlow.Maui.Client.UserControls;

using System;

/// <summary>
/// 📄 CustomPage extends ContentPage with bindable properties for controlling UI navigation elements.
/// </summary>
public partial class CustomPage : ContentPage
{
    /// <summary>
    /// 👉 Default constructor.
    /// </summary>
    public CustomPage()
    {
        InitializeComponent(); // ⚙️ Load associated XAML components
    }

    /// <summary>
    /// 🔧 BindableProperty to control visibility of the navigation bar.
    /// </summary>
    public static readonly BindableProperty IsNavBarVisibleProperty =
        BindableProperty.Create(
            propertyName: nameof(IsNavBarVisible),               // 🔑 Property name
            returnType: typeof(bool),                            // 🔢 Type
            declaringType: typeof(CustomPage),                   // 📦 Declaring type
            defaultValue: true,                                  // ✅ Default: visible
            defaultBindingMode: BindingMode.TwoWay);             // 🔄 Supports two-way binding

    /// <summary>
    /// 🔳 Gets or sets whether the navigation bar is visible.
    /// </summary>
    public bool IsNavBarVisible
    {
        get => (bool)GetValue(IsNavBarVisibleProperty);          // 🔍 Get value from BindableProperty
        set => SetValue(IsNavBarVisibleProperty, value);         // ✏️ Set value to BindableProperty
    }

    /// <summary>
    /// 🔧 BindableProperty to control visibility of the back button.
    /// </summary>
    public static readonly BindableProperty IsBackButtonVisibleProperty =
        BindableProperty.Create(
            propertyName: nameof(IsBackButtonVisible),            // 🔑 Property name
            returnType: typeof(bool),                             // 🔢 Type
            declaringType: typeof(CustomPage),                    // 📦 Declaring type
            defaultValue: true,                                   // ✅ Default: visible
            defaultBindingMode: BindingMode.TwoWay);              // 🔄 Supports two-way binding

    /// <summary>
    /// 🔙 Gets or sets whether the back button is visible.
    /// </summary>
    public bool IsBackButtonVisible
    {
        get => (bool)GetValue(IsBackButtonVisibleProperty);       // 🔍 Get value from BindableProperty
        set => SetValue(IsBackButtonVisibleProperty, value);      // ✏️ Set value to BindableProperty
    }

    /// <summary>
    /// ⬅️ Handles the back button click by popping the current page from the navigation stack.
    /// </summary>
    private void BackButton_Clicked(object sender, EventArgs e)
    {
        App.CurrentPage.Navigation.PopAsync(true);                // 🔙 Navigate back
    }
}

/// *****************************************************************************************
/// @remarks 📄 Developer Notes:
/// - Used as a flexible base class for pages that require dynamic control over navigation bar visibility.
/// - XAML can bind to `IsNavBarVisible` and `IsBackButtonVisible`.
/// - Integrates with App.CurrentPage and Navigation stack for consistent page transitions.
/// *****************************************************************************************
