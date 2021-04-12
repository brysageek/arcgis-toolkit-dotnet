using System;
using System.Collections.Generic;
using System.Text;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// EventArgs type for <see cref="TemplatePicker.TemplatePicked"/> event.
    /// </summary>
    public sealed class TemplatePickedEventArgs : EventArgs
    {
        internal TemplatePickedEventArgs(FeatureLayer layer,
            FeatureType featureType,
            FeatureTemplate featureTemplate)
        {
            Layer = layer;
            FeatureType = featureType;
            FeatureTemplate = featureTemplate;
        }

        /// <summary>
        /// Gets the feature layer of the selected template.
        /// </summary>
        /// <value>
        /// The feature layer.
        /// </value>
        public FeatureLayer Layer { get; private set; }

        /// <summary>
        /// Gets the feature sub type selected. 
        /// </summary>
        /// <remarks>The sub type is null when the template is not associated to a sub type
        /// (for example when the templates are generated from an UniqueValueRenderer)</remarks>
        /// <value>
        /// The sub type of the template selected.
        /// </value>
        public FeatureType FeatureType { get; private set; }

        /// <summary>
        /// Gets the feature template selected.
        /// </summary>
        /// <value>sc
        /// The feature template.
        /// </value>
        public FeatureTemplate FeatureTemplate { get; private set; }
    }
}