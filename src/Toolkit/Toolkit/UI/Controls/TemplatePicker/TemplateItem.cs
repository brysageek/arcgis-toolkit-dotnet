using System.ComponentModel;
using System.Windows.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    class TemplateItem : INotifyPropertyChanged
    {
        public FeatureLayer Layer { get; set; }

        public FeatureType FeatureType { get; set; }

        public FeatureTemplate FeatureTemplate { get; set; }

        public Symbology.Symbol Symbol { get; set; }

        public GeometryType GeometryType { get; set; }

        internal void SetSymbol(Symbology.Symbol symbol)
        {
            if (symbol != null)
            {
                // force the geometry type since GeometryType.Unknown doesn't work well with advanced symbology.
                var geometryType = GeometryType.Unknown;
                var gdbFeatureTable = Layer == null ? null : Layer.FeatureTable as GeodatabaseFeatureTable;
                if (gdbFeatureTable != null && gdbFeatureTable.ServiceInfo != null)
                    geometryType = gdbFeatureTable.ServiceInfo.GeometryType;
                GeometryType = geometryType;
            }

            Symbol = symbol;
            OnPropertyChanged("GeometryType");
            OnPropertyChanged("Symbol");
        }

        public ICommand Command { get; set; }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}