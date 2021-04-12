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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
#if NETFX_CORE
using Windows.UI.Xaml;
#else
using System.Windows;

#endif

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// Manages a set of DependencyPropertyChangedListener by source
    /// </summary>
    /// <typeparam name="TInstance">The type of the instance.</typeparam>
    internal class DependencyPropertyChangedListeners<TInstance>
        where TInstance : class
    {
        private readonly Dictionary<DependencyObject, IList<DependencyPropertyChangedListener<TInstance>>>
            _weakEventListenersDict
                = new Dictionary<DependencyObject, IList<DependencyPropertyChangedListener<TInstance>>>();

        /// <summary>
        /// WeakReference to the instance listening for the DP changed.
        /// </summary>
        private readonly WeakReference _weakInstance;

        public DependencyPropertyChangedListeners(TInstance instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            _weakInstance = new WeakReference(instance);
        }

        /// <summary>
        /// Gets or sets the method to call when the DP changes.
        /// </summary>
        public Action<TInstance, DependencyObject, PropertyChangedEventArgs> OnEventAction { get; set; }

        /// <summary>
        /// Starts listening for the DP changed of a DO
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="propertyName">Name of the property.</param>
        public void Attach(DependencyObject source, string propertyName)
        {
            var target = (TInstance) _weakInstance.Target;

            if (target != null)
            {
                IList<DependencyPropertyChangedListener<TInstance>> weakEventListeners;
                if (_weakEventListenersDict.ContainsKey(source))
                {
                    weakEventListeners = _weakEventListenersDict[source];
                }
                else
                {
                    weakEventListeners = new List<DependencyPropertyChangedListener<TInstance>>();
                    _weakEventListenersDict[source] = weakEventListeners;
                }

                weakEventListeners.Add(new DependencyPropertyChangedListener<TInstance>(target, source, propertyName)
                    {OnEventAction = OnEventAction});
            }
            else
            {
                Debug.Assert(false, "A disposed instance tries to listen");
            }
        }

        public void Detach(DependencyObject source)
        {
            if (_weakEventListenersDict.ContainsKey(source))
            {
                var weakEventListeners = _weakEventListenersDict[source];
                foreach (var wel in weakEventListeners)
                    wel.Detach();
                _weakEventListenersDict.Remove(source);
            }
            else
            {
                Debug.Assert(false, "Trying to detach inexisting WeakEventListener");
            }
        }

        public void DetachAll()
        {
            foreach (DependencyPropertyChangedListener<TInstance> wel in _weakEventListenersDict.Values
                .SelectMany(l => l).ToArray())
            {
                wel.Detach();
            }

            _weakEventListenersDict.Clear();
        }
    }
}