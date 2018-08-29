﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ReactiveUI
{
#if NETFX_CORE
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
#else
    using System.Windows;
    using System.Windows.Controls;
#endif

    /// <summary>
    /// A <see cref="UserControl"/> that is reactive.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is a <see cref="UserControl"/> that is also reactive. That is, it implements <see cref="IViewFor{TViewModel}"/>.
    /// You can extend this class to get an implementation of <see cref="IViewFor{TViewModel}"/> rather than writing one yourself.
    /// </para>
    /// <para>
    /// Note that the XAML for your control must specify the same base class, including the generic argument you provide for your view
    /// model. To do this, use the <c>TypeArguments</c> attribute as follows:
    /// <code>
    /// <![CDATA[
    /// <rxui:ReactiveUserControl
    ///         x:Class="views:YourView"
    ///         x:TypeArguments="vms:YourViewModel"
    ///         xmlns:rxui="http://reactiveui.net"
    ///         xmlns:views="clr-namespace:Foo.Bar.Views"
    ///         xmlns:vms="clr-namespace:Foo.Bar.ViewModels">
    ///     <!-- view XAML here -->
    /// </rxui:ReactiveUserControl>
    /// ]]>
    /// </code>
    /// </para>
    /// </remarks>
    /// <typeparam name="TViewModel">
    /// The type of the view model backing the view.
    /// </typeparam>
    public abstract class ReactiveUserControl<TViewModel> :
        UserControl, IViewFor<TViewModel>
        where TViewModel : class
    {
        public TViewModel BindingRoot => ViewModel;

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                "ViewModel",
                typeof(TViewModel),
                typeof(ReactiveUserControl<TViewModel>),
                new PropertyMetadata(null));

        /// <inheritdoc/>
        public TViewModel ViewModel
        {
            get => (TViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <inheritdoc/>
        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (TViewModel)value;
        }
    }
}
