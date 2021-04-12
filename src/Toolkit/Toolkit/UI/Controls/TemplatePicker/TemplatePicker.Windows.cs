#if !XAMARIN
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Esri.ArcGISRuntime.ArcGISServices;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// A template picker control enables selecting feature types to add 
    /// when editing a feature layer.
    /// </summary>
    public class TemplatePicker : Control
    {
        // Underlying ItemsControl
        private ItemsControl _itemsControl;

        // TemplateItems by layer (useful to update quickly ItemsSource when a layer visibility changes)
        private readonly Dictionary<Layer, List<TemplateItem>> _templatesByLayer = new Dictionary<Layer, List<TemplateItem>>();

        // WeakEventListeners for feature layers changes (so the template picker may be released even if the feature layers are long lived object)

        // Listen for feature layers DP changes
        private DependencyPropertyChangedListeners<TemplatePicker> _featureLayerPropertyChangedListeners;
        // Listen for Layers Collection changed
        private WeakEventListener<TemplatePicker, object, NotifyCollectionChangedEventArgs> _layersWeakEventListener;

        private ThrottleTimer _updateItemsSourceTimer;
        private bool _isLoaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatePicker"/> class.
        /// </summary>
        public TemplatePicker()
        {
            DefaultStyleKey = typeof(TemplatePicker);
            Loaded += (sender, args) => OnLoaded();
            Unloaded += (sender, args) => OnUnloaded();
        }

        private void OnLoaded()
        {
            _isLoaded = true;
            DetachLayersHandler();
            AttachLayersHandler(Layers);
            RebuildAllTemplates();
        }

        private void OnUnloaded()
        {
            _isLoaded = false;
            DetachLayersHandler();
        }

        /// <summary>
        /// Occurs when a template is selected.
        /// </summary>
        public event EventHandler<TemplatePickedEventArgs> TemplatePicked;

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application.
        /// code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
#if NETFX_CORE
        protected 
#else
        public
#endif
            override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _itemsControl = GetTemplateChild("TemplateItems") as ItemsControl;
            InitItemsSource();
        }

        private void OnItemClicked(object item)
        {
            if (TemplatePicked != null && item is TemplateItem templateItem)
            {
                TemplatePicked(this, new TemplatePickedEventArgs(templateItem.Layer, templateItem.FeatureType, templateItem.FeatureTemplate));
            }
        }

        private void RebuildAllTemplates()
        {
            _templatesByLayer.Clear();
            if (Layers != null)
            {
                foreach (var flayer in Layers.OfType<FeatureLayer>())
                {
                    RebuildTemplate(flayer);
                }
            }

            InitItemsSource();
        }

        private void RebuildTemplate(FeatureLayer flayer)
        {
            var templates = new List<TemplateItem>();
            ArcGISFeatureServiceInfo serviceInfo = null;
            if (flayer.FeatureTable is ServiceGeodatabase gdbFeatureTable && !gdbFeatureTable. && flayer.LoadStatus == LoadStatus.Loaded)
            {
                try
                {
                    serviceInfo = gdbFeatureTable.ServiceInfo; //todo
                }
                catch { }
            }

            if (serviceInfo != null)
            {
                var cmd = new InvokeCommand(OnItemClicked);
                var renderer = flayer.Renderer ?? (serviceInfo.DrawingInfo == null ? null : serviceInfo.DrawingInfo.Renderer);
                if (serviceInfo.Templates != null)
                {
                    foreach (var template in serviceInfo.Templates)
                    {
                        var item = new TemplateItem
                        {
                            Layer = flayer,
                            FeatureTemplate = template,
                            Command = cmd,
                        };
                        templates.Add(item);
                        if (renderer != null)
                        {
                            var g = new Graphic(template.Prototype.Attributes ?? Enumerable.Empty<System.Collections.Generic.KeyValuePair<string, object>>()); // Need to disambiguate from winstore toolkit KeyValuePair
                            item.SetSymbol(renderer.GetSymbol(g));
                        }
                    }
                }

                if (serviceInfo.Types != null)
                {
                    foreach (var type in serviceInfo.Types)
                    {
                        foreach (var template in type.Templates)
                        {
                            var item = new TemplateItem
                            {
                                Layer = flayer,
                                FeatureType = type,
                                FeatureTemplate = template,
                                Command = cmd,
                            };
                            templates.Add(item);
                            if (renderer != null)
                            {
                                var g = new Graphic(template.Prototype.Attributes ?? Enumerable.Empty<System.Collections.Generic.KeyValuePair<string, object>>());
                                item.SetSymbol(renderer.GetSymbol(g));
                            }
                        }
                    }
                }
            }

            _templatesByLayer[flayer] = templates;
        }

        private void InitItemsSource()
        {
            // wait for the map to stop navigating so
            //map navigation performance doesn't suffer from it.
            if (_updateItemsSourceTimer == null)
            {
                _updateItemsSourceTimer = new ThrottleTimer(100) { Action = InitItemsSourceImpl };
            }

            _updateItemsSourceTimer.Invoke();
        }

        private void InitItemsSourceImpl()
        {
            if (_itemsControl == null) return;
            var templates = new List<TemplateItem>();
            if (Layers != null)
            {
                foreach (var flayer in Layers.OfType<FeatureLayer>().Where(l => l.IsVisible && IsInScaleRange(l)))
                {
                    if (_templatesByLayer.ContainsKey(flayer))
                    {
                        templates.AddRange(_templatesByLayer[flayer]);
                    }
                }
            }

            if (!AreEquals(_itemsControl.ItemsSource as ICollection<TemplateItem>, templates)) // Avoid changing ItemSources if the list didn't change
            {
                _itemsControl.ItemsSource = templates;
            }
        }

        private bool AreEquals(ICollection<TemplateItem> templates1, ICollection<TemplateItem> templates2)
        {
            return templates1 != null && templates2 != null && templates1.Count == templates2.Count && templates1.Zip(templates2, (item1, item2) => item1 == item2).All(b => b);
        }

        private bool IsInScaleRange(Layer layer)
        {
            return !(Scale > 0.0) || (!(layer.MinScale < Scale) && !(layer.MaxScale > Scale)); // Note: ! useful for managing correctly NaN cases 
        }

        /// <summary>
        /// Gets or sets the layers for which templates are displayed.
        /// </summary>
        public IEnumerable<Layer> Layers
        {
            get => (IEnumerable<Layer>)GetValue(LayersProperty);
            set => SetValue(LayersProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Layers"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty LayersProperty =
            DependencyProperty.Register("Layers", typeof(IEnumerable<Layer>), typeof(TemplatePicker), new PropertyMetadata(null, OnLayersPropertyChanged));

        private static void OnLayersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = d as TemplatePicker;
            if (picker != null)
                picker.OnLayersPropertyChanged(e.NewValue as IEnumerable<Layer>);
        }

        private void OnLayersPropertyChanged(IEnumerable<Layer> newLayers)
        {
            if (_isLoaded) // else useless to subscribe to events and to build templates for now
            {
                DetachLayersHandler();
                AttachLayersHandler(newLayers);
                RebuildAllTemplates();
            }
        }

        private void DetachLayersHandler()
        {
            if (_layersWeakEventListener != null)
            {
                _layersWeakEventListener.Detach();
                _layersWeakEventListener = null;
            }
            if (_featureLayerPropertyChangedListeners != null)
            {
                _featureLayerPropertyChangedListeners.DetachAll();
                _featureLayerPropertyChangedListeners = null;
            }
        }

        private void AttachLayersHandler(IEnumerable<Layer> layers)
        {
            if (layers != null)
            {
                // Subscribe to Layers Collection changes
                var layersINotifyCollectionChanged = layers as INotifyCollectionChanged;
                if (layersINotifyCollectionChanged != null)
                {
                    Debug.Assert(_layersWeakEventListener == null);
                    _layersWeakEventListener = new WeakEventListener<TemplatePicker, object, NotifyCollectionChangedEventArgs>(this)
                    {
                        OnEventAction = (instance, source, eventArgs) => instance.OnLayerCollectionChanged(source, eventArgs),
                        OnDetachAction = weakEventListener => layersINotifyCollectionChanged.CollectionChanged -= weakEventListener.OnEvent
                    };
                    layersINotifyCollectionChanged.CollectionChanged += _layersWeakEventListener.OnEvent;
                }

                // Subscribe to FeatureLayers Property Changes
                Debug.Assert(_featureLayerPropertyChangedListeners == null);
                _featureLayerPropertyChangedListeners = new DependencyPropertyChangedListeners<TemplatePicker>(this)
                {
                    OnEventAction = (instance, source, eventArgs) => instance.OnLayerPropertyChanged(source, eventArgs)
                };

                foreach (var layer in layers.OfType<FeatureLayer>())
                    AttachLayerHandler(layer);
            }
        }

        private void AttachLayerHandler(FeatureLayer flayer)
        {
            if (_featureLayerPropertyChangedListeners != null)
            {
                _featureLayerPropertyChangedListeners.Attach(flayer, "Renderer"); // to do: subscribe to Renderer changed events
                _featureLayerPropertyChangedListeners.Attach(flayer, "IsVisible");
                _featureLayerPropertyChangedListeners.Attach(flayer, "MinScale");
                _featureLayerPropertyChangedListeners.Attach(flayer, "MaxScale");
                _featureLayerPropertyChangedListeners.Attach(flayer, "Status");
                _featureLayerPropertyChangedListeners.Attach(flayer, "FeatureTable");
            }
        }

        private void DetachLayerHandler(FeatureLayer flayer)
        {
            if (_featureLayerPropertyChangedListeners != null)
            {
                _featureLayerPropertyChangedListeners.Detach(flayer);
            }
        }

        private void OnLayerPropertyChanged(DependencyObject sender, PropertyChangedEventArgs e)
        {
            if (!(sender is FeatureLayer flayer)) return;

            if (e.PropertyName == "IsVisible" || e.PropertyName == "MinScale" || e.PropertyName == "MaxScale")
            {
                InitItemsSource();
            }
            else // "Renderer"/"Status"/"FeatureTable"
            {
                RebuildTemplate(flayer);
                InitItemsSource();
            }
        }

        private void OnLayerCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    if (e.NewItems != null)
                    {
                        foreach (var flayer in e.NewItems.OfType<FeatureLayer>())
                        {
                            AttachLayerHandler(flayer);
                            RebuildTemplate(flayer);
                        }
                    }
                    if (e.OldItems != null)
                    {
                        foreach (var flayer in e.OldItems.OfType<FeatureLayer>())
                        {
                            DetachLayerHandler(flayer);
                            _templatesByLayer.Remove(flayer);
                        }
                    }
                    InitItemsSource();
                    break;

                case NotifyCollectionChangedAction.Move:
                    InitItemsSource();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    DetachLayersHandler();
                    AttachLayersHandler(Layers);
                    RebuildAllTemplates();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Gets or sets the data template used to display each TemplatePicker item.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemTemplate"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(TemplatePicker), null);

        /// <summary>
        /// Gets or sets the template that defines the panel that controls the layout of items.
        /// </summary>
        public ItemsPanelTemplate ItemsPanel
        {
            get => (ItemsPanelTemplate)GetValue(ItemsPanelProperty);
            set => SetValue(ItemsPanelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemsPanel"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsPanelProperty =
            DependencyProperty.Register("ItemsPanel", typeof(ItemsPanelTemplate), typeof(TemplatePicker), null);

        /// <summary>
        /// Gets or sets the scale if filtering layers by their visible scale range.
        /// </summary>
        /// <remarks>Typically this value has to be binded to the <see cref="MapView.Scale">MapView scale property</see></remarks>
        /// <value>
        /// The scale filter.
        /// </value>
        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Scale"/> Dependency property.
        /// </summary>
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(double), typeof(TemplatePicker), new PropertyMetadata(double.NaN, OnScalePropertyChanged));

        private static void OnScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TemplatePicker picker)
            {
                picker.InitItemsSource();
            }
        }
    }
}
#endif
