using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [Serializable]
    public enum FollowerViewTypeEnum
    {
        Point,
        Plane
    }
    //
    [Serializable]
    public class FollowerViewParameters
    {
        // Variables                                                                                                                
        private FollowerViewTypeEnum _followerViewType;
        private int _centerNodeId;
        private int _direction1NodeId;
        private int _direction2NodeId;


        // Properties                                                                                                               
        public FollowerViewTypeEnum Type { get { return _followerViewType; } set { _followerViewType = value; } }
        public int CenterNodeId { get { return _centerNodeId; } set { _centerNodeId = value; } }
        public int Direction1NodeId { get { return _direction1NodeId; } set { _direction1NodeId = value; } }
        public int Direction2NodeId { get { return _direction2NodeId; } set { _direction2NodeId = value; } }


        // Constructors                                                                                                             
        public FollowerViewParameters()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void Reset()
        {
            _followerViewType = FollowerViewTypeEnum.Point;
            _centerNodeId = int.MinValue;
            _direction1NodeId = int.MinValue;
            _direction2NodeId = int.MinValue;
        }
    }
}
