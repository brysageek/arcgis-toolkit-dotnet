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
    // RelayObject with a property (Value) binded to the property to observe.
    internal sealed class RelayObject : DependencyObject, IDisposable
    {
        private Action<DependencyObject, PropertyChangedEventArgs> _onPropertyChanged;
        private DependencyObject _dependencyObject;
        private readonly string _propertyName;

        public RelayObject(DependencyObject dependencyObject, string propertyName,
            Action<DependencyObject, PropertyChangedEventArgs> onPropertyChanged)
        {
            _propertyName = propertyName;

            // Bind Value property to the property to observe
            var binding = new Binding
            {
                Path = new PropertyPath(propertyName),
                Source = dependencyObject,
                Mode = BindingMode.OneWay
            };

            BindingOperations.SetBinding(this, ValueProperty, binding);
            _onPropertyChanged =
                onPropertyChanged; // Note set _onPropertyChanged after SetBinding so event not fired for the initial value
            _dependencyObject = dependencyObject;
        }

        private static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(RelayObject),
                new PropertyMetadata(null, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RelayObject)d).OnPropertyChanged();
        }

        private void OnPropertyChanged()
        {
            if (_onPropertyChanged == null) return;
            Debug.Assert(_dependencyObject != null, "Dependency Should not be Null");
            _onPropertyChanged(_dependencyObject, new PropertyChangedEventArgs(_propertyName));
        }

        public void Dispose()
        {
            _onPropertyChanged = null;
            if (_dependencyObject == null) return;
            _dependencyObject = null;
            BindingOperations.SetBinding(this, ValueProperty,
                new Binding()); // No WinRT ClearBinding so set an empty binding instead
        }
    }
}