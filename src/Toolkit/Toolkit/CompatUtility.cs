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
using System.Diagnostics;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
#else
using System.Windows;
using System.Windows.Threading;

#endif

namespace Esri.ArcGISRuntime.Toolkit
{
    internal static class CompatUtility
    {
        private static bool? _isInDesignMode;

        public static bool IsDesignMode
        {
            get
            {
                if (_isInDesignMode.HasValue) return _isInDesignMode.Value;
#if NETFX_CORE
					_isInDesignMode = Windows.ApplicationModel.DesignMode.DesignModeEnabled;
#else
                var prop = System.ComponentModel.DesignerProperties.IsInDesignModeProperty;
                _isInDesignMode
                    = (bool) System.ComponentModel.DependencyPropertyDescriptor
                        .FromProperty(prop, typeof(FrameworkElement))
                        .Metadata.DefaultValue;
#endif

                return _isInDesignMode.Value;
            }
        }

        public static float LogicalDpi(DependencyObject dp = null)
        {
#if NETFX_CORE
			return Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi;
#else
            if (dp == null)
            {
                return 96f;
            }

            System.Windows.Media.Matrix m =
                PresentationSource.FromDependencyObject(dp).CompositionTarget.TransformToDevice;
            return (float) m.M11 * 96.0f;
#endif
        }

#if NETFX_CORE
		public static void ExecuteOnUIThread(Action action, CoreDispatcher dispatcher)
		{
			if (dispatcher.HasThreadAccess)
				action();
			else
			{
				var asyncAction = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
			}
		}
#else
        public static void ExecuteOnUIThread(Action action, Dispatcher dispatcher)
        {
            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.BeginInvoke(action);
            }
        }
#endif

        // Execute a task in the UI thread
        internal static Task<T> ExecuteOnUIThread<T>(Func<Task<T>> f)
        {
            var dispatcher = GetDispatcher();
            return dispatcher == null ? f() : ExecuteOnUIThread(f, dispatcher);
        }

#if NETFX_CORE
		private static CoreDispatcher GetDispatcher()
		{
			return Application.Current != null && CoreApplication.MainView != null && CoreApplication.MainView.CoreWindow != null && !CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess
				? CoreApplication.MainView.CoreWindow.Dispatcher
				: null;
		}

		private static async Task<T> ExecuteOnUIThread<T>(Func<Task<T>> f, CoreDispatcher dispatcher)
		{
			Debug.Assert(dispatcher != null);
			Task<T> task = null;
			await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { task = f(); });
			return await task;
		}
#else
        private static Dispatcher GetDispatcher()
        {
            return Application.Current != null && Application.Current.Dispatcher != null &&
                   !Application.Current.Dispatcher.CheckAccess()
                ? Application.Current.Dispatcher
                : null;
        }

        private static async Task<T> ExecuteOnUIThread<T>(Func<Task<T>> f, Dispatcher dispatcher)
        {
            Debug.Assert(dispatcher != null, "Dispatcher Should not be Null");
            Task<T> task = null;
            await dispatcher.BeginInvoke(new Action(() => { task = f(); }));
            return await task;
        }
#endif
    }
}