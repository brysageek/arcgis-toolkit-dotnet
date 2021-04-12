// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System;
using System.ComponentModel;
using System.Diagnostics;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#else
using System.Windows;
using System.Windows.Data;

#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    internal class DependencyPropertyChangedListener<TInstance>
        where TInstance : class
    {
        /// <summary>
        /// WeakReference to the instance listening for the DP changed.
        /// </summary>
        private readonly WeakReference _weakInstance;

        // Relay Object binded to the DP to observe
        private RelayObject _relayObject;

        /// <summary>
        /// Gets or sets the method to call when the DP changes.
        /// </summary>
        public Action<TInstance, DependencyObject, PropertyChangedEventArgs> OnEventAction { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyPropertyChangedListener{TInstance}"/> class.
        /// </summary>
        /// <param name="instance">Instance subscribing to the property changed event.</param>
        /// <param name="dependencyObject">Source of the propertyChanged</param>
        /// <param name="propertyName">Dependency property name</param>
        public DependencyPropertyChangedListener(TInstance instance, DependencyObject dependencyObject,
            string propertyName)
        {
            _relayObject = new RelayObject(dependencyObject, propertyName, OnPropertyChanged);
            _weakInstance = new WeakReference(instance);
        }

        private void OnPropertyChanged(DependencyObject sender, PropertyChangedEventArgs args)
        {
            var target = (TInstance) _weakInstance.Target;
            if (target != null)
            {
                // Call registered action
                OnEventAction?.Invoke(target, sender, args);
            }
            else
            {
                // Detach from event
                Detach();
            }
        }

        /// <summary>
        /// Stop listening for the dependency property changed.
        /// </summary>
        public void Detach()
        {
            if (_relayObject != null)
            {
                CompatUtility.ExecuteOnUIThread(() =>
                {
                    if (_relayObject == null) return;
                    _relayObject.Dispose();
                    _relayObject = null;
                }, _relayObject.Dispatcher);
            }
        }
    }
}