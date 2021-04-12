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
using System.Windows.Threading;

namespace Esri.ArcGISRuntime.Toolkit.Internal
{
    /// <summary>
    /// The throttle timer is useful for limiting the number of requests to a method if
    /// the method is repeatedly called many times but you only want the method raised once.
    /// It delays raising the method until a set interval, and any previous calls to the
    /// actions in that interval will be cancelled.
    /// </summary>
    internal class ThrottleTimer
    {
        readonly DispatcherTimer _throttleTimer;

        internal ThrottleTimer(int milliseconds) : this(milliseconds, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottleTimer"/> class.
        /// </summary>
        /// <param name="milliseconds">Milliseconds to throttle.</param>
        /// <param name="handler">The delegate to invoke.</param>
        internal ThrottleTimer(int milliseconds, Action handler)
        {
            Action = handler;
            _throttleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
            _throttleTimer.Tick += (s, e) =>
            {
                _throttleTimer.Stop();
                if (Action != null)
                    Action.Invoke();
            };
        }

        /// <summary>
        /// Delegate to Invoke.
        /// </summary>
        /// <value>The action.</value>
        public Action Action { get; set; }

        /// <summary>
        /// Invokes this instance (note that this will happen asynchronously and delayed).
        /// </summary>
        public void Invoke()
        {
            _throttleTimer.Stop();
            _throttleTimer.Start();
        }

        /// <summary>
        /// Cancels this timer if running.
        /// </summary>
        internal void Cancel()
        {
            _throttleTimer.Stop();
        }
    }
}
