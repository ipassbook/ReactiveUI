﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows.Input;
#if NETFX_CORE
using Windows.UI.Xaml.Input;
#endif

namespace ReactiveUI
{
    /// <summary>
    /// This binder is the default binder for connecting to arbitrary events.
    /// </summary>
    public class CreatesCommandBindingViaEvent : ICreatesCommandBinding
    {
        // NB: These are in priority order
        private static readonly List<Tuple<string, Type>> defaultEventsToBind = new List<Tuple<string, Type>>
        {
            Tuple.Create("Click", typeof(EventArgs)),
            Tuple.Create("TouchUpInside", typeof(EventArgs)),
            Tuple.Create("MouseUp", typeof(EventArgs)),
#if NETFX_CORE
            Tuple.Create("PointerReleased", typeof(PointerRoutedEventArgs)),
            Tuple.Create("Tapped", typeof(TappedRoutedEventArgs)),
#endif
        };

        /// <inheritdoc/>
        public int GetAffinityForObject(Type type, bool hasEventTarget)
        {
            if (hasEventTarget)
            {
                return 5;
            }

            return defaultEventsToBind.Any(x =>
            {
                var ei = type.GetRuntimeEvent(x.Item1);
                return ei != null;
            }) ? 3 : 0;
        }

        /// <inheritdoc/>
        public IDisposable BindCommandToObject(ICommand command, object target, IObservable<object> commandParameter)
        {
            var type = target.GetType();
            var eventInfo = defaultEventsToBind
                .Select(x => new { EventInfo = type.GetRuntimeEvent(x.Item1), Args = x.Item2 })
                .FirstOrDefault(x => x.EventInfo != null);

            if (eventInfo == null)
            {
                throw new Exception(
                                    string.Format(
                                                  "Couldn't find a default event to bind to on {0}, specify an event expicitly",
                                                  target.GetType().FullName));
            }

            var mi = GetType().GetRuntimeMethods().First(x => x.Name == "BindCommandToObject" && x.IsGenericMethod);
            mi = mi.MakeGenericMethod(eventInfo.Args);

            return (IDisposable)mi.Invoke(this, new[] { command, target, commandParameter, eventInfo.EventInfo.Name });
        }

        /// <inheritdoc/>
        public IDisposable BindCommandToObject<TEventArgs>(ICommand command, object target, IObservable<object> commandParameter, string eventName)
#if MONO
            where TEventArgs : EventArgs
#endif
        {
            var ret = new CompositeDisposable();

            object latestParameter = null;
            var evt = Observable.FromEventPattern<TEventArgs>(target, eventName);

            ret.Add(commandParameter.Subscribe(x => latestParameter = x));

            ret.Add(evt.Subscribe(ea =>
            {
                if (command.CanExecute(latestParameter))
                {
                    command.Execute(latestParameter);
                }
            }));

            return ret;
        }
    }
}
