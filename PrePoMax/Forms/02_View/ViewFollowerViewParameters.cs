using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Drawing.Design;
using System.Drawing;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewFollowerViewParameters
    {
        // Variables                                                                                                                      
        private DynamicCustomTypeDescriptor _dctd = null;
        private FollowerViewParameters _parameters;
        private ItemSetData _centerNodeItemSetData;
        private ItemSetData _direction1NodeItemSetData;
        private ItemSetData _direction2NodeItemSetData;


        // Properties                                                                                                               
        [Category("Data")]
        [OrderedDisplayName(0, 10, "Type")]
        [DescriptionAttribute("Select the follower view type.")]
        [Id(1, 1)]
        public FollowerViewTypeEnum Type { get { return _parameters.Type; } set { _parameters.Type = value; UpdateVisibility(); } }
        //
        [Category("Center node")]
        [OrderedDisplayName(0, 10, "Selection")]
        [DescriptionAttribute("Select the center node.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(1, 2)]
        public ItemSetData CenterNodeItemSetData
        {
            get { return _centerNodeItemSetData; }
            set { if (value != _centerNodeItemSetData) _centerNodeItemSetData = value; }
        }
        //
        [ReadOnly(true)]
        [Category("Center node")]
        [OrderedDisplayName(1, 10, "Node id")]
        [Description("Node id of the center node.")]
        [TypeConverter(typeof(StringIntegerEmptyConverter))]
        [Id(2, 2)]
        public int CenterNodeId { get { return _parameters.CenterNodeId; } set { _parameters.CenterNodeId = value; } }
        //
        [Category("Direction 1 node")]
        [OrderedDisplayName(0, 10, "Selection ")]
        [DescriptionAttribute("Select the direction 1 node.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(1, 3)]
        public ItemSetData Direction1NodeItemSetData
        {
            get { return _direction1NodeItemSetData; }
            set { if (value != _direction1NodeItemSetData) _direction1NodeItemSetData = value; }
        }
        //
        [ReadOnly(true)]
        [Category("Direction 1 node")]
        [OrderedDisplayName(1, 10, "Node id")]
        [Description("Node id of the direction 1 node.")]
        [TypeConverter(typeof(StringIntegerEmptyConverter))]
        [Id(2, 3)]
        public int Direction1NodeId { get { return _parameters.Direction1NodeId; } set { _parameters.Direction1NodeId = value; } }
        //
        [Category("Direction 2 node")]
        [OrderedDisplayName(0, 10, "Selection  ")]
        [DescriptionAttribute("Select the direction 2 node.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(1, 4)]
        public ItemSetData Direction2NodeItemSetData
        {
            get { return _direction2NodeItemSetData; }
            set { if (value != _direction2NodeItemSetData) _direction2NodeItemSetData = value; }
        }
        //
        [ReadOnly(true)]
        [Category("Direction 2 node")]
        [OrderedDisplayName(1, 10, "Node id")]
        [Description("Node id of the direction 2 node.")]
        [TypeConverter(typeof(StringIntegerEmptyConverter))]
        [Id(2, 4)]
        public int Direction2NodeId { get { return _parameters.Direction2NodeId; } set { _parameters.Direction2NodeId = value; } }


        // Constructors                                                                                                             
        public ViewFollowerViewParameters(FollowerViewParameters followerViewParameters)
        {
            _parameters = followerViewParameters;
            //
            _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            //
            _centerNodeItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _centerNodeItemSetData.ToStringType = ItemSetDataToStringType.SelectSingleNode;
            //
            _direction1NodeItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _direction1NodeItemSetData.ToStringType = ItemSetDataToStringType.SelectSingleNode;
            //
            _direction2NodeItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _direction2NodeItemSetData.ToStringType = ItemSetDataToStringType.SelectSingleNode;
            //
            UpdateVisibility();
        }


        // Methods                                                                                                                  
        public FollowerViewParameters GetBase()
        {
            return _parameters;
        }
        private void UpdateVisibility()
        {
            bool visible = _parameters.Type == FollowerViewTypeEnum.Plane;
            _dctd.GetProperty(nameof(Direction1NodeItemSetData)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(Direction1NodeId)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(Direction2NodeItemSetData)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(Direction2NodeId)).SetIsBrowsable(visible);
        }

       
    }
}
