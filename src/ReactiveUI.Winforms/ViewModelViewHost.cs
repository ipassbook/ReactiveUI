﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;

namespace ReactiveUI.Winforms
{
    [DefaultProperty("ViewModel")]
    public partial class ViewModelControlHost : UserControl, IReactiveObject, IViewFor
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private Control _defaultContent;
        private IObservable<string> _viewContractObservable;
        private object _viewModel;

        public static bool DefaultCacheViewsEnabled { get; set; }

        public ViewModelControlHost()
        {
            InitializeComponent();
            _cacheViews = DefaultCacheViewsEnabled;

            foreach (var subscription in SetupBindings())
            {
                _disposables.Add(subscription);
            }
        }

        private IEnumerable<IDisposable> SetupBindings()
        {
            var viewChanges =
                this.WhenAnyValue(x => x.Content).Select(x => x as Control).Where(x => x != null).Subscribe(x =>
                {
                    // change the view in the ui
                    SuspendLayout();

                    // clear out existing visible control view
                    foreach (Control c in Controls)
                    {
                        c.Dispose();
                        Controls.Remove(c);
                    }

                    x.Dock = DockStyle.Fill;
                    Controls.Add(x);
                    ResumeLayout();
                });

            yield return viewChanges;

            yield return this.WhenAny(x => x.DefaultContent, x => x.Value).Subscribe(x =>
            {
                if (x != null)
                {
                    Content = DefaultContent;
                }
            });

            ViewContractObservable = Observable<string>.Default;

            var vmAndContract =
                this.WhenAny(x => x.ViewModel, x => x.Value)
                    .CombineLatest(
                        this.WhenAnyObservable(x => x.ViewContractObservable),
                        (vm, contract) => new { ViewModel = vm, Contract = contract });

            yield return vmAndContract.Subscribe(
                x =>
            {
                // set content to default when viewmodel is null
                if (ViewModel == null)
                {
                    if (DefaultContent != null)
                    {
                        Content = DefaultContent;
                    }

                    return;
                }

                if (CacheViews)
                {
                    // when caching views, check the current viewmodel and type
                    var c = _content as IViewFor;

                    if (c != null && c.ViewModel != null
                        && c.ViewModel.GetType() == x.ViewModel.GetType())
                        {
                        c.ViewModel = x.ViewModel;

                        // return early here after setting the viewmodel
                        // allowing the view to update it's bindings
                        return;
                    }
                }

                IViewLocator viewLocator = ViewLocator ?? ReactiveUI.ViewLocator.Current;
                IViewFor view = viewLocator.ResolveView(x.ViewModel, x.Contract);
                view.ViewModel = x.ViewModel;
                Content = view;
            }, RxApp.DefaultExceptionHandler.OnNext);
        }

        /// <inheritdoc/>
        public event PropertyChangingEventHandler PropertyChanging
        {
            add => PropertyChangingEventManager.AddHandler(this, value);
            remove => PropertyChangingEventManager.RemoveHandler(this, value);
        }

        /// <inheritdoc/>
        void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args)
        {
            PropertyChangingEventManager.DeliverEvent(this, args);
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => PropertyChangedEventManager.AddHandler(this, value);
            remove => PropertyChangedEventManager.RemoveHandler(this, value);
        }

        /// <inheritdoc/>
        void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChangedEventManager.DeliverEvent(this, args);
        }

        public Control CurrentView => _content as Control;

        [Category("ReactiveUI")]
        [Description("The default control when no viewmodel is specified")]
        public Control DefaultContent
        {
            get => _defaultContent;
            set => this.RaiseAndSetIfChanged(ref _defaultContent, value);
        }

        [Browsable(false)]
        public IObservable<string> ViewContractObservable
        {
            get => _viewContractObservable;
            set => this.RaiseAndSetIfChanged(ref _viewContractObservable, value);
        }

        [Browsable(false)]
        public IViewLocator ViewLocator { get; set; }

        /// <inheritdoc/>
        [Category("ReactiveUI")]
        [Description("The viewmodel to host.")]
        [Bindable(true)]
        public object ViewModel
        {
            get => _viewModel;
            set => this.RaiseAndSetIfChanged(ref _viewModel, value);
        }

        private object _content;

        [Category("ReactiveUI")]
        [Description("The Current View")]
        [Bindable(true)]
        public object Content
        {
            get => _content;
            protected set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        private bool _cacheViews;

        [Category("ReactiveUI")]
        [Description("Cache Views")]
        [Bindable(true)]
        [DefaultValue(true)]
        public bool CacheViews
        {
            get => _cacheViews;
            set => this.RaiseAndSetIfChanged(ref _cacheViews, value);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
                _disposables.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
